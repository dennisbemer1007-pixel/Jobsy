using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// B2C invoice for a paid uitgebreide-test checkout. Series LOB-KT-{yyyy}-{0000}.
/// Kept after account deletion (UserId set null) for fiscal retention.
/// </summary>
public class ConsumerPurchaseInvoice
{
    public Guid Id { get; set; }

    /// <summary>Unique administrative invoice id, e.g. LOB-KT-2026-0001.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid DeepAnalysisCheckoutId { get; set; }
    public DeepAnalysisCheckout Checkout { get; set; } = null!;

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerCountry { get; set; }

    public string Description { get; set; } = string.Empty;
    public AssessmentKind Kind { get; set; }

    public int AmountExVatCents { get; set; }
    public int VatAmountCents { get; set; }
    public int TotalAmountCents { get; set; }
    public decimal VatRate { get; set; } = 0.21m;

    public string MolliePaymentId { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public bool IsStub { get; set; }

    public DateTime IssuedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Guid? VatDeclarationId { get; set; }
    public VatDeclaration? VatDeclaration { get; set; }
    public string? VatDeclarationStatusLabel { get; set; }
}
