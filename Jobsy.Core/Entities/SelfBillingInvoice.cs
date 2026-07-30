namespace Jobsy.Core.Entities;

public class SelfBillingInvoice
{
    public Guid Id { get; set; }
    public Guid SalesManagerUserId { get; set; }
    public User SalesManagerUser { get; set; } = null!;

    public string InvoiceNumber { get; set; } = string.Empty;
    public string SalesManagerCompanyName { get; set; } = string.Empty;
    public string SalesManagerKvkNumber { get; set; } = string.Empty;
    public string SalesManagerVatNumber { get; set; } = string.Empty;
    public string SalesManagerAddress { get; set; } = string.Empty;

    public decimal SubtotalExVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }
    public decimal VatRate { get; set; } = 0.21m;

    /// <summary>How VAT applies for this SM invoice (default 21% inkoop-btw).</summary>
    public SalesManagerVatTreatment VatTreatment { get; set; } = SalesManagerVatTreatment.Standard21;

    public SelfBillingInvoiceStatus Status { get; set; } = SelfBillingInvoiceStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    /// <summary>Set when included in a confirmed BTW aangifte.</summary>
    public Guid? VatDeclarationId { get; set; }
    public VatDeclaration? VatDeclaration { get; set; }

    /// <summary>e.g. "Verwerkt in aangifte 2026-Q1".</summary>
    public string? VatDeclarationStatusLabel { get; set; }

    public ICollection<SelfBillingInvoiceLine> Lines { get; set; } = new List<SelfBillingInvoiceLine>();
    public ICollection<CommissionLedgerEntry> LinkedLedgerEntries { get; set; } = new List<CommissionLedgerEntry>();
}

public class SelfBillingInvoiceLine
{
    public Guid Id { get; set; }
    public Guid SelfBillingInvoiceId { get; set; }
    public SelfBillingInvoice Invoice { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal AmountExVat { get; set; }
    public Guid? SourceLedgerEntryId { get; set; }
}

public enum SelfBillingInvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Paid = 2,
    Cancelled = 3
}
