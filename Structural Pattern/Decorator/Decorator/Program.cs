using System;
using System.Collections.Generic;
using System.Linq;

namespace Design_Patterns.Structural_Pattern
{
    public enum ShippingMethod { Standard, Express }

    public sealed record OrderItem(string Sku, int Quantity, decimal UnitPrice)
    {
        public decimal Subtotal => Quantity * UnitPrice;
    }

    public sealed class Order
    {
        public List<OrderItem> Items { get; } = new();
        public string Country { get; init; } = "US";
        public ShippingMethod ShippingMethod { get; init; } = ShippingMethod.Standard;
    }

    // Target
    public interface IPriceCalculator
    {
        decimal Calculate(Order o);
    }

    // Concrete Component
    public sealed class BasePriceCalculator : IPriceCalculator
    {
        public decimal Calculate(Order o)
        {
            if (o is null) throw new ArgumentNullException(nameof(o));
            var subtotal = o.Items.Sum(i =>
            {
                if (i.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(i.Quantity));
                if (i.UnitPrice < 0) throw new ArgumentOutOfRangeException(nameof(i.UnitPrice));
                return i.Subtotal;
            });
            return subtotal;
        }
    }

    // Base Decorator
    public abstract class PriceDecorator : IPriceCalculator
    {
        protected readonly IPriceCalculator Inner;
        protected PriceDecorator(IPriceCalculator inner)
            => Inner = inner ?? throw new ArgumentNullException(nameof(inner));

        public virtual decimal Calculate(Order o) => Inner.Calculate(o);
    }

    public sealed class ShippingDecorator : PriceDecorator
    {
        public ShippingDecorator(IPriceCalculator inner) : base(inner) { }
        public override decimal Calculate(Order o)
        {
            var baseTotal = base.Calculate(o);
            var fee = o.ShippingMethod == ShippingMethod.Express ? 15m : 5m;
            return baseTotal + fee;
        }
    }

    public sealed class TaxDecorator : PriceDecorator
    {
        private readonly Func<Order, decimal> _rateProvider; // 0.08 = 8%
        public TaxDecorator(IPriceCalculator inner, Func<Order, decimal> rateProvider)
            : base(inner)
        {
            _rateProvider = rateProvider ?? (_ => 0m);
        }

        public override decimal Calculate(Order o)
        {
            var baseTotal = base.Calculate(o);
            var rate = Math.Clamp(_rateProvider(o), 0m, 1m);
            return baseTotal + baseTotal * rate;
        }
    }

    public sealed class CouponPercentDecorator : PriceDecorator
    {
        private readonly decimal _percent; // 0.10 = 10%
        public CouponPercentDecorator(decimal percent, IPriceCalculator inner) : base(inner)
        {
            _percent = Math.Clamp(percent, 0m, 1m);
        }
        public override decimal Calculate(Order o)
        {
            var baseTotal = base.Calculate(o);
            var discount = baseTotal * _percent;
            var result = baseTotal - discount;
            return result < 0 ? 0 : result;
        }
    }

    public class Program
    {
        public static void Main()
        {
            // Tạo đơn hàng
            var order = new Order
            {
                Country = "US",
                ShippingMethod = ShippingMethod.Express
            };
            order.Items.Add(new OrderItem("SKU-001", 2, 50m)); // 2 sản phẩm giá 50
            order.Items.Add(new OrderItem("SKU-002", 1, 100m)); // 1 sản phẩm giá 100

            // Tạo calculator cơ bản
            IPriceCalculator calculator = new BasePriceCalculator();

            // Thêm phí ship
            calculator = new ShippingDecorator(calculator);

            // Thêm thuế (8%)
            calculator = new TaxDecorator(calculator, o => 0.08m);

            // Thêm mã giảm giá 10%
            calculator = new CouponPercentDecorator(0.10m, calculator);

            // Tính tổng
            decimal total = calculator.Calculate(order);

            Console.WriteLine($"Tong tien thanh toan: {total:C}");
        }
    }
}