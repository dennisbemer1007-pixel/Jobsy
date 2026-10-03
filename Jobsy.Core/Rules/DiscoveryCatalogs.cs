namespace Jobsy.Core.Rules;

/// <summary>
/// Catalog codes for the Ontdekkingsreis / passport discovery fields.
/// Localized labels live under Discovery.* in UiStringsDiscovery.
/// Language display names come from <see cref="System.Globalization.CultureInfo"/> (no string keys).
/// </summary>
public static class DiscoveryCatalogs
{
    public static readonly string[] SpokenLanguageCodes =
    [
        "nl", "en", "ar", "tr", "pl", "ro", "uk", "so", "ti", "fa", "es", "fr", "de"
    ];

    /// <summary>Languages offered in “add a language”. Dutch is the separate Dutch-level field.</summary>
    public static IEnumerable<string> AddableSpokenLanguageCodes
        => SpokenLanguageCodes.Where(code => !string.Equals(code, "nl", StringComparison.OrdinalIgnoreCase));

    public static readonly string[] DutchLevels =
    [
        "beginner", "basis", "goed", "vloeiend", "moedertaal"
    ];

    public static readonly string[] EmployerPreferenceCodes =
    [
        "small-team", "large-company", "fixed-workplace", "learn-on-the-job",
        "dutch-support", "growth", "close-to-home", "variety"
    ];

    public static readonly string[] HobbyCodes =
    [
        "sport", "music", "cooking", "gaming", "crafts", "nature",
        "reading", "caring", "tech", "art", "volunteering", "fashion"
    ];

    public static readonly string[] DislikeCodes =
    [
        "night-shifts", "heavy-lifting", "working-alone", "phone-customers", "noise",
        "cold-outdoor", "computer-work", "changing-hours", "crowded", "long-travel"
    ];

    public const int MaxSpokenLanguages = 8;
    public const int MaxLearningGoals = 5;
    public const int MaxLearningGoalLength = 60;
    public const int MaxHobbies = 10;
    public const int MaxHobbyFreeTextLength = 40;
    public const int MaxCustomDislikes = 5;
    public const int MaxCustomDislikeLength = 40;

    public static string DutchLabelKey(string code) => $"Discovery.Dutch.{CanonicalDutch(code) ?? code}";
    public static string DutchExplainKey(string code) => $"Discovery.Dutch.{CanonicalDutch(code) ?? code}.Explain";
    public static string EmployerLabelKey(string code) => $"Discovery.Employer.{CanonicalEmployer(code) ?? code}";
    public static string HobbyLabelKey(string code) => $"Discovery.Hobby.{CanonicalHobby(code) ?? code}";
    public static string DislikeLabelKey(string code) => $"Discovery.Dislike.{CanonicalDislike(code) ?? code}";

    public static bool IsKnownDutchLevel(string? code)
        => !string.IsNullOrWhiteSpace(code) && CanonicalDutch(code) is not null;

    public static bool IsKnownEmployerPreference(string? code)
        => !string.IsNullOrWhiteSpace(code) && CanonicalEmployer(code) is not null;

    public static bool IsKnownHobby(string? code)
        => !string.IsNullOrWhiteSpace(code) && CanonicalHobby(code) is not null;

    public static bool IsKnownDislike(string? code)
        => !string.IsNullOrWhiteSpace(code) && CanonicalDislike(code) is not null;

    public static bool IsCuratedLanguage(string? code)
        => !string.IsNullOrWhiteSpace(code)
           && SpokenLanguageCodes.Any(c => string.Equals(c, code.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Accept ISO 639-1 (2 letters) or curated 3-letter codes (e.g. so, ti already 2).</summary>
    public static bool IsPlausibleLanguageCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var trimmed = code.Trim().ToLowerInvariant();
        if (IsCuratedLanguage(trimmed))
        {
            return true;
        }

        return trimmed.Length is >= 2 and <= 3 && trimmed.All(char.IsLetter);
    }

    public static string? CanonicalDutch(string? code)
        => FindCanonical(DutchLevels, code);

    public static string? CanonicalEmployer(string? code)
        => FindCanonical(EmployerPreferenceCodes, code);

    public static string? CanonicalHobby(string? code)
        => FindCanonical(HobbyCodes, code);

    public static string? CanonicalDislike(string? code)
        => FindCanonical(DislikeCodes, code);

    public static string? CanonicalLanguage(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || !IsPlausibleLanguageCode(code))
        {
            return null;
        }

        var trimmed = code.Trim().ToLowerInvariant();
        var curated = SpokenLanguageCodes.FirstOrDefault(c =>
            string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
        return curated ?? trimmed;
    }

    private static string? FindCanonical(IReadOnlyList<string> catalog, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var trimmed = code.Trim();
        return catalog.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
