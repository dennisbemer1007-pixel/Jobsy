namespace Jobsy.Core.Entities;

/// <summary>
/// Automated BTW-buffer transfer order toward the configured Knab BTW IBAN.
/// Description/kenmerk is always the related token-purchase invoice number.
/// </summary>
public class VatBufferTransfer
{
    public Guid Id { get; set; }

    public Guid TokenPurchaseInvoiceId { get; set; }
    public TokenPurchaseInvoice Invoice { get; set; } = null!;

    /// <summary>Copy of invoice number — used as bank omschrijving/kenmerk.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Destination Knab BTW IBAN at the time of queuing.</summary>
    public string DestinationIban { get; set; } = string.Empty;

    /// <summary>BTW amount to transfer, in whole cents.</summary>
    public int AmountCents { get; set; }

    public VatBufferTransferStatus Status { get; set; } = VatBufferTransferStatus.Pending;

    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Admin/ops note or failure detail.</summary>
    public string? Note { get; set; }
}

public enum VatBufferTransferStatus
{
    /// <summary>Waiting for background processor.</summary>
    Pending = 0,

    /// <summary>Transfer order recorded/logged for bank execution.</summary>
    Logged = 1,

    /// <summary>Confirmed completed (manual or bank feedback).</summary>
    Completed = 2,

    /// <summary>Failed — see Note.</summary>
    Failed = 3,

    /// <summary>No Knab BTW IBAN configured at queue time.</summary>
    SkippedNoIban = 4
}
