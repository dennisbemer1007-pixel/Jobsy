namespace Jobsy.Core.Entities;

/// <summary>€ 2,99 Mollie checkout to unlock the 150-question deep analysis.</summary>
public class DeepAnalysisCheckout
{
    public const decimal PriceEuro = 2.99m;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string PaymentId { get; set; } = string.Empty;
    public decimal AmountEuro { get; set; } = PriceEuro;
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
