namespace Jobsy.Core.Rules;

/// <summary>Visible year range for employer history (CV / paspoort bewijzen).</summary>
public static class CandidateEmployerYearRangeFormat
{
    public static string Format(string? startMonth, string? endMonth, bool isCurrent, string presentLabel)
    {
        var start = Year(startMonth);
        if (isCurrent)
        {
            return string.IsNullOrEmpty(start) ? presentLabel : $"{start} - {presentLabel}";
        }

        var end = Year(endMonth);
        if (string.IsNullOrEmpty(start))
        {
            return end;
        }

        if (string.IsNullOrEmpty(end))
        {
            return start;
        }

        return $"{start} - {end}";
    }

    private static string Year(string? yyyyMm)
        => string.IsNullOrWhiteSpace(yyyyMm)
            ? string.Empty
            : yyyyMm.Length >= 4
                ? yyyyMm[..4]
                : yyyyMm.Trim();
}
