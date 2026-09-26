using Jobsy.Core.Enums;

namespace Jobsy.Core.Privacy;

public static class PrivacyConstants
{
    /// <summary>Current privacy / terms consent version (bump when legal text changes).</summary>
    public const string CurrentConsentVersion = "2026-09-26";

    /// <summary>Version of the separate optional test/AI and talent-pool consents.</summary>
    public const string CandidateProfilingConsentVersion = "2026-09-26";

    public const int PlatformLogRetentionDays = 90;
    public const int CancelledRegistrationRetentionDays = 30;
    public const int EngagementEventRetentionDays = 365;

    /// <summary>In-app notifications older than this are purged (AVG retention).</summary>
    public const int UserNotificationRetentionDays = 365;

    /// <summary>Used or expired candidate action tokens older than this are purged.</summary>
    public const int CandidateActionTokenRetentionDays = 30;

    /// <summary>Screenshots on any feedback are dropped after this many days (AVG minimization).</summary>
    public const int FeedbackScreenshotRetentionDays = 90;

    /// <summary>Placeholder written over free-text feedback when a user is forgotten.</summary>
    public const string ForgottenFeedbackDescription = "[Gewist bij uitschrijving]";

    /// <summary>Unverified application drafts (OTP pending) are purged after this many hours.</summary>
    public const int UnverifiedApplicationRetentionHours = 48;

    /// <summary>
    /// Unconfirmed company/intermediary registrations (OTP pending) are hard-deleted after this many minutes.
    /// </summary>
    public const int UnconfirmedRegistrationRetentionMinutes = 10;

    public static bool IsCurrentConsent(string? consentVersion)
        => string.Equals(consentVersion, CurrentConsentVersion, StringComparison.Ordinal);

    /// <summary>
    /// Every account must re-accept after a privacy/terms version bump.
    /// </summary>
    public static bool RequiresAccountConsentReaccept(UserRole role, string? consentVersion)
        => !IsCurrentConsent(consentVersion);
}
