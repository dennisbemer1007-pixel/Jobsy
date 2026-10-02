using System.Globalization;
using Jobsy.Core.Privacy;

namespace Jobsy.Core.Legal;

/// <summary>
/// One row of the retention table in the privacy statement.
/// <paramref name="Duration"/> is evaluated lazily so the table always shows today's constant.
/// <paramref name="Constants"/> names the <see cref="PrivacyConstants"/> fields the row is built from;
/// <c>LegalRetentionCatalogTests</c> uses it so a new retention constant cannot be forgotten.
/// </summary>
public sealed record LegalRetentionRow(
    string LabelKey,
    Func<string> Duration,
    IReadOnlyList<string> Constants);

/// <summary>
/// The retention table of the privacy statement, built from <see cref="PrivacyConstants"/>
/// instead of hand-typed copy. Durations are the official Dutch text of the statement.
/// </summary>
public static class LegalRetention
{
    public static readonly IReadOnlyList<LegalRetentionRow> Rows =
    [
        new(
            "Legal.Retention.UnverifiedApplication",
            () => FormatHours(PrivacyConstants.UnverifiedApplicationRetentionHours),
            [nameof(PrivacyConstants.UnverifiedApplicationRetentionHours)]),
        new(
            "Legal.Retention.UnconfirmedRegistration",
            () => FormatMinutes(PrivacyConstants.UnconfirmedRegistrationRetentionMinutes),
            [nameof(PrivacyConstants.UnconfirmedRegistrationRetentionMinutes)]),
        new(
            "Legal.Retention.CancelledRegistration",
            () => FormatDays(PrivacyConstants.CancelledRegistrationRetentionDays),
            [nameof(PrivacyConstants.CancelledRegistrationRetentionDays)]),
        new(
            "Legal.Retention.ActionLinks",
            () => FormatDays(Math.Max(
                PrivacyConstants.CandidateActionTokenRetentionDays,
                PrivacyConstants.OneTimeLinkRetentionDays)),
            [
                nameof(PrivacyConstants.CandidateActionTokenRetentionDays),
                nameof(PrivacyConstants.OneTimeLinkRetentionDays)
            ]),
        new(
            "Legal.Retention.PlatformLogs",
            () => FormatDays(Math.Max(
                PrivacyConstants.PlatformLogRetentionDays,
                PrivacyConstants.FeedbackScreenshotRetentionDays)),
            [
                nameof(PrivacyConstants.PlatformLogRetentionDays),
                nameof(PrivacyConstants.FeedbackScreenshotRetentionDays)
            ]),
        new(
            "Legal.Retention.Engagement",
            () => FormatDays(Math.Max(
                PrivacyConstants.EngagementEventRetentionDays,
                PrivacyConstants.UserNotificationRetentionDays)),
            [
                nameof(PrivacyConstants.EngagementEventRetentionDays),
                nameof(PrivacyConstants.UserNotificationRetentionDays)
            ]),
        new(
            "Legal.Retention.AccessLog",
            () => FormatDays(PrivacyConstants.PersonalDataAccessLogRetentionDays),
            [nameof(PrivacyConstants.PersonalDataAccessLogRetentionDays)]),
        new(
            "Legal.Retention.SalesLinkClicks",
            () => FormatMonths(PrivacyConstants.SalesLinkClickRetentionMonths),
            [nameof(PrivacyConstants.SalesLinkClickRetentionMonths)]),
        new(
            "Legal.Retention.SalesManagerApplication",
            () => string.Join(" · ", new[]
            {
                FormatDays(PrivacyConstants.SalesManagerApplicationPendingRetentionDays),
                FormatDays(Math.Max(
                    PrivacyConstants.SalesManagerApplicationRejectedRetentionDays,
                    PrivacyConstants.SalesManagerApplicationApprovedRetentionDays))
            }.Distinct(StringComparer.Ordinal)),
            [
                nameof(PrivacyConstants.SalesManagerApplicationPendingRetentionDays),
                nameof(PrivacyConstants.SalesManagerApplicationRejectedRetentionDays),
                nameof(PrivacyConstants.SalesManagerApplicationApprovedRetentionDays)
            ]),
        new(
            "Legal.Retention.Invoices",
            () => FormatYears(PrivacyConstants.SalesFiscalRetentionYears),
            [nameof(PrivacyConstants.SalesFiscalRetentionYears)]),
        new(
            "Legal.Retention.AdminAudit",
            () => FormatDays(PrivacyConstants.AdminAuditRetentionDays),
            [nameof(PrivacyConstants.AdminAuditRetentionDays)]),
        new(
            "Legal.Retention.Account",
            () => UntilAccountDeleted,
            [])
    ];

    /// <summary>Account data has no fixed term; it lives until the visitor deletes the account.</summary>
    public const string UntilAccountDeleted = "tot je je account verwijdert";

    public static string FormatMinutes(int minutes)
        => $"{minutes.ToString(CultureInfo.InvariantCulture)} minuten";

    public static string FormatHours(int hours)
        => $"{hours.ToString(CultureInfo.InvariantCulture)} uur";

    public static string FormatMonths(int months)
        => $"{months.ToString(CultureInfo.InvariantCulture)} maanden";

    public static string FormatYears(int years)
        => years == 1 ? "1 jaar" : $"{years.ToString(CultureInfo.InvariantCulture)} jaar";

    /// <summary>Whole multiples of a year from 730 days up read as years ("2 jaar", "7 jaar").</summary>
    public static string FormatDays(int days)
        => days >= 730 && days % 365 == 0
            ? FormatYears(days / 365)
            : $"{days.ToString(CultureInfo.InvariantCulture)} dagen";
}
