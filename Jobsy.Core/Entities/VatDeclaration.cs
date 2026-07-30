namespace Jobsy.Core.Entities;

/// <summary>
/// Generated Dutch VAT (BTW) declaration overview for a calendar quarter.
/// PDF bytes are stored for re-download; linked invoices are marked as processed.
/// </summary>
public class VatDeclaration
{
    public Guid Id { get; set; }

    public int Year { get; set; }

    /// <summary>1–4.</summary>
    public int Quarter { get; set; }

    /// <summary>e.g. 2026-Q1 — used in status labels on linked invoices.</summary>
    public string PeriodLabel { get; set; } = string.Empty;

    public VatDeclarationStatus Status { get; set; } = VatDeclarationStatus.Confirmed;

    /// <summary>Rubriek 1a — omzet ex. BTW from token sales (cents).</summary>
    public int Rubriek1OmzetExVatCents { get; set; }

    /// <summary>Rubriek 1a — verschuldigde BTW from token sales (cents).</summary>
    public int Rubriek1VatCents { get; set; }

    public int TokenInvoiceCount { get; set; }
    public int GoodwillCount { get; set; }

    /// <summary>Rubriek 5b — aftrekbare voorbelasting including SM payouts (cents).</summary>
    public int Rubriek5VoorbelastingCents { get; set; }

    /// <summary>Cost base ex. BTW of Rubriek 5 lines (cents).</summary>
    public int Rubriek5CostExVatCents { get; set; }

    public int SalesManagerInvoiceCount { get; set; }

    /// <summary>Positive = te betalen; negative = terug te ontvangen (cents).</summary>
    public int AmountDueCents { get; set; }

    public DateTime GeneratedAt { get; set; }
    public Guid? GeneratedByUserId { get; set; }
    public User? GeneratedByUser { get; set; }
    public string? GeneratedByName { get; set; }

    public byte[]? PdfBytes { get; set; }
    public string PdfFileName { get; set; } = string.Empty;

    public string PlatformCompanyName { get; set; } = string.Empty;
    public string? PlatformKvkNumber { get; set; }
    public string? PlatformVatNumber { get; set; }
    public string? PlatformAddress { get; set; }

    public ICollection<TokenPurchaseInvoice> TokenPurchaseInvoices { get; set; } = new List<TokenPurchaseInvoice>();
    public ICollection<SelfBillingInvoice> SelfBillingInvoices { get; set; } = new List<SelfBillingInvoice>();
}

public enum VatDeclarationStatus
{
    Confirmed = 0,
    Cancelled = 1
}

/// <summary>How VAT applies on salesmanager self-billing / payout invoices.</summary>
public enum SalesManagerVatTreatment
{
    /// <summary>Standard 21% Dutch VAT (inkoop-btw / voorbelasting).</summary>
    Standard21 = 0,

    /// <summary>BTW-verlegging — no VAT charged; not deductible in Rubriek 5.</summary>
    ReverseCharge = 1,

    /// <summary>VAT-exempt exception.</summary>
    Exempt = 2
}
