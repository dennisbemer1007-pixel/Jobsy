namespace Jobsy.Core.Entities;

/// <summary>
/// Business profile for a salesmanager (B2B self-billing). Tracking code is issued only after onboarding + agreement.
/// </summary>
public class SalesManagerProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? CompanyName { get; set; }
    public string? KvkNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; } = "NL";
    public string? Iban { get; set; }

    /// <summary>Unique referral code; null until onboarding + agreement are complete.</summary>
    public string? TrackingCode { get; set; }

    public DateTime? AgreementSignedAt { get; set; }
    public string? AgreementVersion { get; set; }
    public DateTime? OnboardingCompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsOnboardingComplete =>
        OnboardingCompletedAt.HasValue
        && AgreementSignedAt.HasValue
        && !string.IsNullOrWhiteSpace(TrackingCode);
}
