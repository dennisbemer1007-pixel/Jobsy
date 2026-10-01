using Jobsy.Core.Enums;

namespace Jobsy.Core.Privacy;

public static class PrivacyConstants
{
    /// <summary>Current privacy / terms consent version (bump when legal text changes).</summary>
    public const string CurrentConsentVersion = "2026-09-26";

    /// <summary>Version of the separate optional test/AI and talent-pool consents.</summary>
    public const string CandidateProfilingConsentVersion = "2026-09-26";

    public const int PlatformLogRetentionDays = 90;

    /// <summary>
    /// Personal-data access log retention (AVG accountability). Configurable override:
    /// <c>Privacy:PersonalDataAccessLogRetentionDays</c>.
    /// </summary>
    public const int PersonalDataAccessLogRetentionDays = 730;

    /// <summary>
    /// Admin audit log retention (fiscal bewaarplicht for token/payout actions). Configurable:
    /// <c>Privacy:AdminAuditRetentionDays</c>. Default 7 years.
    /// </summary>
    public const int AdminAuditRetentionDays = 2555;

    public const int CancelledRegistrationRetentionDays = 30;
    public const int EngagementEventRetentionDays = 365;

    /// <summary>Sales link click daily counters (no personal data) — §P / D13.</summary>
    public const int SalesLinkClickRetentionMonths = 25;

    /// <summary>Pending "Salesmanager aanbevelen" applications expire and PII is cleared after this many days (D13).</summary>
    public const int SalesManagerApplicationPendingRetentionDays = 60;

    /// <summary>Rejected applications: PII cleared this many days after <c>ReviewedAtUtc</c> (D13).</summary>
    public const int SalesManagerApplicationRejectedRetentionDays = 30;

    /// <summary>
    /// Approved applications: PII cleared this many days after <c>ReviewedAtUtc</c> once provisioned (D13).
    /// The provisioned account then holds the person's data.
    /// </summary>
    public const int SalesManagerApplicationApprovedRetentionDays = 30;

    /// <summary>
    /// Fiscal bewaarplicht for invoices, ledger and payout records (documented; no auto-delete job).
    /// </summary>
    public const int SalesFiscalRetentionYears = 7;

    /// <summary>In-app notifications older than this are purged (AVG retention).</summary>
    public const int UserNotificationRetentionDays = 365;

    /// <summary>Used or expired candidate action tokens older than this are purged.</summary>
    public const int CandidateActionTokenRetentionDays = 30;

    /// <summary>Used or expired one-time invite / API-key reveal links older than this are purged.</summary>
    public const int OneTimeLinkRetentionDays = 30;

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

    /// <summary>
    /// Decided content reports (DSA notice and action) are purged this many days after the decision.
    /// Open reports are kept until an admin decides.
    /// </summary>
    public const int ContentReportRetentionDays = 365;

    /// <summary>
    /// The reporter's e-mail address is cleared this many days after the decision — long enough to
    /// send the outcome mail, short enough to keep the report itself anonymous afterwards.
    /// </summary>
    public const int ContentReportEmailRetentionDays = 30;

    public static bool IsCurrentConsent(string? consentVersion)
        => string.Equals(consentVersion, CurrentConsentVersion, StringComparison.Ordinal);

    /// <summary>
    /// Every account must re-accept after a privacy/terms version bump.
    /// </summary>
    public static bool RequiresAccountConsentReaccept(UserRole role, string? consentVersion)
        => !IsCurrentConsent(consentVersion);
}
