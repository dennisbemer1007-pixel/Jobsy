using System.Globalization;

namespace Jobsy.Core.Localization;

/// <summary>
/// Thread culture for one circuit. Never touches <see cref="CultureInfo.DefaultThreadCurrentCulture"/>.
/// Arabic uses a Gregorian calendar so dates stay 2026, not a Hijri year.
/// </summary>
public static class JobsyCultures
{
    public static CultureInfo For(string? language)
    {
        var name = JobsyLanguages.ToCultureName(language);
        CultureInfo source;
        try
        {
            source = CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            source = CultureInfo.GetCultureInfo("nl-NL");
        }

        if (source.DateTimeFormat.Calendar is GregorianCalendar)
        {
            return (CultureInfo)source.Clone();
        }

        var gregorian = source.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault();
        if (gregorian is null)
        {
            return (CultureInfo)CultureInfo.InvariantCulture.Clone();
        }

        var clone = (CultureInfo)source.Clone();
        clone.DateTimeFormat.Calendar = gregorian;
        return clone;
    }

    public static void ApplyToCurrentThread(string? language)
    {
        var culture = For(language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
