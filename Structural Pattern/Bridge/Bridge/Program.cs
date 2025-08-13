using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Design_Patterns.Structural_Pattern { 
    public sealed class ReportContent
    {
        public string Title { get; }
        public List<string> Lines { get; } = new();
        public ReportContent(string title, IEnumerable<string>? lines = null)
        {
            Title = title;
            if (lines != null) Lines.AddRange(lines);
        }
    }

    // Implementation
    public interface IReportRenderer
    {
        string Render(ReportContent content);
    }

    public sealed class HtmlRenderer : IReportRenderer
    {
        public string Render(ReportContent c)
        {
            var body = string.Join("", c.Lines.Select(l => $"<li>{System.Net.WebUtility.HtmlEncode(l)}</li>"));
            return $"<html><head><title>{c.Title}</title></head><body><h1>{c.Title}</h1><ul>{body}</ul></body></html>";
        }
    }

    public sealed class PdfRenderer : IReportRenderer
    {
        public string Render(ReportContent c)
        {
            var body = string.Join(Environment.NewLine, c.Lines.Select(l => $"• {l}"));
            return $"PDF\nTITLE: {c.Title}\n{body}";
        }
    }

    // Abstraction
    public abstract class Report
    {
        private IReportRenderer _renderer;
        protected Report(IReportRenderer renderer) => _renderer = renderer;

        public void SetRenderer(IReportRenderer r) => _renderer = r ?? throw new ArgumentNullException(nameof(r));

        public string Export()
        {
            var content = Compose();
            return _renderer.Render(content);
        }

        protected abstract ReportContent Compose();

        protected static string Money(decimal v, string culture = "en-US")
            => v.ToString("C", CultureInfo.CreateSpecificCulture(culture));
    }

    // Refined Abstractions
    public sealed class SalesReport : Report
    {
        private readonly IReadOnlyList<decimal> _amounts;
        private readonly string _currencyCulture;

        public SalesReport(IReportRenderer renderer, IEnumerable<decimal> amounts, string currencyCulture = "en-US")
            : base(renderer)
        {
            _amounts = amounts?.ToList() ?? throw new ArgumentNullException(nameof(amounts));
            _currencyCulture = currencyCulture;
        }

        protected override ReportContent Compose()
        {
            var total = _amounts.Sum();
            var avg = _amounts.Count == 0 ? 0 : _amounts.Average();
            var lines = new List<string>
            {
                $"Orders: {_amounts.Count}",
                $"Total Revenue: {Money(total, _currencyCulture)}",
                $"Average Order: {Money(avg, _currencyCulture)}"
            };
            return new ReportContent("Sales Report", lines);
        }
    }

    public sealed class InventoryReport : Report
    {
        private readonly IReadOnlyDictionary<string, int> _stock;
        public InventoryReport(IReportRenderer renderer, IDictionary<string, int> stock)
            : base(renderer)
        {
            _stock = new Dictionary<string, int>(stock ?? throw new ArgumentNullException(nameof(stock)));
        }

        protected override ReportContent Compose()
        {
            var totalSku = _stock.Count;
            var totalQty = _stock.Values.Sum();
            var lines = new List<string> { $"SKUs: {totalSku}", $"Total Quantity: {totalQty}" };
            foreach (var kv in _stock.OrderBy(k => k.Key))
                lines.Add($"{kv.Key}: {kv.Value}");
            return new ReportContent("Inventory Report", lines);
        }
    }

    public class Program
    {
        public static void Main()
        {
            var salesData = new List<decimal> { 100m, 250.50m, 75m };
            var stockData = new Dictionary<string, int>
            {
                { "A001", 50 },
                { "B002", 20 },
                { "C003", 0 }
            };

            // Renderer HTML
            IReportRenderer htmlRenderer = new HtmlRenderer();

            // Renderer PDF 
            IReportRenderer pdfRenderer = new PdfRenderer();

            //xuất ra HTML
            Report salesReportHtml = new SalesReport(htmlRenderer, salesData, "en-US");
            Console.WriteLine("=== Sales Report (HTML) ===");
            Console.WriteLine(salesReportHtml.Export());

            //xuất ra PDF
            salesReportHtml.SetRenderer(pdfRenderer);
            Console.WriteLine("\n=== Sales Report (PDF) ===");
            Console.WriteLine(salesReportHtml.Export());

            // Inventory report xuất ra HTML
            Report inventoryReportHtml = new InventoryReport(htmlRenderer, stockData);
            Console.WriteLine("\n=== Inventory Report (HTML) ===");
            Console.WriteLine(inventoryReportHtml.Export());

            // Inventory report xuất ra PDF
            inventoryReportHtml.SetRenderer(pdfRenderer);
            Console.WriteLine("\n=== Inventory Report (PDF) ===");
            Console.WriteLine(inventoryReportHtml.Export());
        }
    }
}
