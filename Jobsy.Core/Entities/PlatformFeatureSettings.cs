namespace Jobsy.Core.Entities;

/// <summary>
/// Singleton row for platform feature toggles and public URLs (admin settings).
/// </summary>
public class PlatformFeatureSettings
{
    public Guid Id { get; set; }

    /// <summary>When false, vacancy save skips AI/heuristic content moderation.</summary>
    public bool VacancyContentModerationEnabled { get; set; } = true;

    public bool AuthenticatorEnabled { get; set; }

    public bool ExposeRegistrationActivationLinks { get; set; }

    public string? PublicWebBaseUrl { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
