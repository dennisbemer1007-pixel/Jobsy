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
    int SessionInactivityTimeoutMinutes = 30);

public sealed record PlatformFeatureUpdate(
    bool VacancyContentModerationEnabled,
    bool AuthenticatorEnabled,
    bool ExposeRegistrationActivationLinks,
    string? PublicWebBaseUrl,
    int InactiveCompanyDays = 120,
    int SessionInactivityTimeoutMinutes = 30);
