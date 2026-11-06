namespace Jobsy.Core.Entities;

public class CommissionLedgerEntry
{
    public Guid Id { get; set; }

    /// <summary>
    /// Beneficiary user id for SalesManager or Ambassadeur commission rows.
    /// Named historically for SalesManager; Ambassadeur payouts reuse the same ledger.
    /// </summary>
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

    /// <summary>When a hold ends and the amount becomes available (Europe/Amsterdam day boundary as UTC).</summary>
    public DateTime AvailableFromUtc { get; set; }

    public Guid? SalesPayoutRequestId { get; set; }
    public SalesPayoutRequest? SalesPayoutRequest { get; set; }

    /// <summary>Self-FK to the ledger line this correction reverses.</summary>
    public Guid? CorrectsEntryId { get; set; }
    public CommissionLedgerEntry? CorrectsEntry { get; set; }

    /// <summary>Idempotency key for Mollie refund/chargeback corrections (≤ 80).</summary>
    public string? SourceRefundKey { get; set; }

    public string? Reason { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}

public enum CommissionEntryKind
{
    FounderBonus = 0,
    TokenCommission = 1,
    Payout = 2,
    Adjustment = 3,
    /// <summary>Passive referral bonus for the SM who referred the primary salesmanager.</summary>
    IndirectTokenCommission = 4,
    RefundCorrection = 5,
    ChargebackCorrection = 6
}
