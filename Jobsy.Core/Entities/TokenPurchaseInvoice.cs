namespace Jobsy.Core.Entities;

/// <summary>
/// Official token-purchase invoice generated after a successful Mollie payment.
/// InvoiceNumber is unique and used as the BTW-buffer transfer description.
/// </summary>
public class TokenPurchaseInvoice
{
    public Guid Id { get; set; }

    /// <summary>Unique administrative invoice id, e.g. LOB-TK-2026-0001.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid TokenPurchaseCheckoutId { get; set; }
    public TokenPurchaseCheckout Checkout { get; set; } = null!;

    public Guid? TokenTransactionId { get; set; }
    public TokenTransaction? TokenTransaction { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string MolliePaymentId { get; set; } = string.Empty;
    public int PackSize { get; set; }

    /// <summary>Amount excl. BTW in whole cents.</summary>
    public int AmountExVatCents { get; set; }

    /// <summary>BTW amount (21%) in whole cents.</summary>
    public int VatAmountCents { get; set; }

    /// <summary>Total incl. BTW in whole cents (what was paid via Mollie).</summary>
    public int TotalAmountCents { get; set; }

    public decimal VatRate { get; set; } = 0.21m;

    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyKvkNumber { get; set; }
    public string? CompanyAddress { get; set; }

    public DateTime IssuedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Set when included in a confirmed BTW aangifte — excluded from future open periods.</summary>
    public Guid? VatDeclarationId { get; set; }
    public VatDeclaration? VatDeclaration { get; set; }

    /// <summary>e.g. "Verwerkt in aangifte 2026-Q1".</summary>
    public string? VatDeclarationStatusLabel { get; set; }

    public ICollection<VatBufferTransfer> VatBufferTransfers { get; set; } = new List<VatBufferTransfer>();
}
