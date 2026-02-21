using System;

namespace DesignPatternChallenge
{
    public interface IPaymentProcessor
    {
        PaymentResult ProcessPayment(PaymentRequest request);
        bool RefundPayment(string transactionId, decimal amount);
        PaymentStatus CheckStatus(string transactionId);
    }

    public class PaymentRequest
    {
        public string CustomerEmail { get; set; }
        public decimal Amount { get; set; }
        public string CreditCardNumber { get; set; }
        public string Cvv { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Description { get; set; }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string Message { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,
        Approved,
        Declined,
        Refunded
    }

    public class LegacyPaymentSystem
    {
        public LegacyTransactionResponse AuthorizeTransaction(
            string cardNum,
            int cvvCode,
            int expMonth,
            int expYear,
            double amountInCents,
            string customerInfo)
        {
            Console.WriteLine($"[Sistema Legado] Autorizando transação...");
            Console.WriteLine($"Cartão: {cardNum}");
            Console.WriteLine($"Valor: {amountInCents / 100:C}");

            return new LegacyTransactionResponse
            {
                AuthCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                ResponseCode = "00",
                ResponseMessage = "TRANSACTION APPROVED",
                TransactionRef = $"LEG{DateTime.Now.Ticks}"
            };
        }

        public bool ReverseTransaction(string transRef, double amountInCents)
        {
            Console.WriteLine($"[Sistema Legado] Revertendo transação {transRef}");
            Console.WriteLine($"Valor: {amountInCents / 100:C}");
            return true;
        }

        public string QueryTransactionStatus(string transRef)
        {
            Console.WriteLine($"[Sistema Legado] Consultando transação {transRef}");
            return "APPROVED";
        }
    }

    public class LegacyTransactionResponse
    {
        public string AuthCode { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public string TransactionRef { get; set; }
    }

    public class LegacyPaymentAdapter : IPaymentProcessor
    {
        private readonly LegacyPaymentSystem _legacySystem;

        public LegacyPaymentAdapter(LegacyPaymentSystem legacySystem)
        {
            _legacySystem = legacySystem;
        }

        public PaymentResult ProcessPayment(PaymentRequest request)
        {
            Console.WriteLine("[Adapter] Convertendo requisição moderna para sistema legado...");

            var response = _legacySystem.AuthorizeTransaction(
                request.CreditCardNumber,
                int.Parse(request.Cvv),
                request.ExpirationDate.Month,
                request.ExpirationDate.Year,
                (double)(request.Amount * 100), // legado usa centavos
                request.CustomerEmail
            );

            return new PaymentResult
            {
                Success = response.ResponseCode == "00",
                TransactionId = response.TransactionRef,
                Message = response.ResponseMessage
            };
        }

        public bool RefundPayment(string transactionId, decimal amount)
        {
            Console.WriteLine("[Adapter] Convertendo reembolso para sistema legado...");

            return _legacySystem.ReverseTransaction(
                transactionId,
                (double)(amount * 100)
            );
        }

        public PaymentStatus CheckStatus(string transactionId)
        {
            Console.WriteLine("[Adapter] Consultando status no sistema legado...");

            var status = _legacySystem.QueryTransactionStatus(transactionId);

            return status switch
            {
                "APPROVED" => PaymentStatus.Approved,
                "DECLINED" => PaymentStatus.Declined,
                "REFUNDED" => PaymentStatus.Refunded,
                _ => PaymentStatus.Pending
            };
        }
    }

    public class ModernPaymentProcessor : IPaymentProcessor
    {
        public PaymentResult ProcessPayment(PaymentRequest request)
        {
            Console.WriteLine("[Processador Moderno] Processando pagamento...");

            return new PaymentResult
            {
                Success = true,
                TransactionId = Guid.NewGuid().ToString(),
                Message = "Pagamento aprovado"
            };
        }

        public bool RefundPayment(string transactionId, decimal amount)
        {
            Console.WriteLine($"[Processador Moderno] Reembolsando {amount:C}");
            return true;
        }

        public PaymentStatus CheckStatus(string transactionId)
        {
            return PaymentStatus.Approved;
        }
    }

    public class CheckoutService
    {
        private readonly IPaymentProcessor _paymentProcessor;

        public CheckoutService(IPaymentProcessor paymentProcessor)
        {
            _paymentProcessor = paymentProcessor;
        }

        public void CompleteOrder(string customerEmail, decimal amount, string cardNumber)
        {
            Console.WriteLine($"\n=== Finalizando Pedido ===");
            Console.WriteLine($"Cliente: {customerEmail}");
            Console.WriteLine($"Valor: {amount:C}\n");

            var request = new PaymentRequest
            {
                CustomerEmail = customerEmail,
                Amount = amount,
                CreditCardNumber = cardNumber,
                Cvv = "123",
                ExpirationDate = new DateTime(2026, 12, 31),
                Description = "Compra de produtos"
            };

            var result = _paymentProcessor.ProcessPayment(request);

            if (result.Success)
                Console.WriteLine($"✅ Pedido aprovado! ID: {result.TransactionId}");
            else
                Console.WriteLine($"❌ Pagamento recusado: {result.Message}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Sistema de Checkout ===\n");

            // Usando processador moderno
            var modernProcessor = new ModernPaymentProcessor();
            var checkoutModern = new CheckoutService(modernProcessor);

            checkoutModern.CompleteOrder(
                "cliente@email.com",
                150.00m,
                "4111111111111111"
            );

            Console.WriteLine("\n" + new string('-', 50));

            // Usando sistema legado via Adapter
            var legacySystem = new LegacyPaymentSystem();
            var legacyAdapter = new LegacyPaymentAdapter(legacySystem);

            var checkoutLegacy = new CheckoutService(legacyAdapter);

            checkoutLegacy.CompleteOrder(
                "cliente2@email.com",
                200.00m,
                "4111111111111111"
            );
        }
    }
}