using System.Globalization;

namespace Jobsy.Web.Localization;

/// <summary>Gregorian calendar date in Europe/Amsterdam. Arabic UI must not switch to Hijri.</summary>
public static class NlDate
{
    public static string Day(DateTime utc)
    {
        var instant = utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        var local = TimeZoneInfo.ConvertTimeFromUtc(instant, zone);
        return local.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    }
}
