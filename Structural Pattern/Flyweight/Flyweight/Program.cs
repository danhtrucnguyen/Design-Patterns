using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Design_Patterns.Structural_Pattern
{
    // Intrinsic state (bất biến, có thể chia sẻ)
    public sealed class ProductShared
    {
        public string Sku { get; }
        public string Name { get; }
        public string Brand { get; }
        public byte[] ImageBytes { get; } // giả lập dữ liệu nặng
        public IReadOnlyDictionary<string, string> Attributes { get; }

        public ProductShared(string sku, string name, string brand, byte[] imageBytes,
            IDictionary<string, string>? attributes = null)
        {
            Sku = sku ?? throw new ArgumentNullException(nameof(sku));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Brand = brand ?? throw new ArgumentNullException(nameof(brand));
            ImageBytes = imageBytes ?? throw new ArgumentNullException(nameof(imageBytes));
            Attributes = new Dictionary<string, string>(attributes ?? new Dictionary<string, string>());
        }
    }

    //Extrinsic state (truyền lúc dùng)
    public sealed record ProductViewContext(
        string StoreId, decimal Price, int Stock, string Variant, int GridX, int GridY
    );

    //Flyweight
    public interface IProductFlyweight
    {
        string Sku { get; }
        string Render(ProductViewContext ctx); // demo: trả về chuỗi render
    }

    public sealed class ProductFlyweight : IProductFlyweight
    {
        private readonly ProductShared _shared;
        public string Sku => _shared.Sku;

        public ProductFlyweight(ProductShared shared) => _shared = shared ?? throw new ArgumentNullException(nameof(shared));

        // Ở thực tế có thể render UI; demo trả về text gộp intrinsic + extrinsic
        public string Render(ProductViewContext ctx)
        {
            return $"[{ctx.GridX},{ctx.GridY}] {_shared.Brand} {_shared.Name} ({_shared.Sku}) " +
                   $"Variant={ctx.Variant} Price={ctx.Price} Stock={ctx.Stock} Store={ctx.StoreId} " +
                   $"ImgBytes={_shared.ImageBytes.Length}";
        }
    }

    // ----- Factory (thread-safe, đảm bảo chỉ khởi tạo 1 lần/sku) -----
    public sealed class FlyweightFactory
    {
        private readonly ConcurrentDictionary<string, Lazy<IProductFlyweight>> _cache = new(StringComparer.OrdinalIgnoreCase);

        public IProductFlyweight GetOrCreate(string sku, Func<string, ProductShared> loader)
        {
            if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU is required");
            if (loader is null) throw new ArgumentNullException(nameof(loader));

            var lazy = _cache.GetOrAdd(sku, key =>
                new Lazy<IProductFlyweight>(() => new ProductFlyweight(loader(key)), LazyThreadSafetyMode.ExecutionAndPublication));

            return lazy.Value;
        }

        public int Count => _cache.Count;
        public bool TryGet(string sku, out IProductFlyweight? fw)
        {
            fw = null;
            if (_cache.TryGetValue(sku, out var lazy) && lazy.IsValueCreated)
            {
                fw = lazy.Value;
                return true;
            }
            return false;
        }
    }
    public class Program
    {
        public static void Main()
        {
            // Tạo factory
            var factory = new FlyweightFactory();

            // Hàm giả lập tải dữ liệu sản phẩm nặng
            ProductShared Loader(string sku)
            {
                Console.WriteLine($"[Loader] Loading product data for {sku}...");
                var img = new byte[1024]; // giả lập ảnh 1KB
                new Random().NextBytes(img);
                return new ProductShared(
                    sku,
                    name: $"Product {sku}",
                    brand: "BrandX",
                    imageBytes: img,
                    attributes: new Dictionary<string, string>
                    {
                        ["Color"] = "Red",
                        ["Size"] = "M"
                    }
                );
            }

            // Các SKU lặp lại để kiểm tra Flyweight
            var skus = new[] { "SKU-001", "SKU-002", "SKU-001", "SKU-003", "SKU-002" };

            // Render sản phẩm ở các vị trí khác nhau (extrinsic state)
            int y = 0;
            foreach (var sku in skus)
            {
                var fw = factory.GetOrCreate(sku, Loader);

                var ctx = new ProductViewContext(
                    StoreId: "Store-01",
                    Price: new Random().Next(10, 100),
                    Stock: new Random().Next(1, 50),
                    Variant: "Default",
                    GridX: 0,
                    GridY: y++
                );

                Console.WriteLine(fw.Render(ctx));
            }

            Console.WriteLine($"\nFlyweight objects created: {factory.Count}");
        }
    }
}

