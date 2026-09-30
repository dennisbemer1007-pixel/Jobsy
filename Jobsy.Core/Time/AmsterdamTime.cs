using System.Globalization;

namespace Jobsy.Core.Time;

/// <summary>Europe/Amsterdam calendar helpers for mail copy and consent expiry lines.</summary>
public static class AmsterdamTime
{
    private static readonly TimeZoneInfo Zone = ResolveDutchTimeZone();
    private static readonly CultureInfo DutchCulture = CultureInfo.GetCultureInfo("nl-NL");

    public static DateTime ToLocal(DateTime utc)
    {
        var instant = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(instant, Zone);
    }

    public static string FormatDate(DateTime utc, CultureInfo? culture = null)
        => ToLocal(utc).ToString("d MMMM yyyy", culture ?? DutchCulture);

    public static string FormatDateTime(DateTime utc, CultureInfo? culture = null)
        => ToLocal(utc).ToString("d MMMM yyyy, HH:mm", culture ?? DutchCulture);

    private static TimeZoneInfo ResolveDutchTimeZone()
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
