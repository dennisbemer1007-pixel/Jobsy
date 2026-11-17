using System.Globalization;
using Jobsy.Core.Contracts;

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

    public static JobSearchActivityOverviewDto CreateEmptyOverview(DateOnly periodAnchor, DateOnly today)
    {
        var (periodStart, periodEnd) = CurrentPeriod(periodAnchor, today);
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        return new JobSearchActivityOverviewDto(
            periodStart,
            periodEnd,
            FormatPeriodLabel(periodStart, periodEnd, culture),
            ActivitiesDoneInPeriod: 0,
            ActivitiesTarget: DefaultActivitiesPerPeriod,
            DaysRemainingInPeriod: DaysRemainingInclusive(periodEnd, today),
            TargetMet: false,
            Weeks: Array.Empty<JobSearchActivityWeekDto>());
    }

    public static string FormatActiviteitCountNl(int count)
        => count == 1 ? "1 activiteit" : $"{count} activiteiten";

    public static string FormatActiviteitenGedaanNl(int count)
        => count == 1 ? "1 activiteit gedaan" : $"{count} activiteiten gedaan";

    public static string FormatPeriodLabel(DateOnly start, DateOnly end, CultureInfo culture)
    {
        var isNl = culture.TwoLetterISOLanguageName.Equals("nl", StringComparison.OrdinalIgnoreCase);
        if (isNl)
        {
            return $"{start.ToString("d MMMM", culture)} tot en met {end.ToString("d MMMM", culture)}";
        }

        return $"{start.ToString("d MMMM", culture)} through {end.ToString("d MMMM", culture)}";
    }
}
