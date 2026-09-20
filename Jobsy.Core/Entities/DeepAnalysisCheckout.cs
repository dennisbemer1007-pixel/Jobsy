namespace Jobsy.Core.Entities;

/// <summary>Mollie checkout to unlock the 150-question deep analysis. Amount comes from admin settings.</summary>
public class DeepAnalysisCheckout
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string PaymentId { get; set; } = string.Empty;
    public decimal AmountEuro { get; set; }
    public DeepAnalysisCheckoutStatus Status { get; set; } = DeepAnalysisCheckoutStatus.Pending;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
}

public enum DeepAnalysisCheckoutStatus
{
    Pending = 0,
    Paid = 1,
    Cancelled = 2
}
