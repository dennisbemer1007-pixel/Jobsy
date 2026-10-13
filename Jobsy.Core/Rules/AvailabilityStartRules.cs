using System.Globalization;

namespace Jobsy.Core.Rules;

/// <summary>When a candidate can start working (immediate vs future date on User.AvailableFromDate).</summary>
public static class AvailabilityStartRules
{
    public static DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);

    public static bool IsImmediate(DateOnly? availableFromDate) => availableFromDate is null;

    /// <summary>Future start date is valid when present and not before today (UTC).</summary>
    public static bool IsValidExplicitStartDate(DateOnly? date)
        => date is { } d && d >= TodayUtc;

    public static bool StepIsValid(bool immediate, DateOnly? availableFrom)
        => immediate || IsValidExplicitStartDate(availableFrom);

    public static string MinDateInputValue => TodayUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string FormatDisplayDate(DateOnly date, string language)
    {
        var culture = language switch
        {
            "en" => "en-GB",
            "pl" => "pl-PL",
            "ro" => "ro-RO",
            "ar" => "ar",
            _ => "nl-NL"
        };
        return date.ToString("d", CultureInfo.GetCultureInfo(culture));
    }
}
