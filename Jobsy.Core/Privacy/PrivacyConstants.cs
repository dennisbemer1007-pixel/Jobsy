namespace Jobsy.Core.Privacy;

public static class PrivacyConstants
{
    /// <summary>Current privacy / terms consent version (bump when legal text changes).</summary>
    public const string CurrentConsentVersion = "2026-07-29";

    public const int PlatformLogRetentionDays = 90;
    public const int CancelledRegistrationRetentionDays = 30;
    public const int EngagementEventRetentionDays = 365;

    /// <summary>Unverified application drafts (OTP pending) are purged after this many hours.</summary>
    public const int UnverifiedApplicationRetentionHours = 48;
}
