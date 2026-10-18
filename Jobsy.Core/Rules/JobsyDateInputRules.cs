using System.Globalization;
using System.Text.RegularExpressions;
using Jobsy.Core.Privacy;

namespace Jobsy.Core.Rules;

/// <summary>Parse, mask, validate and format shared date text inputs (day-first, ISO storage).</summary>
public static class JobsyDateInputRules
{
    private static readonly Regex IsoDateRegex = new(
        @"^(?<y>\d{4})-(?<m>\d{1,2})-(?<d>\d{1,2})$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

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

    /// <summary>Native picker value (ISO) or flexible user text.</summary>
    public static bool TryParseDateInput(string? raw, out DateOnly date)
    {
        date = default;
        return !string.IsNullOrWhiteSpace(raw)
               && (TryParseFlexibleDate(raw, out date)
                   || DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date));
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

    public static string Placeholder(string? language)
        => string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
            ? "dd-mm-yyyy"
            : "dd-mm-jjjj";

    /// <summary>User-visible date (nl/en: dd-MM-yyyy, day-first).</summary>
    public static string FormatDisplayDate(DateOnly date, string? language)
    {
        if (string.Equals(language, "nl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(language))
        {
            return date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        }

        return AvailabilityStartRules.FormatDisplayDate(date, language);
    }

    /// <summary>Mask typed digits/separators to dd-MM-yyyy while the user types.</summary>
    public static string ApplyDateMask(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "";
        }

        var trimmed = raw.Trim();
        if (IsoDateRegex.IsMatch(trimmed))
        {
            if (TryParseFlexibleDate(trimmed, out var isoDay))
            {
                return FormatDisplayDate(isoDay, null);
            }
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return "";
        }

        if (digits.Length > 8)
        {
            digits = digits[..8];
        }

        return digits.Length switch
        {
            <= 2 => digits,
            <= 4 => $"{digits[..2]}-{digits[2..]}",
            _ => $"{digits[..2]}-{digits[2..4]}-{digits[4..]}"
        };
    }

    public static bool IsCompleteDisplayDate(string? text)
        => !string.IsNullOrWhiteSpace(text) && text.Trim().Length == 10;

    public static bool TryParseFlexibleDate(string? raw, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var text = raw.Trim();
        var iso = IsoDateRegex.Match(text);
        if (iso.Success
            && int.TryParse(iso.Groups["y"].Value, out var y)
            && int.TryParse(iso.Groups["m"].Value, out var m)
            && int.TryParse(iso.Groups["d"].Value, out var d)
            && TryCreateDate(y, m, d, out date))
        {
            return true;
        }

        var normalized = text.Replace('/', '-').Replace('.', '-');
        var parts = normalized.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var day)
            || !int.TryParse(parts[1], out var month)
            || !int.TryParse(parts[2], out var year))
        {
            return false;
        }

        if (year < 100)
        {
            year += year >= 50 ? 1900 : 2000;
        }

        return TryCreateDate(year, month, day, out date);
    }

    public static JobsyDateValidationCode Validate(
        DateOnly date,
        JobsyDateConstraint constraint,
        DateOnly? reference = null)
    {
        var today = TodayUtc;
        return constraint switch
        {
            JobsyDateConstraint.CandidateBirthDate => ValidateBirthDate(date, today),
            JobsyDateConstraint.AvailabilityStart when date < today => JobsyDateValidationCode.NotPast,
            JobsyDateConstraint.NotFuture when date > today => JobsyDateValidationCode.NotFuture,
            JobsyDateConstraint.NotPast when date < today => JobsyDateValidationCode.NotPast,
            JobsyDateConstraint.VacancyEndAfterStart when reference is { } start && date < start
                => JobsyDateValidationCode.BeforeMin,
            JobsyDateConstraint.None when reference is { } min && date < min => JobsyDateValidationCode.BeforeMin,
            _ => JobsyDateValidationCode.None
        };
    }

    public static JobsyDateValidationCode ValidateText(
        string? text,
        JobsyDateConstraint constraint,
        DateOnly? reference = null,
        bool required = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return required ? JobsyDateValidationCode.Incomplete : JobsyDateValidationCode.None;
        }

        if (!IsCompleteDisplayDate(text) && !TryParseFlexibleDate(text, out _))
        {
            return JobsyDateValidationCode.Incomplete;
        }

        if (!TryParseFlexibleDate(text, out var date))
        {
            return JobsyDateValidationCode.InvalidDate;
        }

        return Validate(date, constraint, reference);
    }

    public static string ValidationMessage(JobsyDateValidationCode code, string? language)
    {
        var en = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);
        return code switch
        {
            JobsyDateValidationCode.Incomplete => en
                ? "Enter the full date (dd-mm-yyyy)."
                : "Vul de volledige datum in (dd-mm-jjjj).",
            JobsyDateValidationCode.InvalidDate => en
                ? "This is not a valid date."
                : "Dit is geen geldige datum.",
            JobsyDateValidationCode.BirthDateFuture => en
                ? "Date of birth cannot be in the future."
                : "Geboortedatum kan niet in de toekomst liggen.",
            JobsyDateValidationCode.BirthDateTooYoung => en
                ? $"You must be at least {CandidateConsentRules.MinimumCandidateAge} years old."
                : $"Je moet minimaal {CandidateConsentRules.MinimumCandidateAge} jaar zijn.",
            JobsyDateValidationCode.BirthDateTooOld => en
                ? "Enter a more recent date of birth."
                : "Vul een recentere geboortedatum in.",
            JobsyDateValidationCode.NotFuture => en
                ? "Choose today or a date in the past."
                : "Kies vandaag of een datum in het verleden.",
            JobsyDateValidationCode.NotPast => en
                ? "Choose today or a date in the future."
                : "Kies vandaag of een datum in de toekomst.",
            JobsyDateValidationCode.BeforeMin => en
                ? "The date is too early."
                : "De datum is te vroeg.",
            JobsyDateValidationCode.AfterMax => en
                ? "The date is too late."
                : "De datum is te laat.",
            _ => ""
        };
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

    private static JobsyDateValidationCode ValidateBirthDate(DateOnly date, DateOnly today)
    {
        if (date > today)
        {
            return JobsyDateValidationCode.BirthDateFuture;
        }

        var youngest = today.AddYears(-CandidateConsentRules.MinimumCandidateAge);
        if (date > youngest)
        {
            return JobsyDateValidationCode.BirthDateTooYoung;
        }

        var oldest = today.AddYears(-100);
        if (date < oldest)
        {
            return JobsyDateValidationCode.BirthDateTooOld;
        }

        return JobsyDateValidationCode.None;
    }

    private static bool TryCreateDate(int year, int month, int day, out DateOnly date)
    {
        date = default;
        if (year is < 1 or > 9999 || month is < 1 or > 12 || day is < 1 or > 31)
        {
            return false;
        }

        try
        {
            date = new DateOnly(year, month, day);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
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
