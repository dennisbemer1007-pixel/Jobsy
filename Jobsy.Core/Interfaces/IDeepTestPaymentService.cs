using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public enum DeepTestFulfillSource
{
    Webhook = 0,
    Reconcile = 1,
    Return = 2,
    Stub = 3
}

public interface IDeepTestPaymentService
{
    Task<DeepTestCheckoutCreateResult> CreateCheckoutAsync(
        Guid userId,
        AssessmentKind kind,
        bool waiverAccepted,
        string? locale,
        CancellationToken cancellationToken = default);

    Task<DeepTestCheckoutStatusDto> GetStatusAsync(
        Guid userId,
        Guid checkoutId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Idempotent fulfillment. Sets Paid only after Mollie says paid (or stub when allowed + source Stub).
    /// </summary>
    Task<DeepTestFulfillResult> TryFulfillAsync(
        Guid checkoutId,
        DeepTestFulfillSource source,
        CancellationToken cancellationToken = default);

    Task<string> GetPaymentModeAsync(CancellationToken cancellationToken = default);
}

public sealed record DeepTestCheckoutCreateResult(
    Guid CheckoutId,
    string CheckoutUrl,
    int TotalCents,
    bool IsStub,
    AssessmentKind Kind,
    string PaymentId);

public sealed record DeepTestCheckoutStatusDto(
    Guid CheckoutId,
    string Status,
    AssessmentKind Kind,
    string TestSlug,
    int TotalCents,
    DateTime? PaidAtUtc,
    string? InvoiceNumber,
    Guid? InvoiceId,
    bool IsStub,
    string? PaymentMethod);

public sealed record DeepTestFulfillResult(
    bool Changed,
    string Status,
    Guid CheckoutId,
    bool Unlocked,
    string? InvoiceNumber);
