namespace Jobsy.Core.Interfaces;

public interface IPlatformFeatureService
{
    Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default);

    Task<PlatformFeatureSnapshot> UpdateAsync(
        PlatformFeatureUpdate update,
        CancellationToken cancellationToken = default);
}

public sealed record PlatformFeatureSnapshot(
    bool VacancyContentModerationEnabled,
    bool AuthenticatorEnabled,
    bool ExposeRegistrationActivationLinks,
    string PublicWebBaseUrl,
    DateTime? UpdatedAtUtc,
    int InactiveCompanyDays = 120,
    int SessionInactivityTimeoutMinutes = 30,
    /// <summary>Inclusive last day publish is free; null = promo off.</summary>
    DateOnly? FreePublishUntil = null,
    int MinimumSessionVersion = 0,
    bool SupportAccessNotifyAdmins = false,
    bool SupportAccessNotifySubject = false,
    bool CandidateInsightsEnabled = true,
    int CandidateInsightsUnlockDays = 90,
    bool CandidateInsightsUnlockPerBranch = false,
    bool SchoolsEnabled = false,
    bool SchoolPerCodeResultsEnabled = true,
    int SchoolRetentionCutoffMonth = 7,
    int SchoolRetentionCutoffDay = 31,
    /// <summary>Default false — Ambassadeur role parked.</summary>
    bool AmbassadorsEnabled = false,
    bool EmployersEnabled = true,
    bool CandidatePassportEnabled = false);

/// <summary>
/// Partial platform-feature update. Null fields keep the current value (nullable = keep).
/// </summary>
public sealed record PlatformFeatureUpdate(
    bool? VacancyContentModerationEnabled = null,
    bool? AuthenticatorEnabled = null,
    bool? ExposeRegistrationActivationLinks = null,
    string? PublicWebBaseUrl = null,
    int? InactiveCompanyDays = null,
    int? SessionInactivityTimeoutMinutes = null,
    DateOnly? FreePublishUntil = null,
    /// <summary>
    /// When true, clears <see cref="FreePublishUntil"/> (promo off). When false and
    /// <see cref="FreePublishUntil"/> is null, the existing value is preserved so partial
    /// platform-feature updates do not silently disable the launch promo.
    /// </summary>
    bool ClearFreePublishUntil = false,
    int? MinimumSessionVersion = null,
    bool? SupportAccessNotifyAdmins = null,
    bool? SupportAccessNotifySubject = null,
    bool? CandidateInsightsEnabled = null,
    int? CandidateInsightsUnlockDays = null,
    bool? CandidateInsightsUnlockPerBranch = null,
    /// <summary>Null = keep existing.</summary>
    bool? SchoolsEnabled = null,
    /// <summary>Null = keep existing.</summary>
    bool? SchoolPerCodeResultsEnabled = null,
    /// <summary>Null = keep existing.</summary>
    int? SchoolRetentionCutoffMonth = null,
    /// <summary>Null = keep existing.</summary>
    int? SchoolRetentionCutoffDay = null,
    /// <summary>Null = keep existing.</summary>
    bool? AmbassadorsEnabled = null,
    bool? EmployersEnabled = null,
    bool? CandidatePassportEnabled = null);
