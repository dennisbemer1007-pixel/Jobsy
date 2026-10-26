namespace Jobsy.Core.Rules;

public static class JobSearchActivityPeriodRules
{
    public const int PeriodDays = 28;
    public const int DefaultActivitiesPerPeriod = 4;
    public const int MinActivitiesPerPeriod = 1;
    public const int MaxActivitiesPerPeriod = 20;

    public static int ClampActivitiesPerPeriod(int value)
        => Math.Clamp(value, MinActivitiesPerPeriod, MaxActivitiesPerPeriod);

    public static (DateOnly Start, DateOnly EndInclusive) CurrentPeriod(DateOnly periodAnchor, DateOnly today)
    {
        if (today < periodAnchor)
        {
            return (periodAnchor, periodAnchor.AddDays(PeriodDays - 1));
        }

        var daysSince = today.DayNumber - periodAnchor.DayNumber;
        var periodIndex = daysSince / PeriodDays;
        var start = periodAnchor.AddDays(periodIndex * PeriodDays);
        var end = start.AddDays(PeriodDays - 1);
        return (start, end);
    }

    public static int DaysRemainingInclusive(DateOnly periodEnd, DateOnly today)
        => Math.Max(0, periodEnd.DayNumber - today.DayNumber + 1);

    public static bool IsInPeriod(DateOnly activityDate, DateOnly periodStart, DateOnly periodEndInclusive)
        => activityDate >= periodStart && activityDate <= periodEndInclusive;
}
