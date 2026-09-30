using System.Globalization;
using System.Text;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Localization;
using Jobsy.Core.Time;

namespace Jobsy.Core.Email;

/// <summary>Culture-aware dates, money and distances for mail copy (Europe/Amsterdam, Gregorian, Latin digits).</summary>
public static class EmailFormat
{
    public static CultureInfo CultureInfoFor(EmailCulture culture)
    {
        var info = (CultureInfo)CultureInfo.GetCultureInfo(JobsyLanguages.ToCultureName(culture.Language)).Clone();
        if (string.Equals(culture.Language, "ar", StringComparison.OrdinalIgnoreCase))
        {
            info.DateTimeFormat.Calendar = new GregorianCalendar();
            info.NumberFormat.DigitSubstitution = DigitShapes.None;
            info.NumberFormat.NativeDigits = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];
        }

        return info;
    }

    public static string Date(DateTime utc, EmailCulture culture)
        => AmsterdamTime.FormatDate(utc, CultureInfoFor(culture));

    public static string DateTime(DateTime utc, EmailCulture culture)
    {
        var formatted = AmsterdamTime.FormatDateTime(utc, CultureInfoFor(culture));
        var zone = EmailStrings.Get(culture, "Email.Common.TimeZoneNl");
        return $"{formatted} ({zone})";
    }

    public static string DateTimeWithoutZone(DateTime utc, EmailCulture culture)
        => AmsterdamTime.FormatDateTime(utc, CultureInfoFor(culture));

    public static string Money(decimal amount, EmailCulture culture)
    {
        var ci = CultureInfoFor(culture);
        var number = amount.ToString("N2", ci);
        return $"€ {number}";
    }

    public static string Km(double km, EmailCulture culture)
    {
        var ci = CultureInfoFor(culture);
        var number = km.ToString("0.0", ci);
        return EmailStrings.FormatRaw(culture, "Email.Common.Km", number);
    }

    public static string Minutes(int minutes, EmailCulture culture)
        => EmailStrings.FormatRaw(culture, "Email.Common.Minutes", minutes);

    // Back-compat aliases used by templates ported in 02 (nl until callers pass culture).
    public static string FormatEuro(decimal amount) => Money(amount, EmailCulture.Nl);

    public static string FormatKm(double km) => Km(km, EmailCulture.Nl);
}

/// <summary>Unicode bidi isolation for RTL subject/preheader/text user data.</summary>
public static class EmailBidi
{
    public const char FirstStrongIsolate = '\u2068';
    public const char PopDirectionalIsolate = '\u2069';

    public static string Isolate(EmailCulture culture, string? value)
    {
        var text = value ?? string.Empty;
        if (!culture.IsRightToLeft || string.IsNullOrEmpty(text))
        {
            return text;
        }

        return FirstStrongIsolate + text + PopDirectionalIsolate;
    }

    public static string StripIsolates(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch is FirstStrongIsolate or PopDirectionalIsolate)
            {
                continue;
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }
}
