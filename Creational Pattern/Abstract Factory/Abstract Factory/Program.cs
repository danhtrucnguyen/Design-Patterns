using System;

namespace Design_Patterns.Creational_Pattern
{
    // Contracts
    public record PaymentRequest(decimal Amount, string Currency);
    public record PaymentResult(bool Success, string Provider, string Message);

    public interface IPaymentProcessor
    {
        string Name { get; }
        PaymentResult Process(PaymentRequest req);
    }

    public interface IFraudChecker
    {
        bool IsSuspicious(PaymentRequest req);
    }

    public interface IReceiptFormatter
    {
        string Format(PaymentResult res);
    }

    public interface IPaymentSuiteFactory
    {
        IPaymentProcessor CreateProcessor();
        IFraudChecker CreateFraudChecker();
        IReceiptFormatter CreateReceiptFormatter();
    }

    // Visa concrete products
    public sealed class VisaProcessor : IPaymentProcessor
    {
        public string Name => "VISA";
        public PaymentResult Process(PaymentRequest req)
        {
            if (req.Amount <= 0) return new(false, Name, "Don hang khong hop le");
            return new(true, Name, $"Da tinh phi {req.Amount} {req.Currency} qua VISA");
        }
    }

    public sealed class VisaFraudChecker : IFraudChecker
    {
        public bool IsSuspicious(PaymentRequest req) => req.Amount > 10_000m; // demo rule
    }

    public sealed class VisaReceiptFormatter : IReceiptFormatter
    {
        public string Format(PaymentResult res) =>
            res.Success ? $"[VISA RECEIPT] {res.Message}" : $"[VISA FAIL] {res.Message}";
    }

    public sealed class VisaSuiteFactory : IPaymentSuiteFactory
    {
        public IPaymentProcessor CreateProcessor() => new VisaProcessor();
        public IFraudChecker CreateFraudChecker() => new VisaFraudChecker();
        public IReceiptFormatter CreateReceiptFormatter() => new VisaReceiptFormatter();
    }

    //Paypal concrete products
    public sealed class PaypalProcessor : IPaymentProcessor
    {
        public string Name => "PAYPAL";
        public PaymentResult Process(PaymentRequest req)
        {
            if (req.Amount <= 0) return new(false, Name, "Don hang khong hop le");
            return new(true, Name, $"Da tinh phi {req.Amount} {req.Currency} qua PayPal");
        }
    }

    public sealed class PaypalFraudChecker : IFraudChecker
    {
        public bool IsSuspicious(PaymentRequest req) => req.Amount > 8_000m; 
    }

    public sealed class PaypalReceiptFormatter : IReceiptFormatter
    {
        public string Format(PaymentResult res) =>
            res.Success ? $"<PAYPAL RECEIPT> {res.Message}" : $"<PAYPAL FAIL> {res.Message}";
    }

    public sealed class PaypalSuiteFactory : IPaymentSuiteFactory
    {
        public IPaymentProcessor CreateProcessor() => new PaypalProcessor();
        public IFraudChecker CreateFraudChecker() => new PaypalFraudChecker();
        public IReceiptFormatter CreateReceiptFormatter() => new PaypalReceiptFormatter();
    }

    // Client
    public sealed class CheckoutService
    {
        private readonly IPaymentSuiteFactory _factory;

        public CheckoutService(IPaymentSuiteFactory factory)
        {
            _factory = factory;
        }

        public string PayAndPrintReceipt(PaymentRequest req)
        {
            var fraud = _factory.CreateFraudChecker();
            if (fraud.IsSuspicious(req))
                return "GIAO DICH KHONG THANH CONG: Thanh toan vuot muc gioi han";

            var processor = _factory.CreateProcessor();
            var result = processor.Process(req);

            var formatter = _factory.CreateReceiptFormatter();
            return formatter.Format(result);
        }
    }

    public class Program
    {
        public static void Main()
        {
            var request = new PaymentRequest(5000m, "USD");

            //VISA
            var visaService = new CheckoutService(new VisaSuiteFactory());
            Console.WriteLine("=== VISA ===");
            Console.WriteLine(visaService.PayAndPrintReceipt(request));

            //PAYPAL
            var paypalService = new CheckoutService(new PaypalSuiteFactory());
            Console.WriteLine("=== PAYPAL ===");
            Console.WriteLine(paypalService.PayAndPrintReceipt(request));

            //Chan giao dich
            var bigRequest = new PaymentRequest(15000m, "USD");
            Console.WriteLine("=== VISA BLOCKED ===");
            Console.WriteLine(visaService.PayAndPrintReceipt(bigRequest));
        }
    }
}
