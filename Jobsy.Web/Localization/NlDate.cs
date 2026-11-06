using System.Globalization;
using Jobsy.Core.Time;

namespace Jobsy.Web.Localization;

/// <summary>Gregorian calendar date in Europe/Amsterdam. Arabic UI must not switch to Hijri.</summary>
public static class NlDate
{
    public static string Day(DateTime utc)
    {
        var instant = utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
        var local = AmsterdamTime.ToLocal(instant);
        return local.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    }
}
