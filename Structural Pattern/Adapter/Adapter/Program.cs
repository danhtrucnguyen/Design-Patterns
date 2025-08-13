using System;

namespace Design_Patterns.Structural_Pattern
{
    public record PaymentRequest(decimal Amount, string Currency);
    public record PaymentResult(bool Success, string Provider, string Message);

    public interface IPaymentProcessor
    {
        string Name { get; }
        PaymentResult Process(PaymentRequest req);
    }

    //Adaptee 
    public record LegacyResponse(int Code, string Text);
    public interface ILegacyPaymentGateway
    {
        // Ví dụ: yêu cầu "cents" & code nguyên thuỷ
        LegacyResponse Pay(int amountInCents, string currencyCode);
    }

    public sealed class OldPayGateway : ILegacyPaymentGateway
    {
        public LegacyResponse Pay(int amountInCents, string currencyCode)
        {
            if (amountInCents <= 0) return new LegacyResponse(400, "invalid_amount");
            if (string.IsNullOrWhiteSpace(currencyCode)) return new LegacyResponse(422, "invalid_currency");

            return new LegacyResponse(200, $"ok:{amountInCents}:{currencyCode.ToUpperInvariant()}");
        }
    }

    //Adapter 
    public sealed class GatewayAdapter : IPaymentProcessor
    {
        private readonly ILegacyPaymentGateway _gateway;
        public string Name { get; }

        public GatewayAdapter(string providerName, ILegacyPaymentGateway gateway)
        {
            Name = providerName ?? "LEGACY";
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        public PaymentResult Process(PaymentRequest req)
        {
            // Chuyển đổi decimal -> cents (tránh overflow đơn giản hoá demo)
            var cents = checked((int)Math.Round(req.Amount * 100m, MidpointRounding.AwayFromZero));
            var code = NormalizeCurrency(req.Currency);

            var resp = _gateway.Pay(cents, code);
            return Map(resp);
        }

        private static string NormalizeCurrency(string c)
            => string.IsNullOrWhiteSpace(c) ? "USD" : c.Trim().ToUpperInvariant();

        private PaymentResult Map(LegacyResponse r) => r.Code switch
        {
            200 => new(true, Name, $"Charged via {Name}: {r.Text}"),
            400 => new(false, Name, "Amount is invalid"),
            401 => new(false, Name, "Unauthorized"),
            422 => new(false, Name, "Currency is invalid"),
            500 => new(false, Name, "Gateway error"),
            _ => new(false, Name, $"Unknown error ({r.Code}): {r.Text}")
        };
    }
    public class Program
    {
        public static void Main()
        {
            // Adaptee (hệ thống cũ)
            ILegacyPaymentGateway oldGateway = new OldPayGateway();

            // Adapter để dùng được trong chuẩn mới
            IPaymentProcessor processor = new GatewayAdapter("OldPay", oldGateway);

            // Test 1: Thanh toán thành công
            var req1 = new PaymentRequest(50.75m, "usd");
            var res1 = processor.Process(req1);
            Console.WriteLine("=== Payment 1 ===");
            Console.WriteLine(res1);

            // Test 2: Số tiền không hợp lệ
            var req2 = new PaymentRequest(0, "usd");
            var res2 = processor.Process(req2);
            Console.WriteLine("=== Payment 2 ===");
            Console.WriteLine(res2);

            // Test 3: Thiếu mã tiền tệ
            var req3 = new PaymentRequest(20, "");
            var res3 = processor.Process(req3);
            Console.WriteLine("=== Payment 3 ===");
            Console.WriteLine(res3);
        }
    }
}
