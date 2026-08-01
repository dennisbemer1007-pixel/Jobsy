using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

public class TokenTransaction
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary>
    /// Positive for purchase/grant/goodwill/in; negative for spend/out.
    /// Fractional tokens are supported (highlight spends typically 1–2 tokens).
    /// </summary>
    public decimal Amount { get; set; }

    public TokenTransactionKind Kind { get; set; }
    public TokenSpendReason Reason { get; set; } = TokenSpendReason.None;
    public decimal OldBalance { get; set; }
    public decimal NewBalance { get; set; }
    public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public Guid? VacancyId { get; set; }
    public Vacancy? Vacancy { get; set; }
    public Guid? BranchCompanyId { get; set; }
    public Company? BranchCompany { get; set; }

    /// <summary>
    /// Administrative note / reason. Required for Goodwill tokens.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>Ex-BTW amount in whole cents (0 for goodwill/grants/spend).</summary>
    public int AmountExVatCents { get; set; }

    /// <summary>BTW amount in whole cents (21% on purchases; 0 otherwise).</summary>
    public int VatAmountCents { get; set; }

    /// <summary>Total incl. BTW in whole cents (0 for goodwill).</summary>
    public int TotalAmountCents { get; set; }

    public Guid? TokenPurchaseCheckoutId { get; set; }
    public TokenPurchaseCheckout? TokenPurchaseCheckout { get; set; }

    public Guid? TokenPurchaseInvoiceId { get; set; }
    public TokenPurchaseInvoice? TokenPurchaseInvoice { get; set; }

    public DateTime CreatedAt { get; set; }
}
