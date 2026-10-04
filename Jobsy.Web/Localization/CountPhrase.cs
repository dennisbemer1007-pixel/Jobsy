using System.Globalization;

namespace Jobsy.Web.Localization;

/// <summary>
/// Plural phrases for nl, en, pl (1 / 2–4 / 5+ and teens), ro, and ar (0, 1, 2, 3–10, 11+).
/// </summary>
public static class CountPhrase
{
    public static string Tokens(CultureState culture, decimal amount)
    {
        if (amount == decimal.Truncate(amount))
        {
            return Phrase(culture, (int)amount, "Count.Token");
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            culture["Count.Token.Many"],
            amount.ToString("0.##", CultureInfo.InvariantCulture));
    }

    public static string Vacancies(CultureState culture, int count)
        => Phrase(culture, count, "Count.Vacancy");

    public static string Applications(CultureState culture, int count)
        => Phrase(culture, count, "Count.Application");

    public static string Form(string? language, int count)
    {
        var lang = (language ?? "nl").Trim().ToLowerInvariant();
        var n = Math.Abs(count);
        if (lang.StartsWith("pl", StringComparison.Ordinal))
        {
            if (n == 1)
            {
                return "One";
            }

            var mod10 = n % 10;
            var mod100 = n % 100;
            if (mod10 is >= 2 and <= 4 && mod100 is not (>= 12 and <= 14))
            {
                return "Few";
            }

            return "Many";
        }

        if (lang.StartsWith("ar", StringComparison.Ordinal))
        {
            if (n == 0)
            {
                return "Many";
            }

            if (n == 1)
            {
                return "One";
            }

            if (n == 2)
            {
                return "Two";
            }

            if (n % 100 is >= 3 and <= 10)
            {
                return "Few";
            }

            return "Many";
        }

        return n == 1 ? "One" : "Many";
    }

    private static string Phrase(CultureState culture, int count, string stem)
    {
        var form = Form(culture.Language, count);
        var template = culture[$"{stem}.{form}"];
        if (string.Equals(template, $"{stem}.{form}", StringComparison.Ordinal))
        {
            template = culture[$"{stem}.Many"];
        }

        return string.Format(CultureInfo.InvariantCulture, template, count);
    }
}
