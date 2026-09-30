namespace Jobsy.Core.Security;

/// <summary>Lifetimes and retention for single-use invite / API-key reveal links (emails hotfix D7).</summary>
public static class OneTimeLinkRules
{
    public static readonly TimeSpan SetPasswordLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan ApiKeyRevealLifetime = TimeSpan.FromHours(72);

    /// <summary>Used or expired links older than this are purged by the daily retention job.</summary>
    public const int RetentionDays = 30;
}
