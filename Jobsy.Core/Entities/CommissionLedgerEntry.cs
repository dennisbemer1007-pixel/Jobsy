namespace Jobsy.Core.Entities;

public class CommissionLedgerEntry
{
    public Guid Id { get; set; }
    public Guid SalesManagerUserId { get; set; }
    public User SalesManagerUser { get; set; } = null!;

    public CommissionEntryKind Kind { get; set; }
    public decimal AmountExVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal VatRate { get; set; } = 0.21m;
    public string? Note { get; set; }

    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    /// <summary>Idempotency key for onboarding payment credit.</summary>
    public string? SourcePaymentId { get; set; }

    /// <summary>Idempotency key for token purchase commission.</summary>
    public Guid? SourceTokenCheckoutId { get; set; }

    public Guid? SelfBillingInvoiceId { get; set; }
    public SelfBillingInvoice? SelfBillingInvoice { get; set; }

    public DateTime CreatedAt { get; set; }
}

public enum CommissionEntryKind
{
    FounderBonus = 0,
    TokenCommission = 1,
    Payout = 2,
    Adjustment = 3,
    /// <summary>Passive referral bonus for the SM who referred the primary salesmanager.</summary>
    IndirectTokenCommission = 4
}
