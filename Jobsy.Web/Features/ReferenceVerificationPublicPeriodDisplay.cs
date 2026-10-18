namespace Jobsy.Web.Features;

/// <summary>Human-readable work periods on the public referent form.</summary>
public static class ReferenceVerificationPublicPeriodDisplay
{
    private static readonly string[] DutchMonths =
    [
        "januari", "februari", "maart", "april", "mei", "juni",
        "juli", "augustus", "september", "oktober", "november", "december"
    ];

    public static string FormatMonth(string? yyyyMm, bool dutch)
    {
        if (string.IsNullOrWhiteSpace(yyyyMm) || yyyyMm.Length < 7)
        {
            return "…";
        }

        if (!int.TryParse(yyyyMm.AsSpan(0, 4), out var year)
            || !int.TryParse(yyyyMm.AsSpan(5, 2), out var month)
            || month is < 1 or > 12)
        {
            return yyyyMm;
        }

        if (dutch)
        {
            return $"{DutchMonths[month - 1]} {year}";
        }

        return new DateTime(year, month, 1).ToString(
            "MMMM yyyy",
            System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
    }

    public static string HumanRange(string startMonth, string? endMonth, bool isCurrent, bool dutch)
    {
        var start = FormatMonth(startMonth, dutch);
        var end = isCurrent
            ? (dutch ? "nu" : "now")
            : FormatMonth(endMonth, dutch);
        return dutch ? $"{start} tot {end}" : $"{start} to {end}";
    }
}
