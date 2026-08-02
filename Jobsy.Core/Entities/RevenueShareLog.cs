namespace Jobsy.Core.Entities;

/// <summary>
/// Immutable financial log for token-purchase revenue share
/// (ambassador + direct SM + optional indirect SM + platform remainder).
/// </summary>
public class RevenueShareLog
{
    public Guid Id { get; set; }

    /// <summary>Token purchase checkout that triggered this split.</summary>
    public Guid TokenCheckoutId { get; set; }

    /// <summary>Primary purchase ledger row (tokens credited to buyer).</summary>
    public Guid? TokenTransactionId { get; set; }

    /// <summary>Company that purchased the tokens.</summary>
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    /// <summary>Recipient company (ambassador rebate) when applicable.</summary>
    public Guid? RecipientCompanyId { get; set; }
    public Company? RecipientCompany { get; set; }

    /// <summary>Recipient user (salesmanager) when applicable.</summary>
    public Guid? RecipientUserId { get; set; }
    public User? RecipientUser { get; set; }

    public RevenueShareRecipientKind RecipientKind { get; set; }

    /// <summary>Share percentage (e.g. 15, 5, 80).</summary>
    public decimal Percentage { get; set; }

    /// <summary>Euro amount of this share (ex-VAT basis of purchase).</summary>
    public decimal AmountEuro { get; set; }

    /// <summary>Token amount credited when this share is paid in tokens.</summary>
    public decimal? Tokens { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public enum RevenueShareRecipientKind
{
    Ambassador = 0,
    SalesManager = 1,
    Platform = 2,
    IndirectSalesManager = 3
}
