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

    /// <summary>
    /// Days without login/API/CSV/active vacancy before a one-time re-engagement e-mail may be sent.
    /// Default 120 days (4 months).
    /// </summary>
    public int InactiveCompanyDays { get; set; } = 120;

    /// <summary>
    /// Interactive session inactivity timeout in minutes (cookie + UI idle timer).
    /// Default 30 minutes; admin-configurable.
    /// </summary>
    public int SessionInactivityTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Inclusive last calendar day (UTC date) on which publishing a vacancy costs 0 tokens.
    /// Highlight and PushBom stay paid. Null = promo off (normal publish rates).
    /// Seeded default: 2026-11-18.
    /// </summary>
    public DateOnly? FreePublishUntil { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
