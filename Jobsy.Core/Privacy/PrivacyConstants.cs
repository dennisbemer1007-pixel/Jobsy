using Jobsy.Core.Enums;

namespace Jobsy.Core.Privacy;

public static class PrivacyConstants
{
    /// <summary>Current privacy / terms consent version (bump when legal text changes).</summary>
    public const string CurrentConsentVersion = "2026-08-02";

    public const int PlatformLogRetentionDays = 90;
    public const int CancelledRegistrationRetentionDays = 30;
    public const int EngagementEventRetentionDays = 365;

    /// <summary>In-app notifications older than this are purged (AVG retention).</summary>
    public const int UserNotificationRetentionDays = 365;

    /// <summary>Used or expired candidate action tokens older than this are purged.</summary>
    public const int CandidateActionTokenRetentionDays = 30;

    /// <summary>Screenshots on resolved feedback are dropped after this many days (AVG minimization).</summary>
    public const int FeedbackScreenshotRetentionDays = 90;

    /// <summary>Unverified application drafts (OTP pending) are purged after this many hours.</summary>
    public const int UnverifiedApplicationRetentionHours = 48;

    /// <summary>
    /// Unconfirmed company/intermediary registrations (OTP pending) are hard-deleted after this many minutes.
    /// </summary>
    public const int UnconfirmedRegistrationRetentionMinutes = 10;

    public static bool IsCurrentConsent(string? consentVersion)
        => string.Equals(consentVersion, CurrentConsentVersion, StringComparison.Ordinal);

    /// <summary>
    /// Employer/sales/admin accounts must re-accept after a consent-version bump.
    /// Candidates re-consent per application (server-stamped), so they are not blocked here.
    /// </summary>
    public static bool RequiresAccountConsentReaccept(UserRole role, string? consentVersion)
        => role != UserRole.Candidate && !IsCurrentConsent(consentVersion);
}
