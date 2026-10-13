namespace Jobsy.Core.Rules;

/// <summary>
/// Mon–Fri working-day arithmetic (no holiday calendar). Used by access-request escalation (07.4)
/// and reusable by verification SLAs (03/06).
/// </summary>
public static class WorkingDays
{
    public static DateOnly AddWorkingDays(DateOnly start, int workingDays)
    {
        if (workingDays == 0)
        {
            return start;
        }

        var step = workingDays > 0 ? 1 : -1;
        var remaining = Math.Abs(workingDays);
        var current = start;
        while (remaining > 0)
        {
            current = current.AddDays(step);
            if (IsWorkingDay(current))
            {
                remaining--;
            }
        }

        return current;
    }

    public static bool IsWorkingDay(DateOnly date)
        => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

    /// <summary>
    /// Whole working days strictly after <paramref name="fromUtc"/> up to <paramref name="toUtc"/>
    /// in the given time zone (date-only, Mon–Fri).
    /// </summary>
    public static int CountWorkingDaysBetween(
        DateTime fromUtc,
        DateTime toUtc,
        TimeZoneInfo timeZone)
    {
        if (toUtc <= fromUtc)
        {
            return 0;
        }

        var fromLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc), timeZone);
        var toLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(toUtc, DateTimeKind.Utc), timeZone);
        var start = DateOnly.FromDateTime(fromLocal);
        var end = DateOnly.FromDateTime(toLocal);
        var count = 0;
        for (var d = start.AddDays(1); d <= end; d = d.AddDays(1))
        {
            if (IsWorkingDay(d))
            {
                count++;
            }
        }

        return count;
    }
}
