using System;
using System.Collections.Generic;
using System.Linq;

namespace Design_Patterns.Structural_Pattern
{
    // ----- Contracts -----
    public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice)
    {
        public decimal Subtotal => checked(Quantity * UnitPrice);
    }

    public sealed record Address(string Recipient, string Line1, string City, string Country, string Email);

    public enum PaymentMethod { Visa, Paypal, Cod }

    public sealed record CheckoutRequest(
        IReadOnlyList<OrderLine> Lines,
        Address ShipTo,
        PaymentMethod Method
    );

    public sealed record CheckoutResult(
        bool Success, string? OrderId, string? TrackingId, string Message
    );

    public sealed record PaymentResult(bool Success, string Provider, string Message);

    public interface IInventoryService
    {
        bool Reserve(IEnumerable<OrderLine> lines); // đơn giản hoá: true nếu đủ hàng
    }

    public interface IPaymentService
    {
        PaymentResult Charge(decimal amount, PaymentMethod method);
    }

    public interface IShippingService
    {
        string CreateShipment(Address to); // trả về TrackingId
    }

    public interface INotificationService
    {
        void SendEmail(string to, string subject, string body);
    }

    // ----- Facade -----
    public sealed class CheckoutFacade
    {
        private readonly IInventoryService _inventory;
        private readonly IPaymentService _payment;
        private readonly IShippingService _shipping;
        private readonly INotificationService _notify;

        public CheckoutFacade(
            IInventoryService inventory,
            IPaymentService payment,
            IShippingService shipping,
            INotificationService notify)
        {
            _inventory = inventory;
            _payment = payment;
            _shipping = shipping;
            _notify = notify;
        }

        public CheckoutResult PlaceOrder(CheckoutRequest req)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (req.Lines is null || req.Lines.Count == 0)
                return new(false, null, null, "Cart is empty");

            // 1) Reserve inventory
            if (!_inventory.Reserve(req.Lines))
                return new(false, null, null, "Out of stock");

            // 2) Charge payment (COD = thành công giả lập, charge 0)
            var amount = req.Lines.Sum(l =>
            {
                if (l.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(l.Quantity));
                if (l.UnitPrice < 0) throw new ArgumentOutOfRangeException(nameof(l.UnitPrice));
                return l.Subtotal;
            });

            var payRes = req.Method == PaymentMethod.Cod
                ? new PaymentResult(true, "COD", "Payment on delivery")
                : _payment.Charge(amount, req.Method);

            if (!payRes.Success)
                return new(false, null, null, $"Payment failed: {payRes.Message}");

            // 3) Create shipment
            var tracking = _shipping.CreateShipment(req.ShipTo);

            // 4) Notify customer (best effort)
            try
            {
                _notify.SendEmail(req.ShipTo.Email, "Order confirmed",
                    $"Your order has been placed. Tracking: {tracking}");
            }
            catch { /* log và bỏ qua trong demo */ }

            // 5) Build result
            var orderId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            return new(true, orderId, tracking, $"Order placed via {payRes.Provider}");
        }
    }

    // Simple fake implementations for tests/demo
    public sealed class MemoryInventory : IInventoryService
    {
        private readonly Dictionary<string, int> _stock;
        public MemoryInventory(IDictionary<string, int> initial)
            => _stock = new(initial ?? new Dictionary<string, int>());

        public bool Reserve(IEnumerable<OrderLine> lines)
        {
            // kiểm tra đủ hàng
            foreach (var line in lines)
            {
                if (!_stock.TryGetValue(line.Sku, out var qty) || qty < line.Quantity)
                    return false;
            }
            // trừ kho (đơn giản)
            foreach (var line in lines)
                _stock[line.Sku] -= line.Quantity;
            return true;
        }
    }

    public sealed class SimplePayment : IPaymentService
    {
        private readonly bool _shouldFail;
        public SimplePayment(bool shouldFail = false) => _shouldFail = shouldFail;

        public PaymentResult Charge(decimal amount, PaymentMethod method)
        {
            if (_shouldFail) return new(false, method.ToString(), "Gateway error");
            if (amount <= 0) return new(false, method.ToString(), "Invalid amount");
            return new(true, method.ToString(), $"Charged {amount} via {method}");
        }
    }

    public sealed class DummyShipping : IShippingService
    {
        public string CreateShipment(Address to)
            => $"TRK-{to.City.ToUpperInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    public sealed class MemoryNotification : INotificationService
    {
        public readonly List<(string To, string Subj, string Body)> Sent = new();
        public void SendEmail(string to, string subject, string body) => Sent.Add((to, subject, body));
    }
    public class Program
    {
        public static void Main()
        {
            // 1. Khởi tạo kho với hàng có sẵn
            var inventory = new MemoryInventory(new Dictionary<string, int>
            {
                ["SKU-001"] = 10,
                ["SKU-002"] = 5
            });

            // 2. Khởi tạo các service
            var payment = new SimplePayment();
            var shipping = new DummyShipping();
            var notification = new MemoryNotification();

            // 3. Tạo Facade
            var checkout = new CheckoutFacade(inventory, payment, shipping, notification);

            // 4. Tạo đơn hàng mẫu
            var orderLines = new List<OrderLine>
        {
            new OrderLine("SKU-001", 2, 50m),
            new OrderLine("SKU-002", 1, 100m)
        };

            var address = new Address(
                Recipient: "John Doe",
                Line1: "123 Main Street",
                City: "New York",
                Country: "US",
                Email: "john@example.com"
            );

            var request = new CheckoutRequest(orderLines, address, PaymentMethod.Visa);

            // 5. Đặt hàng
            var result = checkout.PlaceOrder(request);

            // 6. In kết quả
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"OrderId: {result.OrderId}");
            Console.WriteLine($"TrackingId: {result.TrackingId}");
            Console.WriteLine($"Message: {result.Message}");

            // 7. Kiểm tra email đã gửi
            foreach (var email in notification.Sent)
            {
                Console.WriteLine($"\nEmail sent to: {email.To}");
                Console.WriteLine($"Subject: {email.Subj}");
                Console.WriteLine($"Body: {email.Body}");
            }
        }
    }
}
