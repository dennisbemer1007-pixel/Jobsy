using System.Reflection;
using System.Text.RegularExpressions;
using Jobsy.Web.Localization;
using Xunit.Abstractions;

namespace Jobsy.Tests;

/// <summary>
/// Localization guard (cleanup prompt 14): every key in all languages (hard fail);
/// identical-to-nl debt must not increase vs <c>docs/i18n/untranslated-baseline.txt</c>.
/// </summary>
public sealed class LocalizationParityReportTests
{
    private static readonly string[] Languages = ["nl", "en", "pl", "ro", "ar"];
    private static readonly string[] NonDefaultLanguages = ["en", "pl", "ro", "ar"];

    private readonly ITestOutputHelper _output;

    public LocalizationParityReportTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Every_key_exists_in_all_languages()
    {
        var catalog = LoadCatalog();
        Assert.True(catalog.ContainsKey("nl"));
        var nlKeys = catalog["nl"].Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.NotEmpty(nlKeys);

        foreach (var lang in Languages)
        {
            Assert.True(catalog.ContainsKey(lang), $"Missing language catalog: {lang}");
            // D12: Scholen/Sales module keys are nl-only; other languages fall back via UiStrings.Get.
            var missing = nlKeys
                .Where(k => !UiStringsScholen.IsNlOnlyPrefix(k)
                            && !UiStringsSales.IsNlOnlyPrefix(k)
                            && !catalog[lang].ContainsKey(k))
                .ToList();
            Assert.True(
                missing.Count == 0,
                $"Language {lang} missing {missing.Count} keys. First: {string.Join(", ", missing.Take(10))}");
            var extra = catalog[lang].Keys.Where(k => !catalog["nl"].ContainsKey(k)).ToList();
            Assert.True(
                extra.Count == 0,
                $"Language {lang} has {extra.Count} keys not in nl. First: {string.Join(", ", extra.Take(10))}");
        }
    }

    [Fact]
    public void Identical_to_nl_counts_do_not_increase_vs_baseline()
    {
        var catalog = LoadCatalog();
        var nl = catalog["nl"];
        var baselinePath = Path.Combine(FindRepoRoot(), "docs", "i18n", "untranslated-baseline.txt");
        Assert.True(File.Exists(baselinePath), $"Missing baseline: {baselinePath}");
        var baseline = ParseBaseline(File.ReadAllLines(baselinePath));

        foreach (var lang in NonDefaultLanguages)
        {
            var map = catalog[lang];
            var identical = 0;
            var exempt = 0;
            foreach (var (key, nlValue) in nl)
            {
                if (UiStringsScholen.IsNlOnlyPrefix(key))
                {
                    // D12: Dutch-only Scholen modules — not counted toward identical-to-nl debt.
                    continue;
                }

                if (!map.TryGetValue(key, out var value) || value != nlValue)
                {
                    continue;
                }

                if (UiStringsSales.IsNlOnlyPrefix(key)
                    || LocalizationParityAllowList.IsExemptIdenticalValue(nlValue))
                {
                    exempt++;
                }
                else
                {
                    identical++;
                }
            }

            _output.WriteLine(
                $"{lang}: identical-to-nl (non-exempt)={identical}, exempt={exempt}, baseline={baseline.GetValueOrDefault(lang, -1)}");
            Console.WriteLine(
                $"[LocalizationParity] {lang}: identical-to-nl={identical} (exempt {exempt}); baseline={baseline.GetValueOrDefault(lang, -1)}");

            Assert.True(
                baseline.ContainsKey(lang),
                $"Baseline missing language '{lang}'. Update docs/i18n/untranslated-baseline.txt.");
            Assert.True(
                identical <= baseline[lang],
                $"Untranslated identical-to-nl count for '{lang}' rose from {baseline[lang]} to {identical}. " +
                "Translate new keys or raise the baseline only after review.");
        }
    }

    [Fact]
    public void UiStrings_values_do_not_contain_legacy_brand_Jobsy()
    {
        var catalog = LoadCatalog();
        var hits = new List<string>();
        foreach (var lang in Languages)
        {
            foreach (var (key, value) in catalog[lang])
            {
                if (value.Contains("Jobsy", StringComparison.Ordinal))
                {
                    hits.Add($"{lang}:{key}={value}");
                }
            }
        }

        Assert.True(
            hits.Count == 0,
            "User-visible UiStrings values still contain 'Jobsy' (use 'Lobsy'). " +
            string.Join("; ", hits.Take(20)));
    }

    private static Dictionary<string, Dictionary<string, string>> LoadCatalog()
    {
        var catalogField = typeof(UiStrings).GetField(
            "Catalog",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(catalogField);
        return (Dictionary<string, Dictionary<string, string>>)catalogField!.GetValue(null)!;
    }

    private static Dictionary<string, int> ParseBaseline(IEnumerable<string> lines)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var lang = line[..eq].Trim();
            if (int.TryParse(line[(eq + 1)..].Trim(), out var count))
            {
                map[lang] = count;
            }
        }

        return map;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find repo root (Jobsy.sln).");
    }
}

/// <summary>
/// Values that are legitimately identical across languages (brand, OK, numbers, short universal tokens).
/// Shared with <c>tools/i18n-export</c> baseline logic.
/// </summary>
internal static class LocalizationParityAllowList
{
    private static readonly HashSet<string> Exact = new(StringComparer.Ordinal)
    {
        "Lobsy", "OK", "Match", "match", "Admin", "Sales", "Coach", "Bug", "Tip", "Status",
        "Email", "E-mail", "Model", "Tests", "Trends", "Open", "Later", "Nu", "Doel", "Basis", "min",
        "KVK", "SBI", "Arts", "Kok", "Meer", "Eens", "Samen", "Adres", "E-bike", "CV", "PDF",
        "WhatsApp", "IBAN", "BTW", "ID", "URL", "API", "OTP", "SMS", "GPS", "AI", "2FA",
        "Filters", "Urgent", "Dashboard", "Team", "Privacy"
    };

    public static bool IsExemptIdenticalValue(string value)
    {
        var v = value.Trim();
        if (v.Length == 0)
        {
            return true;
        }

        if (string.Equals(v, "Lobsy", StringComparison.OrdinalIgnoreCase)
            || string.Equals(v, "OK", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Regex.IsMatch(v, @"^\d+([.,]\d+)?$"))
        {
            return true;
        }

        if (Regex.IsMatch(v, @"^[\d½]+\s*min$", RegexOptions.IgnoreCase))
        {
            return true;
        }

        // Universal distance tokens (nl/en/pl/ro share "km").
        if (Regex.IsMatch(v, @"^\{\d+\}\s*km$", RegexOptions.IgnoreCase))
        {
            return true;
        }

        // Pure format placeholders (e.g. "{0}") are language-neutral.
        if (Regex.IsMatch(v, @"^\{\d+\}$"))
        {
            return true;
        }

        // Short day abbreviations shared by nl/en/pl (e.g. "7 d", "{0:0.#} d").
        if (Regex.IsMatch(v, @"^(\d+|\{0:0\.#\})\s*d$", RegexOptions.IgnoreCase))
        {
            return true;
        }

        // Badge overflow "+n" / "+{0}" is language-neutral.
        if (Regex.IsMatch(v, @"^\+\{\d+\}$"))
        {
            return true;
        }

        if (Regex.IsMatch(v, @"^[A-Z] - [A-Z]$"))
        {
            return true;
        }

        return Exact.Contains(v);
    }
}
