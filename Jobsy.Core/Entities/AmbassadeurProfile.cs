namespace Jobsy.Core.Entities;

/// <summary>
/// Business profile for an Ambassadeur (candidate + entrepreneur acquisition partner).
/// Tracking code is issued only after onboarding + agreement (same flow as SalesManager).
/// </summary>
public class AmbassadeurProfile
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

    /// <summary>Starting commission percentage (default 5.0).</summary>
    public decimal BaseCommissionPercentage { get; set; } = 5.0m;

    /// <summary>
    /// Optional Admin override for the effective commission percentage.
    /// When set, this replaces the threshold-based calculation (still capped by Max).
    /// </summary>
    public decimal? CommissionPercentageOverride { get; set; }

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
