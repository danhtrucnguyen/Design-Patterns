using System;
using System.Collections.Generic;
using System.Linq;

namespace Design_Patterns.Creational_Pattern { 
    public enum ShippingMethod { Standard, Express }

    public sealed record OrderItem(string Sku, int Quantity, decimal UnitPrice)
    {
        public decimal Subtotal => Quantity * UnitPrice;
    }

    public sealed record Order(
        string Id,
        string CustomerId,
        IReadOnlyList<OrderItem> Items,
        decimal Subtotal,
        decimal Discount,
        decimal ShippingFee,
        decimal Total,
        ShippingMethod ShippingMethod,
        string ShippingAddress
    );

    public interface IOrderBuilder
    {
        IOrderBuilder WithCustomer(string id);
        IOrderBuilder AddItem(string sku, int qty, decimal unitPrice);
        IOrderBuilder WithCoupon(string code, decimal value); // value: số tiền giảm
        IOrderBuilder WithShipping(ShippingMethod method, string address);
        Order Build();
    }

    public sealed class OrderBuilder : IOrderBuilder
    {
        private string _customerId = "";
        private readonly List<OrderItem> _items = new();
        private string? _couponCode;
        private decimal _discount; // tiền giảm tuyệt đối
        private ShippingMethod _shippingMethod = ShippingMethod.Standard;
        private string _shippingAddress = "";

        public IOrderBuilder WithCustomer(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Customer id is required");
            _customerId = id.Trim();
            return this;
        }

        public IOrderBuilder AddItem(string sku, int qty, decimal unitPrice)
        {
            if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU is required");
            if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
            if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));

            _items.Add(new OrderItem(sku.Trim(), qty, unitPrice));
            return this;
        }

        public IOrderBuilder WithCoupon(string code, decimal value)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Coupon code is required");
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));

            _couponCode = code.Trim();
            _discount = value;
            return this;
        }

        public IOrderBuilder WithShipping(ShippingMethod method, string address)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Shipping address is required");
            _shippingMethod = method;
            _shippingAddress = address.Trim();
            return this;
        }

        public Order Build()
        {
            if (string.IsNullOrWhiteSpace(_customerId))
                throw new InvalidOperationException("Customer is required");
            if (_items.Count == 0)
                throw new InvalidOperationException("At least one item is required");

            var subtotal = _items.Sum(i => i.Subtotal);
            var discount = Math.Min(_discount, subtotal); // không vượt quá subtotal
            var shippingFee = ShippingFeeFor(_shippingMethod);
            var total = Math.Max(0, subtotal - discount) + shippingFee;

            return new Order(
                Id: Guid.NewGuid().ToString("N"),
                CustomerId: _customerId,
                Items: _items.ToList().AsReadOnly(), // snapshot bất biến
                Subtotal: subtotal,
                Discount: discount,
                ShippingFee: shippingFee,
                Total: total,
                ShippingMethod: _shippingMethod,
                ShippingAddress: _shippingAddress
            );
        }

        private static decimal ShippingFeeFor(ShippingMethod method)
            => method == ShippingMethod.Express ? 15m : 5m;
    }

    public class Program
    {
        public static void Main()
        {
            var order = new OrderBuilder()
                .WithCustomer("CUST001")
                .AddItem("SKU123", 2, 50m)
                .AddItem("SKU999", 1, 120m)
                .WithCoupon("SALE10", 10m)
                .WithShipping(ShippingMethod.Express, "123 Nga Tu So Street")
                .Build();

            Console.WriteLine("=== ORDER DETAILS ===");
            Console.WriteLine($"Order ID: {order.Id}");
            Console.WriteLine($"Customer: {order.CustomerId}");
            Console.WriteLine($"Shipping: {order.ShippingMethod} to {order.ShippingAddress}");
            Console.WriteLine("Items:");
            foreach (var item in order.Items)
            {
                Console.WriteLine($" - {item.Sku} x {item.Quantity} @ {item.UnitPrice:C} = {item.Subtotal:C}");
            }
            Console.WriteLine($"Subtotal: {order.Subtotal:C}");
            Console.WriteLine($"Discount: {order.Discount:C}");
            Console.WriteLine($"Shipping Fee: {order.ShippingFee:C}");
            Console.WriteLine($"Total: {order.Total:C}");
        }
    }
}
