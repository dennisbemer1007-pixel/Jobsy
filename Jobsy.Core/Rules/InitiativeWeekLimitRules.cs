namespace Jobsy.Core.Rules;

public static class InitiativeWeekLimitRules
{
    public const int CandidateMaxPerWeek = 3;

    /// <summary>Monday 00:00 Europe/Amsterdam for the week containing <paramref name="utcNow"/>.</summary>
    public static DateTime WeekStartAmsterdam(DateTime utcNow)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        var local = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
            tz);
        var diff = ((int)local.DayOfWeek + 6) % 7;
        var monday = local.Date.AddDays(-diff);
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(monday, DateTimeKind.Unspecified),
            tz);
    }

    public static DateTime NextWeekStartAmsterdam(DateTime utcNow)
        => WeekStartAmsterdam(utcNow).AddDays(7);

    public static bool CanSendAnother(int sentThisWeekThatCounts)
        => sentThisWeekThatCounts < CandidateMaxPerWeek;
}
