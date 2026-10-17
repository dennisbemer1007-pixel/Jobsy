using Jobsy.Core.Options;

namespace Jobsy.Core.Localization;

/// <summary>
/// Supported UI / content languages. Default is Dutch (nl).
/// <see cref="All"/> is the full catalog; <see cref="Enabled"/> is what users may select (see <see cref="LocalizationOptions"/>).
/// </summary>
public static class JobsyLanguages
{
    public const string Default = "nl";

    private static readonly LanguageOption[] Catalog =
    [
        new("nl", "Nederlands", "🇳🇱", "nl-NL", false),
        new("en", "English", "🇬🇧", "en-GB", false),
        new("pl", "Polski", "🇵🇱", "pl-PL", false),
        new("ro", "Română", "🇷🇴", "ro-RO", false),
        new("ar", "العربية", "🇸🇦", "ar-SA", true)
    ];

    public static IReadOnlyList<LanguageOption> All { get; } = Catalog;

    private static IReadOnlyList<LanguageOption> _enabled = BuildEnabled(ParseEnabledCodes(LocalizationOptions.DefaultEnabledCultures));

    public static IReadOnlyList<LanguageOption> Enabled => _enabled;

    public static void Configure(LocalizationOptions? options)
        => ConfigureEnabledCultures(options?.EnabledCultures);

    public static void ConfigureEnabledCultures(string? enabledCulturesCsv)
    {
        var raw = string.IsNullOrWhiteSpace(enabledCulturesCsv)
            ? LocalizationOptions.DefaultEnabledCultures
            : enabledCulturesCsv;
        _enabled = BuildEnabled(ParseEnabledCodes(raw));
    }

    /// <summary>Test hook: restore launch default (<c>nl,en</c>).</summary>
    internal static void ResetEnabledForTests()
        => _enabled = BuildEnabled(ParseEnabledCodes(LocalizationOptions.DefaultEnabledCultures));

    public static bool IsCatalogLanguage(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var primary = PrimaryCode(code);
        return All.Any(l => l.Code == primary);
    }

    /// <summary>True when the code is in the catalog and currently enabled for UI/API/cookies.</summary>
    public static bool IsSupported(string? code) => IsEnabled(code);

    public static bool IsEnabled(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var primary = PrimaryCode(code);
        return Enabled.Any(l => l.Code == primary);
    }

    /// <summary>
    /// Normalizes culture/language tags to a supported two-letter catalog code, or <see cref="Default"/>.
    /// Does not apply the enabled-cultures filter (use <see cref="ResolveActive"/> for runtime UI).
    /// </summary>
    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Default;
        }

        var primary = PrimaryCode(code);
        return All.Any(l => l.Code == primary) ? primary : Default;
    }

    /// <summary>
    /// Maps a stored or requested language to an enabled code. Disabled catalog languages fall back via
    /// <paramref name="acceptLanguage"/> (English when the browser prefers it, otherwise Dutch).
    /// </summary>
    public static string ResolveActive(string? code, string? acceptLanguage = null)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            var primary = PrimaryCode(code);
            if (IsEnabled(primary))
            {
                return primary;
            }

            if (IsCatalogLanguage(primary))
            {
                return ResolveActiveFromAcceptLanguage(acceptLanguage) ?? Default;
            }
        }

        return ResolveActiveFromAcceptLanguage(acceptLanguage) ?? Default;
    }

    /// <summary>Highest-quality enabled language in an <c>Accept-Language</c> header, else null.</summary>
    public static string? ResolveActiveFromAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        var best = (Code: (string?)null, Quality: -1d);
        foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var segments = part.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            if (!TryEnabledCode(segments[0], out var code))
            {
                continue;
            }

            var quality = 1d;
            foreach (var segment in segments.Skip(1))
            {
                if (segment.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                    && double.TryParse(
                        segment[2..],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var parsed))
                {
                    quality = parsed;
                }
            }

            if (quality > best.Quality)
            {
                best = (code, quality);
            }
        }

        return best.Quality > 0 ? best.Code : null;
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

    private static IReadOnlyList<LanguageOption> BuildEnabled(IReadOnlyList<string> codes)
    {
        var list = new List<LanguageOption>(codes.Count);
        foreach (var code in codes)
        {
            var option = All.FirstOrDefault(l => l.Code == code);
            if (option is not null)
            {
                list.Add(option);
            }
        }

        if (list.Count == 0)
        {
            list.Add(All[0]);
            if (!list.Any(l => l.Code == "en"))
            {
                list.Add(All[1]);
            }
        }

        if (!list.Any(l => l.Code == Default))
        {
            list.Insert(0, All[0]);
        }

        return list;
    }

    private static IReadOnlyList<string> ParseEnabledCodes(string raw)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<string>();
        foreach (var segment in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var code = PrimaryCode(segment);
            if (!IsCatalogLanguage(code) || !seen.Add(code))
            {
                continue;
            }

            list.Add(code);
        }

        return list.Count == 0
            ? ParseEnabledCodes(LocalizationOptions.DefaultEnabledCultures)
            : list;
    }

    private static string PrimaryCode(string code)
        => code.Trim().Replace('_', '-').Split('-', 2)[0].ToLowerInvariant();

    /// <summary>True only for an explicitly enabled code (does not treat unknowns as nl).</summary>
    public static string EnabledCodesList()
        => string.Join(", ", Enabled.Select(l => l.Code));

    public static bool TryEnabledCode(string? code, out string normalized)
    {
        normalized = Default;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var primary = PrimaryCode(code);
        if (!IsEnabled(primary))
        {
            return false;
        }

        normalized = primary;
        return true;
    }
}

public sealed record LanguageOption(
    string Code,
    string NativeName,
    string FlagEmoji,
    string CultureName,
    bool IsRightToLeft)
{
    /// <summary>
    /// Local SVG flag path (emoji flags often fail on Windows).
    /// </summary>
    public string FlagImage => Code switch
    {
        "en" => "images/flags/gb.svg",
        "ar" => "images/flags/sa.svg",
        _ => $"images/flags/{Code}.svg"
    };
}
