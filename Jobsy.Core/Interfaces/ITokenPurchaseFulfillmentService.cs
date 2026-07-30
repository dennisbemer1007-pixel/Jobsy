using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Orchestrates token credit + invoice + BTW-buffer queue after a successful Mollie payment.
/// </summary>
public interface ITokenPurchaseFulfillmentService
{
    /// <summary>
    /// Idempotently credits tokens, creates an official invoice, and queues the BTW buffer transfer.
    /// Returns null when the checkout is not (yet) paid.
    /// </summary>
    Task<TokenPurchaseFulfillmentResult?> TryFulfillPaidCheckoutAsync(
        Guid checkoutId,
        Guid? actorUserId = null,
        bool allowDevStubMarkPaid = false,
        CancellationToken cancellationToken = default);
}

public sealed record TokenPurchaseFulfillmentResult(
    Guid CheckoutId,
    Guid CompanyId,
    string CompanyName,
    decimal NewBalance,
    Guid TokenTransactionId,
    Guid InvoiceId,
    string InvoiceNumber,
    bool AlreadyFulfilled);
