namespace Jobsy.Core.Rules;

/// <summary>
/// Pure thresholds for the werkgever dashboard (02): vestiging status, first-response sample floor, token runway.
/// </summary>
public static class WerkgeverDashboardRules
{
    public const int FirstResponseMinSamples = 5;
    public const int OverduePendingHours = 48;
    public const int AchterstandOverdueCount = 5;
    public const double AchterstandFirstResponseDays = 3d;
    public const double AandachtFirstResponseDays = 2d;
    public const decimal LowTokenBalance = 5m;
    public const int TokenRunwayLookbackWeeks = 8;
    public const int VacanciesExpiringDays = 7;
    public const int TodoDefaultTake = 50;
    public const int DashboardTodoPreview = 6;
    public const int SparkPointCount = 7;
    public static readonly TimeSpan DashboardCacheTtl = TimeSpan.FromSeconds(60);

    public enum BranchHealthStatus
    {
        OpSchema = 0,
        Aandacht = 1,
        Achterstand = 2
    }

    /// <summary>
    /// Achterstand when &gt; 5 applications are pending &gt; 48 h <b>or</b> first response &gt; 3 d;
    /// Aandacht when there is no manager, balance ≤ 5 tokens, or first response &gt; 2 d;
    /// otherwise OpSchema.
    /// </summary>
    public static BranchHealthStatus ResolveBranchStatus(
        int overduePendingCount,
        double? avgFirstResponseDays,
        bool hasActiveManager,
        decimal? tokenBalance)
    {
        if (overduePendingCount > AchterstandOverdueCount
            || (avgFirstResponseDays is double daysAchter && daysAchter > AchterstandFirstResponseDays))
        {
            return BranchHealthStatus.Achterstand;
        }

        if (!hasActiveManager
            || (tokenBalance is decimal bal && bal <= LowTokenBalance)
            || (avgFirstResponseDays is double daysAandacht && daysAandacht > AandachtFirstResponseDays))
        {
            return BranchHealthStatus.Aandacht;
        }

        return BranchHealthStatus.OpSchema;
    }

    /// <summary>Mean hours; <c>null</c> when fewer than <see cref="FirstResponseMinSamples"/> samples.</summary>
    public static double? AverageFirstResponseHours(IReadOnlyList<double> responseHours)
    {
        if (responseHours.Count < FirstResponseMinSamples)
        {
            return null;
        }

        return responseHours.Average();
    }

    /// <summary>Balance / average weekly spend over the lookback; <c>null</c> when there is no spend.</summary>
    public static double? TokenRunwayWeeks(decimal balance, decimal totalSpendOverLookback, int lookbackWeeks = TokenRunwayLookbackWeeks)
    {
        if (totalSpendOverLookback <= 0m || lookbackWeeks <= 0)
        {
            return null;
        }

        var weekly = (double)totalSpendOverLookback / lookbackWeeks;
        if (weekly <= 0)
        {
            return null;
        }

        return (double)balance / weekly;
    }

    public static (DateTime PeriodStart, DateTime PreviousStart, DateTime PreviousEnd) ResolvePeriodWindow(
        string period,
        DateTime utcNow)
    {
        var days = period switch
        {
            "7d" => 7,
            "90d" => 90,
            _ => 30
        };
        var periodStart = utcNow.AddDays(-days);
        var previousEnd = periodStart;
        var previousStart = previousEnd.AddDays(-days);
        return (periodStart, previousStart, previousEnd);
    }

    public static string NormalizePeriod(string? period)
        => period switch
        {
            "7d" => "7d",
            "90d" => "90d",
            _ => "30d"
        };

    public static double? HoursToDays(double? hours)
        => hours is null ? null : hours.Value / 24d;
}
