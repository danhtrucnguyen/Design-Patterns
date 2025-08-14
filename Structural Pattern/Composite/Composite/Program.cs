using System;
using System.Collections.Generic;
using System.Linq;

namespace Design_Patterns.Structural_Pattern
{
    public interface ICartComponent
    {
        decimal GetPrice();
    }

    // Leaf
    public sealed record CartItem(string Sku, int Quantity, decimal UnitPrice) : ICartComponent
    {
        public decimal GetPrice()
        {
            if (Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(Quantity));
            if (UnitPrice < 0) throw new ArgumentOutOfRangeException(nameof(UnitPrice));
            return Quantity * UnitPrice;
        }
    }

    // Composite
    public sealed class CartBundle : ICartComponent
    {
        private readonly List<ICartComponent> _children = new();

        public string Name { get; }
        public decimal DiscountAmount { get; } // giảm giá tuyệt đối áp cho toàn gói

        public CartBundle(string name, decimal discountAmount = 0m)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Bundle name is required");
            if (discountAmount < 0) throw new ArgumentOutOfRangeException(nameof(discountAmount));
            Name = name.Trim();
            DiscountAmount = discountAmount;
        }

        public CartBundle Add(ICartComponent child)
        {
            _children.Add(child ?? throw new ArgumentNullException(nameof(child)));
            return this;
        }

        public bool Remove(ICartComponent child) => _children.Remove(child);

        public IReadOnlyList<ICartComponent> Children => _children.AsReadOnly();

        public decimal GetPrice()
        {
            var sum = _children.Sum(c => c.GetPrice());
            var result = Math.Max(0, sum - DiscountAmount);
            return result;
        }
    }

    public class Program
    {
        public static void Main()
        {
            // Sản phẩm lẻ (Leaf)
            var item1 = new CartItem("A001", 2, 50m);    // 2 x 50 = 100
            var item2 = new CartItem("B002", 1, 200m);   // 1 x 200 = 200
            var item3 = new CartItem("C003", 3, 30m);    // 3 x 30 = 90

            // Gói combo (Composite)
            var bundle1 = new CartBundle("Combo 1", discountAmount: 20m)
                .Add(item1)
                .Add(item2); // tổng 300 - 20 = 280

            var bundle2 = new CartBundle("Combo 2", discountAmount: 15m)
                .Add(item3); // tổng 90 - 15 = 75

            // Giỏ hàng chính (Composite)
            var cart = new CartBundle("Shopping Cart")
                .Add(bundle1)
                .Add(bundle2);

            Console.WriteLine("=== Cart Details ===");
            Console.WriteLine($"Bundle 1 Price: {bundle1.GetPrice()}");
            Console.WriteLine($"Bundle 2 Price: {bundle2.GetPrice()}");
            Console.WriteLine($"Total Cart Price: {cart.GetPrice()}");
        }
    }
}
