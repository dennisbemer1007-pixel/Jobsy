using System.Text.RegularExpressions;
using Jobsy.Web.Localization;

namespace Jobsy.Tests.Werkgever;

/// <summary>D11 terminology guard for employer UI strings (file 08).</summary>
public class WerkgeverTerminologyTests
{
    /// <summary>Chosen English terms — keep stable.</summary>
    public static readonly IReadOnlyDictionary<string, string> ChosenEnglishTerms = new Dictionary<string, string>
    {
        ["WgShell.Role.BM"] = "Company manager",
        ["WgShell.Role.RM"] = "Regional manager",
        ["WgShell.Role.VM"] = "Branch manager",
        ["WgApp.Status.Hired"] = "Hired",
        ["WgApp.Status.Invited"] = "Invited",
        ["Employer.Highlight"] = "Feature",
        ["VacancyAction.Highlight"] = "Feature",
        ["Employer.PushBom"] = "Push message",
        ["VacancyAction.PushBom"] = "Push message",
    };

    private static readonly string[] ForbiddenNlPatterns =
    [
        @"\bPushBom\b",
        @"\bPush Bom\b",
        @"\bHighlight\b",
        @"\bGematcht\b",
        @"\bContact opgenomen\b",
        @"\bfiliaalmanager\b",
        @"\bbranchmanager\b",
        @"\bbranch manager\b",
        @"\bEnterprisemanager\b",
        @"\bEnterprise manager\b",
        @"\bRegional manager\b",
        @"\bExtend\b",
        @"\btokens uitgeven\b",
    ];

    [Fact]
    public void Nl_values_have_no_forbidden_jargon()
    {
        var nl = new Dictionary<string, string>(StringComparer.Ordinal);
        var en = new Dictionary<string, string>(StringComparer.Ordinal);
        var pl = new Dictionary<string, string>(StringComparer.Ordinal);
        var ro = new Dictionary<string, string>(StringComparer.Ordinal);
        var ar = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsWerkgever.MergeAll(nl, en, pl, ro, ar);
        UiStringsCandidateInsights.MergeAll(nl, en, pl, ro, ar);

        // Also resolve Culture keys used by werkgever markup from the full catalog.
        foreach (var key in CollectCultureKeysUsedByWerkgever())
        {
            if (!nl.ContainsKey(key))
            {
                var v = UiStrings.Get(key, "nl");
                if (!string.IsNullOrEmpty(v) && v != key)
                {
                    nl[key] = v;
                }
            }
        }

        var offenders = new List<string>();
        foreach (var (key, value) in nl)
        {
            foreach (var pattern in ForbiddenNlPatterns)
            {
                if (Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    offenders.Add($"{key} = {value}");
                    break;
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "Forbidden jargon in Dutch employer strings:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void En_values_use_chosen_role_and_action_terms()
    {
        var nl = new Dictionary<string, string>(StringComparer.Ordinal);
        var en = new Dictionary<string, string>(StringComparer.Ordinal);
        var pl = new Dictionary<string, string>(StringComparer.Ordinal);
        var ro = new Dictionary<string, string>(StringComparer.Ordinal);
        var ar = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsWerkgever.MergeAll(nl, en, pl, ro, ar);

        foreach (var (key, expected) in ChosenEnglishTerms)
        {
            Assert.True(en.ContainsKey(key), $"Missing EN key {key}");
            Assert.Equal(expected, en[key]);
        }

        foreach (var lang in new[] { pl, ro, ar })
        {
            foreach (var key in ChosenEnglishTerms.Keys)
            {
                Assert.True(lang.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) && v != key,
                    $"Parity missing for {key}");
            }
        }
    }

    private static IEnumerable<string> CollectCultureKeysUsedByWerkgever()
    {
        var root = FindRepoRoot();
        var dirs = new[]
        {
            Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Werkgever"),
            Path.Combine(root, "Jobsy.Web", "Components", "Werkgever"),
        };
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var rx = new Regex(@"Culture(?:Service)?\[[@\$]?""([^""]+)""\]", RegexOptions.Compiled);
        foreach (var dir in dirs)
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                         .Where(f => f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                                     || f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
            {
                foreach (Match m in rx.Matches(File.ReadAllText(file)))
                {
                    keys.Add(m.Groups[1].Value);
                }
            }
        }

        return keys;
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

        throw new InvalidOperationException("Repo root not found");
    }
}

/// <summary>
/// Heuristic: markup text nodes with ≥ 2 Dutch stop words must go through @Culture.
/// </summary>
public class WerkgeverHardcodedDutchTests
{
    private static readonly HashSet<string> DutchStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "het", "een", "van", "en", "voor", "met", "op", "te", "je", "jouw", "niet", "nog",
        "naar", "zijn", "deze", "dit", "die", "als", "ook", "bij", "uit", "aan", "tot", "kan",
        "alle", "geen", "meer", "worden", "wordt", "hun", "onze", "via", "of", "dan", "maar"
    };

    private static readonly string[] BrandAllowList =
    [
        "Lobsy", "Jobsy", "KVK", "WhatsApp", "Mollie", "Swagger", "API", "CSV", "PDF", "UTF", "Base64", "SROI"
    ];

    /// <summary>
    /// Pages that were only moved (not redesigned) — remaining Dutch literals are tracked as deferred.
    /// Redesigned surfaces must stay clean.
    /// </summary>
    private static readonly HashSet<string> DeferredFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreateVacancy.razor",
        "CompanyDetailsSection.razor",
        "CompanyDetailsSection.razor.cs",
        "CsvImportSection.razor",
        "SalaryTables.razor",
        "PartnerSales.razor",
        "PartnerSalesPayoutCheckoutStub.razor",
        "CultureScanSection.razor",
        "BranchesSection.razor",
        "RegionsSection.razor",
        "TalentPoolSection.razor",
        "TalentContactsSection.razor",
    };

    [Fact]
    public void Redesigned_werkgever_markup_has_no_hardcoded_dutch_sentences()
    {
        var root = FindRepoRoot();
        var dirs = new[]
        {
            Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Werkgever"),
            Path.Combine(root, "Jobsy.Web", "Components", "Werkgever"),
        };

        var offenders = new List<string>();
        foreach (var dir in dirs)
        {
            foreach (var file in Directory.EnumerateFiles(dir, "*.razor", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(file);
                if (DeferredFiles.Contains(name) || name.Contains("Legacy", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                // Drop @code blocks
                text = Regex.Replace(text, @"@code\s*\{.*\}\s*$", "", RegexOptions.Singleline);
                // Drop attribute values that are bindings
                foreach (Match m in Regex.Matches(text, @">\s*([^<@][^<]{8,}?)\s*<"))
                {
                    var raw = m.Groups[1].Value.Trim();
                    if (raw.Contains("@Culture", StringComparison.Ordinal)
                        || raw.Contains("@(", StringComparison.Ordinal)
                        || raw.StartsWith('@')
                        || BrandAllowList.Any(b => raw.Contains(b, StringComparison.OrdinalIgnoreCase) && CountStopWords(raw) < 2))
                    {
                        continue;
                    }

                    // Skip razor control fragments
                    if (raw.Contains("@if", StringComparison.Ordinal)
                        || raw.Contains("else if", StringComparison.Ordinal)
                        || raw.Contains("@foreach", StringComparison.Ordinal)
                        || raw.Contains("@switch", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (CountStopWords(raw) >= 2)
                    {
                        var rel = file[(root.Length + 1)..].Replace('\\', '/');
                        offenders.Add($"{rel}: {Collapse(raw)}");
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "Hardcoded Dutch outside @Culture (localize or add to DeferredFiles with note):\n"
            + string.Join("\n", offenders.Take(40))
            + (offenders.Count > 40 ? $"\n… +{offenders.Count - 40} more" : ""));
    }

    private static int CountStopWords(string text)
    {
        var words = Regex.Matches(text, @"[A-Za-zÀ-ÿ]+")
            .Select(m => m.Value)
            .Where(w => !BrandAllowList.Contains(w, StringComparer.OrdinalIgnoreCase));
        return words.Count(w => DutchStopWords.Contains(w));
    }

    private static string Collapse(string s)
        => Regex.Replace(s, @"\s+", " ").Trim();

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

        throw new InvalidOperationException("Repo root not found");
    }
}
