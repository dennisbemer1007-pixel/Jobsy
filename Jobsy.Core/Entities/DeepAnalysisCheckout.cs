using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>Mollie checkout to unlock a paid uitgebreide test. Amount comes from admin settings at create time.</summary>
public class DeepAnalysisCheckout
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public AssessmentKind Kind { get; set; } = AssessmentKind.Competence;
    public string PaymentId { get; set; } = string.Empty;
    public decimal AmountEuro { get; set; }

    public int AmountExVatCents { get; set; }
    public int VatAmountCents { get; set; }
    public int TotalAmountCents { get; set; }

    public string? PaymentMethod { get; set; }
    public string? ProviderStatus { get; set; }
    public bool IsStub { get; set; }

    public DateTime WaiverAcceptedAtUtc { get; set; }
    public string WaiverTextVersion { get; set; } = "legacy";
    public string Locale { get; set; } = "nl";

    public Guid? InvoiceId { get; set; }
    public ConsumerPurchaseInvoice? Invoice { get; set; }

    public DateTime? FailedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? ReceiptSentAtUtc { get; set; }
    public int ReceiptSendAttempts { get; set; }

    public DeepAnalysisCheckoutStatus Status { get; set; } = DeepAnalysisCheckoutStatus.Pending;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}

public enum DeepAnalysisCheckoutStatus
{
    Pending = 0,
    Paid = 1,
    Cancelled = 2,
    Failed = 3,
    Expired = 4
}
