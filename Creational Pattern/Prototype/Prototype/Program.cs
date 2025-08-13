using System;
using System.Collections.Generic;
using System.Linq;

namespace Patterns.Creational.Prototype
{
    public interface IDeepCloneable<T>
    {
        T DeepClone();
    }

    public sealed class MediaAsset : IDeepCloneable<MediaAsset>
    {
        public string Url { get; init; }
        public byte[]? Bytes { get; init; }

        public MediaAsset(string url, byte[]? bytes = null)
        {
            Url = url;
            Bytes = bytes;
        }

        public MediaAsset DeepClone()
            => new MediaAsset(Url, Bytes is null ? null : Bytes.ToArray());
    }

    public sealed class ProductTemplate : IDeepCloneable<ProductTemplate>
    {
        public string Sku { get; init; }
        public string Name { get; init; }
        public decimal BasePrice { get; init; }
        public string Category { get; init; }
        public List<string> Tags { get; init; }
        public Dictionary<string, string> Attributes { get; init; }
        public MediaAsset? Media { get; init; }

        public ProductTemplate(
            string sku,
            string name,
            decimal basePrice,
            string category,
            List<string>? tags = null,
            Dictionary<string, string>? attributes = null,
            MediaAsset? media = null)
        {
            Sku = sku;
            Name = name;
            BasePrice = basePrice;
            Category = category;
            Tags = tags ?? new();
            Attributes = attributes ?? new();
            Media = media;
        }

        // Shallow: copy tham chiếu
        public ProductTemplate ShallowClone()
            => (ProductTemplate)MemberwiseClone();

        // Deep: sao chép dữ liệu lồng nhau
        public ProductTemplate DeepClone()
            => new ProductTemplate(
                sku: Sku,
                name: Name,
                basePrice: BasePrice,
                category: Category,
                tags: new List<string>(Tags),
                attributes: new Dictionary<string, string>(Attributes),
                media: Media?.DeepClone()
            );
    }

    public static class CatalogService
    {
        // Tạo biến thể từ template: clone rồi chỉnh khác biệt
        public static ProductTemplate CreateVariant(ProductTemplate from, string skuSuffix, decimal deltaPrice)
        {
            if (string.IsNullOrWhiteSpace(skuSuffix))
                throw new ArgumentException("skuSuffix is required");

            var variant = from.DeepClone();

            variant.Attributes["variant"] = skuSuffix;
            variant.Tags.Add("variant");

            return new ProductTemplate(
                sku: $"{from.Sku}-{skuSuffix}".ToUpperInvariant(),
                name: from.Name,
                basePrice: Math.Max(0, from.BasePrice + deltaPrice),
                category: from.Category,
                tags: variant.Tags,
                attributes: variant.Attributes,
                media: variant.Media
            );
        }
    }

    public class Program
    {
        public static void Main()
        {
            // Sản phẩm gốc
            var original = new ProductTemplate(
                sku: "TSHIRT001",
                name: "T-Shirt",
                basePrice: 20m,
                category: "Clothing",
                tags: new List<string> { "cotton", "unisex" },
                attributes: new Dictionary<string, string> { { "size", "M" }, { "color", "white" } },
                media: new MediaAsset("tshirt.jpg", new byte[] { 1, 2, 3 })
            );

            // Tạo biến thể từ sản phẩm gốc
            var variant = CatalogService.CreateVariant(original, "RED-L", 5m);

            // In sản phẩm gốc
            Console.WriteLine("=== ORIGINAL PRODUCT ===");
            PrintProduct(original);

            // In biến thể
            Console.WriteLine("\n=== VARIANT PRODUCT ===");
            PrintProduct(variant);

            // Thử thay đổi biến thể để chứng minh Deep Clone không ảnh hưởng tới gốc
            variant.Attributes["color"] = "red";
            variant.Tags.Add("new-tag");

            Console.WriteLine("\n=== AFTER MODIFYING VARIANT ===");
            Console.WriteLine("Original color: " + original.Attributes["color"]);
            Console.WriteLine("Variant color: " + variant.Attributes["color"]);
        }

        private static void PrintProduct(ProductTemplate p)
        {
            Console.WriteLine($"SKU: {p.Sku}");
            Console.WriteLine($"Name: {p.Name}");
            Console.WriteLine($"Price: {p.BasePrice:C}");
            Console.WriteLine($"Category: {p.Category}");
            Console.WriteLine("Tags: " + string.Join(", ", p.Tags));
            Console.WriteLine("Attributes:");
            foreach (var kv in p.Attributes)
                Console.WriteLine($"  - {kv.Key}: {kv.Value}");
            Console.WriteLine($"Media: {p.Media?.Url}");
        }
    }
}
