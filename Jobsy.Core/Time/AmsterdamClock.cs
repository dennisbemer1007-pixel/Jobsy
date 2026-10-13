namespace Jobsy.Core.Time;

/// <summary>Europe/Amsterdam wall clock. Server local time is never used.</summary>
public static class AmsterdamClock
{
    private static readonly TimeZoneInfo Zone = Resolve();

    /// <summary>Send window is 09:00 inclusive through 20:00 exclusive.</summary>
    public static bool IsSendWindow(DateTime utc)
    {
        var hour = ToAmsterdam(utc).Hour;
        return hour is >= 9 and < 20;
    }

    public static DateTime MonthStartUtc(DateTime utc)
    {
        var local = ToAmsterdam(utc);
        var start = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(start, Zone);
    }

    public static DateTime ToAmsterdam(DateTime utc)
    {
        var instant = utc.Kind switch
        {
            DateTimeKind.Utc => utc,
            DateTimeKind.Local => utc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utc, DateTimeKind.Utc)
        };
        return TimeZoneInfo.ConvertTimeFromUtc(instant, Zone);
    }

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "Europe/Amsterdam", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
