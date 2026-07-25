namespace Jobsy.Core.Interfaces;

public interface IPaymentService
{
    Task<PaymentCheckoutResult> CreateTokenPurchaseCheckoutAsync(
        Guid companyId,
        int packSize,
        CancellationToken cancellationToken = default);

    Task<PaymentStatusResult> GetPaymentStatusAsync(
        string paymentId,
        CancellationToken cancellationToken = default);
}

public record PaymentCheckoutResult(
    string PaymentId,
    string CheckoutUrl,
    int PackSize,
    decimal AmountEuro,
    bool IsStub);

public record PaymentStatusResult(
    string PaymentId,
    string Status,
    bool IsPaid);
