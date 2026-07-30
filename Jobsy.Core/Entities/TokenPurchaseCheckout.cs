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
    public decimal AmountEuro { get; set; }
    public TokenPurchaseCheckoutStatus Status { get; set; } = TokenPurchaseCheckoutStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? CreditedAt { get; set; }
}

public enum TokenPurchaseCheckoutStatus
{
    Pending = 0,
    Paid = 1,
    Credited = 2,
    Cancelled = 3
}
