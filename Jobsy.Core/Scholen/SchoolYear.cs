namespace Jobsy.Core.Scholen;

/// <summary>School-year calendar helpers (default cutoff 31 July).</summary>
public static class SchoolYear
{
    public const int DefaultCutoffMonth = 7;
    public const int DefaultCutoffDay = 31;

    /// <summary>
    /// Returns the school-year start calendar year for <paramref name="today"/>.
    /// E.g. 29-09-2026 → 2026; 15-07-2027 → 2026; 01-08-2027 → 2027 with the 31-07 default.
    /// </summary>
    public static int Current(DateOnly today, int cutoffMonth = DefaultCutoffMonth, int cutoffDay = DefaultCutoffDay)
    {
        ValidateCutoff(cutoffMonth, cutoffDay);
        var cutoffThisYear = SafeDate(today.Year, cutoffMonth, cutoffDay);
        return today <= cutoffThisYear ? today.Year - 1 : today.Year;
    }

    public static string Label(int schoolYearStart) => $"{schoolYearStart}–{schoolYearStart + 1}";

    public static DateOnly EndsOn(
        int schoolYearStart,
        int cutoffMonth = DefaultCutoffMonth,
        int cutoffDay = DefaultCutoffDay)
    {
        ValidateCutoff(cutoffMonth, cutoffDay);
        return SafeDate(schoolYearStart + 1, cutoffMonth, cutoffDay);
    }

    public static void ValidateCutoff(int month, int day)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "Maand moet 1–12 zijn.");
        }

        try
        {
            _ = new DateOnly(2024, month, day); // leap-safe probe year
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ArgumentOutOfRangeException(nameof(day), ex.Message);
        }
    }

    private static DateOnly SafeDate(int year, int month, int day)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Min(day, daysInMonth));
    }
}
