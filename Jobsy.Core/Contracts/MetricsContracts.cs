using Jobsy.Core.Enums;

namespace Jobsy.Core.Contracts;

public static class MetricsPeriodParser
{
    public static MetricsPeriod Parse(string? period) =>
        (period?.Trim().ToLowerInvariant()) switch
        {
            "week" => MetricsPeriod.Week,
            "month" => MetricsPeriod.Month,
            "quarter" => MetricsPeriod.Quarter,
            "year" => MetricsPeriod.Year,
            _ => MetricsPeriod.Day
        };

    public static (DateTime FromUtc, DateTime ToUtc) ResolveRange(MetricsPeriod period, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var to = now;
        var from = period switch
        {
            MetricsPeriod.Week => now.Date.AddDays(-7),
            MetricsPeriod.Month => now.Date.AddDays(-30),
            MetricsPeriod.Quarter => now.Date.AddDays(-90),
            MetricsPeriod.Year => now.Date.AddDays(-365),
            _ => now.Date
        };
        return (from, to);
    }
}

public record MetricCountDto(
    string Key,
    string Label,
    string Period,
    decimal Value,
    IReadOnlyList<decimal>? Sparkline = null);

public record MetricDrilldownItemDto(
    Guid Id,
    string Title,
    string? Subtitle,
    DateTime CreatedAt,
    decimal? Amount = null);

/// <summary>Per-vacancy performance row for Top/Flop dashboard tables.</summary>
public record VacancyPerformanceItemDto(
    Guid VacancyId,
    string Title,
    string CompanyName,
    int Impressions,
    int Clicks,
    int Applications);

/// <summary>Ranked vacancy performance board for a metrics period.</summary>
public record VacancyPerformanceBoardDto(
    string Period,
    IReadOnlyList<VacancyPerformanceItemDto> Top,
    IReadOnlyList<VacancyPerformanceItemDto> Flop);

/// <summary>Platform-wide KPI keys only returned for Admin metrics summaries/drilldowns.</summary>
public static class MetricsKeys
{
    public static readonly HashSet<string> PlatformOnly = new(StringComparer.OrdinalIgnoreCase)
    {
        "errors", "users_open_for_work", "users_active",
        "companies_employers", "companies_intermediaries",
        "site_visits", "site_visits_unique",
        "companies_with_api", "companies_with_csv",
        "unpublished_vacancies",
        "reengagement_emails_sent", "reengagement_reactivated"
    };
}

public record TokenLogItemDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Kind,
    string Reason,
    decimal Amount,
    decimal OldBalance,
    decimal NewBalance,
    string? Note,
    Guid? VacancyId,
    Guid? BranchCompanyId,
    DateTime CreatedAt);

public record PlatformLogItemDto(
    Guid Id,
    string Level,
    string Category,
    string Message,
    DateTime CreatedAt);
