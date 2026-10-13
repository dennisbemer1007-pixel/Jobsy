using Jobsy.Core.Enums;

namespace Jobsy.Core.Security;

/// <summary>Lifetimes and retention for single-use invite / API-key reveal / password-reset links.</summary>
public static class OneTimeLinkRules
{
    public static readonly TimeSpan SetPasswordLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan ApiKeyRevealLifetime = TimeSpan.FromHours(72);
    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan ExternalVacancyEmployerInviteLifetime = TimeSpan.FromDays(30);

    /// <summary>Used or expired links older than this are purged by the daily retention job.</summary>
    public const int RetentionDays = 30;

    public static TimeSpan LifetimeFor(OneTimeLinkPurpose purpose) => purpose switch
    {
        OneTimeLinkPurpose.ApiKeyReveal => ApiKeyRevealLifetime,
        OneTimeLinkPurpose.PasswordReset => PasswordResetLifetime,
        OneTimeLinkPurpose.ExternalVacancyEmployerInvite => ExternalVacancyEmployerInviteLifetime,
        _ => SetPasswordLifetime
    };
}
