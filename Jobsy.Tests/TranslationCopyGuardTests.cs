using System.Reflection;
using Jobsy.Core.Localization;
using Jobsy.Core.Options;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

/// <summary>
/// Candidate-facing pl/ro/ar must not stay an English copy when Dutch differs.
/// Brand names, numbers and tokens such as "OK" are allowlisted with a reason.
/// At launch only <see cref="JobsyLanguages.Enabled"/> cultures are checked; set
/// <c>JOBSY_I18N_COPYGUARD_ALL=1</c> or enable pl/ro/ar via config to run the full guard.
/// </summary>
public sealed class TranslationCopyGuardTests
{
    private static readonly string[] SecondaryCatalog = ["pl", "ro", "ar"];

    /// <summary>
    /// The everyday word is spelled the same in that language, so it is not an English copy.
    /// </summary>
    private static readonly Dictionary<string, string> LoanwordSameSpelling = new(StringComparer.Ordinal)
    {
        ["ro:Apply.Transport"] = "Romanian spells this Transport.",
        ["ro:Discovery.Transport"] = "Romanian spells this Transport.",
        ["ro:Onboarding.Transport"] = "Romanian spells this Transport.",
        ["ro:Vacancy.Transport"] = "Romanian spells this Transport.",
        ["ro:Dna.CulturePole.Autonomy.High"] = "Romanian spells this Independent.",
        ["ro:Dna.CulturePole.Informal.High"] = "Romanian spells this Informal.",
        ["ro:Dna.CulturePole.Informal.Low"] = "Romanian spells this Formal.",
        ["ro:Dna.HighlightImportant"] = "Romanian spells this Important.",
        ["ro:Fit.Important"] = "Romanian spells this Important.",
        ["ro:WaProfile.Slider.Informal.High"] = "Romanian spells this Informal.",
        ["ro:WaProfile.Slider.Informal.Low"] = "Romanian spells this Formal."
    };

    /// <summary>Employer and admin surfaces. Listed, not translated in this wave.</summary>
    private static readonly string[] EmployerAdminPrefixes =
    [
        "Admin",
        "Employer.",
        "Insights.",
        "Sales.",
        "School.",
        "Leraar.",
        "Werkgever",
        "Enterprise.",
        "Talent.",
        "Wg",
        "EntUi.",
        "Mail."
    ];

    [Fact]
    public void Candidate_facing_pl_ro_ar_are_not_english_copies()
    {
        JobsyLanguages.ResetEnabledForTests();
        var checkedLangs = CopyGuardLanguages().ToArray();
        if (checkedLangs.Length == 0)
        {
            return;
        }

        var gaps = RunCopyGuard(checkedLangs, out var employer);
        Assert.True(
            gaps.Count == 0,
            $"Candidate-facing {string.Join("/", checkedLangs)} still copy English ({gaps.Count}). First: {string.Join(" | ", gaps.Take(12))}");
        _ = employer;
    }

    /// <summary>Opt-in via <c>JOBSY_I18N_COPYGUARD_ALL=1</c> when pl/ro/ar are re-enabled in config.</summary>
    [Fact]
    public void Candidate_facing_pl_ro_ar_are_not_english_copies_when_all_catalog_enabled()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_I18N_COPYGUARD_ALL"), "1", StringComparison.Ordinal))
        {
            return;
        }

        JobsyLanguages.ConfigureEnabledCultures("nl,en,pl,ro,ar");
        try
        {
            var gaps = RunCopyGuard(SecondaryCatalog, out _);
            Assert.True(
                gaps.Count == 0,
                $"Full-catalog copy guard: pl/ro/ar still copy English ({gaps.Count}). First: {string.Join(" | ", gaps.Take(12))}");
        }
        finally
        {
            JobsyLanguages.ResetEnabledForTests();
        }
    }

    private static IEnumerable<string> CopyGuardLanguages()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("JOBSY_I18N_COPYGUARD_ALL"), "1", StringComparison.Ordinal))
        {
            foreach (var lang in SecondaryCatalog)
            {
                yield return lang;
            }

            yield break;
        }

        foreach (var lang in SecondaryCatalog)
        {
            if (JobsyLanguages.IsEnabled(lang))
            {
                yield return lang;
            }
        }
    }

    private static List<string> RunCopyGuard(IReadOnlyList<string> checkedLangs, out List<string> employer)
    {
        var catalog = LoadCatalog();
        var nl = catalog["nl"];
        var en = catalog["en"];
        var gaps = new List<string>();
        employer = new List<string>();

        foreach (var (key, nlValue) in nl.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!en.TryGetValue(key, out var enValue) || string.Equals(nlValue, enValue, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var lang in checkedLangs)
            {
                if (!catalog[lang].TryGetValue(key, out var value))
                {
                    continue;
                }

                if (!string.Equals(value, enValue, StringComparison.Ordinal))
                {
                    continue;
                }

                if (LocalizationParityAllowList.IsExemptIdenticalValue(enValue))
                {
                    continue;
                }

                if (LoanwordSameSpelling.ContainsKey($"{lang}:{key}"))
                {
                    continue;
                }

                var line = $"{lang}\t{key}\t{nlValue}\t{enValue}";
                if (IsEmployerOrAdmin(key))
                {
                    employer.Add(line);
                }
                else
                {
                    gaps.Add(line);
                }
            }
        }

        var root = FindRepoRoot();
        Directory.CreateDirectory(Path.Combine(root, "artifacts"));
        File.WriteAllLines(Path.Combine(root, "artifacts", "i18n-gaps-candidate.tsv"), gaps);
        File.WriteAllLines(Path.Combine(root, "artifacts", "i18n-gaps-employer.tsv"), employer);
        return gaps;
    }

    private static bool IsEmployerOrAdmin(string key)
    {
        foreach (var prefix in EmployerAdminPrefixes)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, Dictionary<string, string>> LoadCatalog()
    {
        var catalogField = typeof(UiStrings).GetField("Catalog", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(catalogField);
        return (Dictionary<string, Dictionary<string, string>>)catalogField!.GetValue(null)!;
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

        throw new InvalidOperationException("Repo root not found.");
    }
}
