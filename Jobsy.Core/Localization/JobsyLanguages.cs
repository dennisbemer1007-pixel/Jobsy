namespace Jobsy.Core.Localization;

/// <summary>
/// Supported UI / content languages. Default is Dutch (nl).
/// </summary>
public static class JobsyLanguages
{
    public const string Default = "nl";

    public static readonly IReadOnlyList<LanguageOption> All =
    [
        new("nl", "Nederlands", "🇳🇱", "nl-NL", false),
        new("en", "English", "🇬🇧", "en-GB", false),
        new("pl", "Polski", "🇵🇱", "pl-PL", false),
        new("ro", "Română", "🇷🇴", "ro-RO", false),
        new("ar", "العربية", "🇸🇦", "ar-SA", true)
    ];

    public static bool IsSupported(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = Normalize(code);
        return All.Any(l => l.Code == normalized);
    }

    /// <summary>
    /// Normalizes culture/language tags to a supported two-letter code, or <see cref="Default"/>.
    /// </summary>
    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Default;
        }

        var primary = code.Trim().Replace('_', '-').Split('-', 2)[0].ToLowerInvariant();
        return All.Any(l => l.Code == primary) ? primary : Default;
    }

    public static bool AreSame(string? a, string? b)
        => string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);

    public static LanguageOption Get(string? code)
    {
        var normalized = Normalize(code);
        return All.First(l => l.Code == normalized);
    }

    public static string ToCultureName(string? code)
        => Get(code).CultureName;
}

public sealed record LanguageOption(
    string Code,
    string NativeName,
    string FlagEmoji,
    string CultureName,
    bool IsRightToLeft);
