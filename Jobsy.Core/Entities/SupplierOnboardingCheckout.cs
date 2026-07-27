namespace Jobsy.Core.Entities;

/// <summary>
/// €2.500 first-year supplier onboarding payment session (Pending → Paid → Credited).
/// </summary>
public class SupplierOnboardingCheckout
{
    public Guid Id { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public decimal AmountEuro { get; set; }
    public SupplierOnboardingCheckoutStatus Status { get; set; } = SupplierOnboardingCheckoutStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? CreditedAt { get; set; }
}

public enum SupplierOnboardingCheckoutStatus
{
    Pending = 0,
    Paid = 1,
    Credited = 2,
    Cancelled = 3
}
