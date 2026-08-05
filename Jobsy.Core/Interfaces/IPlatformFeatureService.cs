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
    DateOnly? FreePublishUntil = null);

public sealed record PlatformFeatureUpdate(
    bool VacancyContentModerationEnabled,
    bool AuthenticatorEnabled,
    bool ExposeRegistrationActivationLinks,
    string? PublicWebBaseUrl,
    int InactiveCompanyDays = 120,
    int SessionInactivityTimeoutMinutes = 30,
    DateOnly? FreePublishUntil = null,
    /// <summary>
    /// When true, clears <see cref="FreePublishUntil"/> (promo off). When false and
    /// <see cref="FreePublishUntil"/> is null, the existing value is preserved so partial
    /// platform-feature updates do not silently disable the launch promo.
    /// </summary>
    bool ClearFreePublishUntil = false);
