namespace Jobsy.Web.Applications;

internal static class AfterMatchMailSsrTime
{
    private static readonly TimeZoneInfo Amsterdam = ResolveAmsterdam();

    /// <summary>Wall clock in Europe/Amsterdam (SSR hosts run UTC).</summary>
    internal static DateTime ToAmsterdam(DateTime utc)
    {
        var u = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(u, Amsterdam);
    }

    internal static DateTime ToUtcFromAmsterdamWallClock(DateTime amsterdamLocalUnspecified)
        => TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(amsterdamLocalUnspecified, DateTimeKind.Unspecified),
            Amsterdam);

    private static TimeZoneInfo ResolveAmsterdam()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }
}
