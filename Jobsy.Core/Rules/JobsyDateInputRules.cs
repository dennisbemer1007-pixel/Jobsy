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

    private static readonly Regex IsoMonthRegex = new(
        @"^(?<y>\d{4})-(?<m>\d{1,2})$",
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

        if (TryParseFlexibleMonth(raw, out yyyyMm))
        {
            return true;
        }

        if (!DateOnly.TryParse(raw + "-01", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first))
        {
            return false;
        }

        yyyyMm = $"{first.Year:0000}-{first.Month:00}";
        return true;
    }

    /// <summary>User-visible month (mm-yyyy).</summary>
    public static string FormatDisplayMonth(string? yyyyMm, string? language)
    {
        _ = language;
        if (!TryParseFlexibleMonth(yyyyMm, out var normalized) || normalized is null)
        {
            return yyyyMm ?? "";
        }

        var parts = normalized.Split('-', 2);
        return $"{parts[1]}-{parts[0]}";
    }

    /// <summary>Mask typed digits to mm-yyyy while the user types.</summary>
    public static string ApplyMonthMask(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "";
        }

        var trimmed = raw.Trim();
        if (IsoMonthRegex.IsMatch(trimmed) && TryParseFlexibleMonth(trimmed, out var iso))
        {
            return FormatDisplayMonth(iso, null);
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return "";
        }

        if (digits.Length > 6)
        {
            digits = digits[..6];
        }

        return digits.Length switch
        {
            <= 2 => digits,
            _ => $"{digits[..2]}-{digits[2..]}"
        };
    }

    public static bool IsCompleteDisplayMonth(string? text)
        => !string.IsNullOrWhiteSpace(text) && text.Trim().Length is >= 6 and <= 7;

    public static bool TryParseFlexibleMonth(string? raw, out string? yyyyMm)
    {
        yyyyMm = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var text = raw.Trim();
        var iso = IsoMonthRegex.Match(text);
        if (iso.Success
            && int.TryParse(iso.Groups["y"].Value, out var isoYear)
            && int.TryParse(iso.Groups["m"].Value, out var isoMonth)
            && TryCreateMonth(isoYear, isoMonth, out yyyyMm))
        {
            return true;
        }

        var normalized = text.Replace('/', '-').Replace('.', '-');
        var parts = normalized.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var first) || !int.TryParse(parts[1], out var second))
        {
            return false;
        }

        int month;
        int year;
        if (first > 31)
        {
            year = first;
            month = second;
        }
        else
        {
            month = first;
            year = second;
        }

        if (year < 100)
        {
            year += year >= 50 ? 1900 : 2000;
        }

        return TryCreateMonth(year, month, out yyyyMm);
    }

    public static JobsyDateValidationCode ValidateMonth(
        string? yyyyMm,
        JobsyDateConstraint constraint,
        string? referenceMonth = null)
    {
        if (!TryParseFlexibleMonth(yyyyMm, out var normalized) || normalized is null)
        {
            return JobsyDateValidationCode.InvalidMonth;
        }

        var today = TodayUtc;
        var current = ToMonthInputValue($"{today.Year:0000}-{today.Month:00}");
        if (constraint is JobsyDateConstraint.WorkHistoryMonth or JobsyDateConstraint.WorkHistoryEndAfterStart)
        {
            if (string.CompareOrdinal(normalized, current) > 0)
            {
                return JobsyDateValidationCode.NotFuture;
            }

            if (string.CompareOrdinal(normalized, "1950-01") < 0)
            {
                return JobsyDateValidationCode.BeforeMin;
            }
        }

        if (constraint == JobsyDateConstraint.WorkHistoryEndAfterStart
            && TryParseFlexibleMonth(referenceMonth, out var start)
            && start is not null
            && string.CompareOrdinal(normalized, start) < 0)
        {
            return JobsyDateValidationCode.MonthBeforeStart;
        }

        return JobsyDateValidationCode.None;
    }

    public static string MonthValidationMessage(JobsyDateValidationCode code, string? language)
    {
        if (code == JobsyDateValidationCode.Incomplete)
        {
            var en = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);
            return en
                ? "Enter the full month (mm-yyyy)."
                : "Vul de volledige maand in (mm-jjjj).";
        }

        return ValidationMessage(code, language);
    }

    public static JobsyDateValidationCode ValidateMonthText(
        string? text,
        JobsyDateConstraint constraint,
        string? referenceMonth = null,
        bool required = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return required ? JobsyDateValidationCode.Incomplete : JobsyDateValidationCode.None;
        }

        if (!IsCompleteDisplayMonth(text) && !TryParseFlexibleMonth(text, out _))
        {
            return JobsyDateValidationCode.Incomplete;
        }

        if (!TryParseFlexibleMonth(text, out var yyyyMm))
        {
            return JobsyDateValidationCode.InvalidMonth;
        }

        return ValidateMonth(yyyyMm, constraint, referenceMonth);
    }

    public static string Placeholder(string? language)
        => string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
            ? "dd-mm-yyyy"
            : "dd-mm-jjjj";

    public static string MonthPlaceholder(string? language)
        => string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
            ? "mm-yyyy"
            : "mm-jjjj";

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

    /// <summary>Mask vacancy CSV einddatum: mm-yyyy (≤6 digits) or dd-mm-yyyy (7+ digits).</summary>
    public static string ApplyVacancyCsvDateMask(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "";
        }

        var trimmed = raw.Trim();
        if (IsoDateRegex.IsMatch(trimmed) && TryParseFlexibleDate(trimmed, out var isoDay))
        {
            return FormatDisplayDate(isoDay, null);
        }

        if (TryParseFlexibleMonth(trimmed, out var isoMonth) && isoMonth is not null)
        {
            return FormatDisplayMonth(isoMonth, null);
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return "";
        }

        var normalized = trimmed.Replace('/', '-').Replace('.', '-');
        var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 3 || digits.Length > 6)
        {
            return ApplyDateMask(raw);
        }

        if (parts.Length == 2 && parts[1].Length is >= 1 and <= 2)
        {
            return ApplyDateMask(raw);
        }

        var monthMasked = ApplyMonthMask(raw);
        if (digits.Length == 6 && TryParseFlexibleMonth(monthMasked, out _))
        {
            return monthMasked;
        }

        if (digits.Length < 6)
        {
            return monthMasked;
        }

        return ApplyDateMask(raw);
    }

    public static bool IsCompleteVacancyCsvDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return IsCompleteDisplayDate(text) || IsCompleteDisplayMonth(text);
    }

    public static bool TryParseVacancyCsvDate(string? raw, out DateOnly date)
    {
        if (TryParseFlexibleDate(raw, out date))
        {
            return true;
        }

        if (!TryParseFlexibleMonth(raw, out var yyyyMm) || yyyyMm is null)
        {
            date = default;
            return false;
        }

        var parts = yyyyMm.Split('-', 2);
        if (!int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month))
        {
            date = default;
            return false;
        }

        date = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        return true;
    }

    public static JobsyDateValidationCode ValidateVacancyCsvDateText(
        string? text,
        bool required = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return required ? JobsyDateValidationCode.Incomplete : JobsyDateValidationCode.None;
        }

        if (TryParseVacancyCsvDate(text, out _))
        {
            return JobsyDateValidationCode.None;
        }

        if (!IsCompleteVacancyCsvDate(text))
        {
            if (IsCompleteDisplayMonth(text))
            {
                return JobsyDateValidationCode.InvalidMonth;
            }

            return JobsyDateValidationCode.Incomplete;
        }

        return JobsyDateValidationCode.InvalidDate;
    }

    public static string VacancyCsvDatePlaceholder(string? language)
        => string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
            ? "dd-mm-yyyy or mm-yyyy"
            : "dd-mm-jjjj of mm-jjjj";

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
            JobsyDateValidationCode.InvalidMonth => en
                ? "This is not a valid month."
                : "Dit is geen geldige maand.",
            JobsyDateValidationCode.MonthBeforeStart => en
                ? "End month must be the same as or after the start month."
                : "Eindmaand moet gelijk zijn aan of na de startmaand liggen.",
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
            JobsyDateConstraint.WorkHistoryMonth or JobsyDateConstraint.WorkHistoryEndAfterStart => (
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

    private static bool TryCreateMonth(int year, int month, out string? yyyyMm)
    {
        yyyyMm = null;
        if (year is < 1950 or > 2100 || month is < 1 or > 12)
        {
            return false;
        }

        yyyyMm = $"{year:0000}-{month:00}";
        return true;
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
    WorkHistoryEndAfterStart,
    NotFuture,
    NotPast,
    VacancyEndAfterStart,
    VacancyCsvDate
}
