using System.Text.RegularExpressions;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Rules;

public static class ProactiveMatchingRules
{
    public const string ConsentVersion = "proactive-matching-2026-01";

    public const int MinScorePercentFloor = 50;
    public const int MinScorePercentCeiling = 99;
    public const int MaxNotificationsPerWeekFloor = 1;
    public const int MaxNotificationsPerWeekCeiling = 21;
    public const int SameVacancyCooldownDaysFloor = 1;
    public const int SameVacancyCooldownDaysCeiling = 365;
    public const int SameVacancyCooldownDaysDefault = 30;

    private static readonly Regex TrailingParenCount = new(
        @"\(\d+\)\s*$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
        matchTimeout: TimeSpan.FromMilliseconds(50));

    private static readonly Regex CollapseWhitespace = new(
        @"\s+",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
        matchTimeout: TimeSpan.FromMilliseconds(50));

    public static int ClampMinScore(int value)
        => Math.Clamp(value, MinScorePercentFloor, MinScorePercentCeiling);

    public static int ClampMaxPerWeek(int value)
        => Math.Clamp(value, MaxNotificationsPerWeekFloor, MaxNotificationsPerWeekCeiling);

    public static int ClampSameVacancyCooldownDays(int value)
        => Math.Clamp(value, SameVacancyCooldownDaysFloor, SameVacancyCooldownDaysCeiling);

    public static string NormalizeVacancyTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "";
        }

        var t = TrailingParenCount.Replace(title.Trim(), "").Trim();
        return CollapseWhitespace.Replace(t, " ");
    }

    public static string NormalizeCompanyLabel(string? companyLabel)
    {
        if (string.IsNullOrWhiteSpace(companyLabel))
        {
            return "";
        }

        return CollapseWhitespace.Replace(companyLabel.Trim(), " ");
    }

    public static (string Title, string Company) DeliveryIdentity(string vacancyTitle, string? companyLabel)
        => (NormalizeVacancyTitle(vacancyTitle), NormalizeCompanyLabel(companyLabel));

    public static bool MatchesDeliveryIdentity(
        string leftTitle,
        string? leftCompany,
        string rightTitle,
        string? rightCompany)
    {
        var left = DeliveryIdentity(leftTitle, leftCompany);
        var right = DeliveryIdentity(rightTitle, rightCompany);
        return left.Title.Equals(right.Title, StringComparison.OrdinalIgnoreCase)
               && left.Company.Equals(right.Company, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Skip delivery when candidate rejected this title+company or received it within the cooldown window.</summary>
    public static bool ShouldSkipProactiveDelivery(
        IReadOnlyList<ProactiveDeliveryHistoryEntry> history,
        string vacancyTitle,
        string? companyLabel,
        DateTime utcNow,
        int cooldownDays)
    {
        if (IsPermanentlyBlockedByNotInterested(history, vacancyTitle, companyLabel))
        {
            return true;
        }

        return IsWithinSameVacancyCooldown(history, vacancyTitle, companyLabel, utcNow, cooldownDays);
    }

    public static bool IsPermanentlyBlockedByNotInterested(
        IReadOnlyList<ProactiveDeliveryHistoryEntry> history,
        string vacancyTitle,
        string? companyLabel)
        => history.Any(h =>
            h.Feedback == ProactiveMatchFeedback.NotInterested
            && MatchesDeliveryIdentity(h.Title, h.CompanyLabel, vacancyTitle, companyLabel));

    /// <summary>DNA-based one-liner for proactive match cards (never a generic profile blurb when headlines exist).</summary>
    public static string FormatProactiveMatchReason(ProfileVacancyMatch match)
    {
        var headlines = ProfileVacancyMatchCalculator.WhyHeadlines(match);
        if (headlines.Count > 0)
        {
            return string.Join(" · ", headlines);
        }

        var line = ProfileVacancyMatchCalculator.SummaryLine(match);
        if (!string.IsNullOrWhiteSpace(line) && !IsGenericStoredRationale(line))
        {
            return line!;
        }

        if (!string.IsNullOrWhiteSpace(match.MatchRationale) && !IsGenericStoredRationale(match.MatchRationale))
        {
            return match.MatchRationale!;
        }

        return $"Op basis van je DNA-profiel en voorkeuren scoort deze vacature {match.TotalPercent}%.";
    }

    public static IReadOnlyList<string> ProactiveMatchHighlightLabels(ProfileVacancyMatch match)
        => ProfileVacancyMatchCalculator.WhyHeadlines(match);

    public static bool IsGenericStoredRationale(string? rationale)
    {
        if (string.IsNullOrWhiteSpace(rationale))
        {
            return true;
        }

        return (rationale.Contains("past voor", StringComparison.OrdinalIgnoreCase)
                && rationale.Contains("bij jouw profiel", StringComparison.OrdinalIgnoreCase))
               || rationale.Contains("externe vacature sluit aan", StringComparison.OrdinalIgnoreCase)
               || rationale.Equals("Deze vacature past bij jouw profiel.", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsWithinSameVacancyCooldown(
        IReadOnlyList<ProactiveDeliveryHistoryEntry> history,
        string vacancyTitle,
        string? companyLabel,
        DateTime utcNow,
        int cooldownDays)
    {
        cooldownDays = ClampSameVacancyCooldownDays(cooldownDays);
        var cutoff = utcNow.AddDays(-cooldownDays);
        return history.Any(h =>
            h.DeliveredAtUtc >= cutoff
            && MatchesDeliveryIdentity(h.Title, h.CompanyLabel, vacancyTitle, companyLabel));
    }

    public static bool HasValidConsent(DateTime? acceptedAt, string? version)
        => acceptedAt is not null
           && string.Equals(version, ConsentVersion, StringComparison.Ordinal);

    public static bool ShouldDeliverNow(
        ProactiveMatchingFrequency frequency,
        DateTime utcNow,
        DateTime? lastProcessedAtUtc)
    {
        if (lastProcessedAtUtc is null)
        {
            return true;
        }

        return frequency switch
        {
            ProactiveMatchingFrequency.Immediate => true,
            ProactiveMatchingFrequency.Daily => utcNow - lastProcessedAtUtc.Value >= TimeSpan.FromHours(20),
            ProactiveMatchingFrequency.Weekly => utcNow - lastProcessedAtUtc.Value >= TimeSpan.FromDays(6),
            _ => false
        };
    }

    public static DateTime WeekStartUtc(DateTime utcNow)
    {
        var d = DateOnly.FromDateTime(utcNow);
        var offset = ((int)d.DayOfWeek + 6) % 7;
        return d.AddDays(-offset).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }

    /// <summary>In-app notification title for a delivered proactive match tip.</summary>
    public static string FormatInAppNewMatchTitle(string vacancyTitle, string companyName, int matchPercent)
    {
        var title = string.IsNullOrWhiteSpace(vacancyTitle) ? "Vacature" : vacancyTitle.Trim();
        var company = string.IsNullOrWhiteSpace(companyName) ? "werkgever" : companyName.Trim();
        return $"Nieuwe match: {title} bij {company}, {matchPercent}%";
    }

    /// <summary>In-app notification title for a vacancy introduction request.</summary>
    public static string FormatInAppIntroductionTitle(string vacancyTitle, string companyName)
    {
        var title = string.IsNullOrWhiteSpace(vacancyTitle) ? "Vacature" : vacancyTitle.Trim();
        var company = string.IsNullOrWhiteSpace(companyName) ? "werkgever" : companyName.Trim();
        return $"Nieuw kennismakingsverzoek: {title} bij {company}";
    }
}
