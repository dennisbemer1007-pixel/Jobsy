namespace Jobsy.Core.Interfaces;

public interface IPaymentService
{
    /// <param name="paymentMethod">
    /// Optional Mollie method (<c>ideal</c> / <c>creditcard</c>). When null, the company's
    /// preferred method is used when set; otherwise Mollie Checkout offers primary methods.
    /// </param>
    Task<PaymentCheckoutResult> CreateTokenPurchaseCheckoutAsync(
        Guid companyId,
        int packSize,
        string? paymentMethod = null,
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
    bool IsStub,
    Guid CheckoutId = default,
    string? PaymentMethod = null);

public record PaymentStatusResult(
    string PaymentId,
    string Status,
    bool IsPaid,
    string? Method = null);
