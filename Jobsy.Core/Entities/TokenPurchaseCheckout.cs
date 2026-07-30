namespace Jobsy.Core.Entities;

/// <summary>
/// Persisted Mollie checkout session. Credits must use stored PackSize/CompanyId only.
/// </summary>
public class TokenPurchaseCheckout
{
    public Guid Id { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public int PackSize { get; set; }

    /// <summary>Legacy euro total (incl. BTW). Prefer the *Cents fields for new logic.</summary>
    public decimal AmountEuro { get; set; }

    /// <summary>Ex-BTW amount in whole cents.</summary>
    public int AmountExVatCents { get; set; }

    /// <summary>BTW (21%) in whole cents.</summary>
    public int VatAmountCents { get; set; }

    /// <summary>Total incl. BTW in whole cents.</summary>
    public int TotalAmountCents { get; set; }

    public TokenPurchaseCheckoutStatus Status { get; set; } = TokenPurchaseCheckoutStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? CreditedAt { get; set; }

    public Guid? TokenTransactionId { get; set; }
    public Guid? TokenPurchaseInvoiceId { get; set; }
    public TokenPurchaseInvoice? Invoice { get; set; }
}

public enum TokenPurchaseCheckoutStatus
{
    Pending = 0,
    Paid = 1,
    Credited = 2,
    Cancelled = 3
}
