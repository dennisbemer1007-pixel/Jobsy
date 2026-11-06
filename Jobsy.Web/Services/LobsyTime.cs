using System.Globalization;

namespace Jobsy.Web.Services;

/// <summary>
/// Amsterdam wall-clock time for candidate copy (04 §3). Never <c>ToLocalTime()</c>: the server
/// zone is not the candidate's zone. Formatting follows the current UI culture, with a Gregorian
/// calendar and Latin digits for Arabic so dates stay readable next to Dutch job data.
/// </summary>
public static class LobsyTime
{
    private static readonly TimeZoneInfo Zone = ResolveDutchTimeZone();

    /// <summary>Deadline line: weekday, day, month and time.</summary>
    public const string DeadlineFormat = "ddd d MMM, HH:mm";

    /// <summary>History line: day and month.</summary>
    public const string DayFormat = "d MMM";

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

    public static string Deadline(DateTime utc, CultureInfo? culture = null)
        => Format(utc, DeadlineFormat, culture);

    public static string Day(DateTime utc, CultureInfo? culture = null)
        => Format(utc, DayFormat, culture);

    private static string Format(DateTime utc, string format, CultureInfo? culture)
        => ToAmsterdam(utc).ToString(format, Display(culture ?? CultureInfo.CurrentUICulture));

    private static CultureInfo Display(CultureInfo culture)
    {
        if (!culture.TwoLetterISOLanguageName.Equals("ar", StringComparison.OrdinalIgnoreCase))
        {
            return culture;
        }

        var latin = (CultureInfo)culture.Clone();
        latin.DateTimeFormat.Calendar = new GregorianCalendar();
        latin.NumberFormat.DigitSubstitution = DigitShapes.None;
        latin.NumberFormat.NativeDigits = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];
        return latin;
    }

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
