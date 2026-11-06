namespace Jobsy.Core.Rules;

public static class InitiativeBusinessDayRules
{
    public const int ResponseBusinessDays = 5;

    public static DateTime AddBusinessDaysUtc(DateTime startUtc, int businessDays)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        var local = TimeZoneInfo.ConvertTimeFromUtc(startUtc, tz);
        var remaining = businessDays;
        while (remaining > 0)
        {
            local = local.AddDays(1);
            if (local.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                remaining--;
            }
        }

        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local.Date.AddHours(23).AddMinutes(59), DateTimeKind.Unspecified),
            tz);
    }
}
