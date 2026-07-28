namespace Jobsy.Core.Entities;

/// <summary>
/// Mollie (stub) payout session for salesmanager self-billing settlement.
/// Pending → Paid → Completed (invoice issued + marked paid + ledger payout).
/// </summary>
public class SalesManagerPayoutCheckout
{
    public Guid Id { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public Guid SalesManagerUserId { get; set; }
    public User SalesManagerUser { get; set; } = null!;

    /// <summary>Snapshot of expected payout incl. VAT at checkout creation.</summary>
    public decimal AmountEuro { get; set; }
    public decimal AmountExVat { get; set; }
    public decimal VatAmount { get; set; }

    /// <summary>Masked IBAN shown in UI/logs (never full account).</summary>
    public string MaskedIban { get; set; } = string.Empty;

    public SalesManagerPayoutCheckoutStatus Status { get; set; } = SalesManagerPayoutCheckoutStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? SelfBillingInvoiceId { get; set; }
}

public enum SalesManagerPayoutCheckoutStatus
{
    Pending = 0,
    Paid = 1,
    Completed = 2,
    Cancelled = 3
}
