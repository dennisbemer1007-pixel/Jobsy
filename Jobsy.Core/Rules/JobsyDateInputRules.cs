using System.Globalization;
using Jobsy.Core.Privacy;

namespace Jobsy.Core.Rules;

/// <summary>Min/max and formatting for native <c>type=date</c> / <c>type=month</c> inputs.</summary>
public static class JobsyDateInputRules
{
    public static DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);

    public static string ToDateInputValue(DateOnly? date)
        => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";

    public static string ToMonthInputValue(string? yyyyMm)
    {
        if (string.IsNullOrWhiteSpace(yyyyMm))
        {
            return "";
        }

        var parts = yyyyMm.Split('-', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var year)
            || !int.TryParse(parts[1], out var month)
            || month is < 1 or > 12)
        {
            return "";
        }

        return $"{year:0000}-{month:00}";
    }

    public static bool TryParseDateInput(string? raw, out DateOnly date)
    {
        date = default;
        return !string.IsNullOrWhiteSpace(raw)
               && DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    public static bool TryParseMonthInput(string? raw, out string? yyyyMm)
    {
        yyyyMm = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (!DateOnly.TryParse(raw + "-01", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first))
        {
            return false;
        }

        yyyyMm = $"{first.Year:0000}-{first.Month:00}";
        return true;
    }

    public static (string? Min, string? Max) Bounds(JobsyDateConstraint constraint, DateOnly? reference = null)
    {
        var today = TodayUtc;
        return constraint switch
        {
            JobsyDateConstraint.CandidateBirthDate => (
                ToDateInputValue(today.AddYears(-100)),
                ToDateInputValue(today.AddYears(-CandidateConsentRules.MinimumCandidateAge))),
            JobsyDateConstraint.AvailabilityStart => (AvailabilityStartRules.MinDateInputValue, null),
            JobsyDateConstraint.NotFuture => (null, ToDateInputValue(today)),
            JobsyDateConstraint.NotPast => (ToDateInputValue(today), null),
            JobsyDateConstraint.WorkHistoryMonth => (
                "1950-01",
                ToMonthInputValue($"{today.Year:0000}-{today.Month:00}")),
            JobsyDateConstraint.VacancyEndAfterStart when reference is { } start => (
                ToDateInputValue(start),
                null),
            _ => (null, null)
        };
    }
}

public enum JobsyDateConstraint
{
    None = 0,
    CandidateBirthDate,
    AvailabilityStart,
    WorkHistoryMonth,
    NotFuture,
    NotPast,
    VacancyEndAfterStart
}
