using System.Reflection;
using System.Text.RegularExpressions;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

/// <summary>
/// Dutch candidate-facing copy stays B1. Jargon on the passport and test screens must be rewritten.
/// Hits elsewhere stay, with a reason, and are listed for the PR.
/// </summary>
public sealed class B1WordListTests
{
    private static readonly (string Label, Regex Pattern)[] Banned =
    [
        ("initiëren", new Regex(@"initi\w*ren", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),
        ("competentie(s)", new Regex(@"competentie\w*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),
        ("assessment", new Regex(@"\bassessment\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),
        ("RIASEC", new Regex(@"\bRIASEC\b", RegexOptions.CultureInvariant | RegexOptions.Compiled)),
        ("matchscore", new Regex(@"matchscore", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),
        ("normgroep", new Regex(@"normgroep", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)),
        ("valideren", new Regex(@"valideren", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled))
    ];

    /// <summary>Not the passport or a test screen. Left unchanged on purpose.</summary>
    private static readonly Dictionary<string, string> Elsewhere = new(StringComparer.OrdinalIgnoreCase)
    {
        ["HowLobsy.Step2Body"] = "Public how-it-works page, not a passport or test screen.",
        ["Profile.Lead"] = "Profile lead outside the passport and test screens.",
        ["Kb.Why.competency"] = "Knowledge-base article, not a passport or test screen.",
        ["Kb.Dna.Competencies"] = "Knowledge-base article, not a passport or test screen.",
        ["WhoAmI.StepCompetency"] = "Unused who-am-I step label; not rendered on the passport or test screens.",
        ["WhoAmI.OpenCompetency"] = "Unused who-am-I step label; not rendered on the passport or test screens."
    };

    [Fact]
    public void Dutch_candidate_copy_avoids_banned_jargon_on_passport_and_tests()
    {
        var nl = LoadCatalog()["nl"];
        var unexplained = new List<string>();
        foreach (var (key, value) in nl.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (IsEmployerOrAdmin(key) || UiStringsScholen.IsNlOnlyPrefix(key) || UiStringsSales.IsNlOnlyPrefix(key))
            {
                continue;
            }

            var hit = Banned.FirstOrDefault(b => b.Pattern.IsMatch(value));
            if (hit.Pattern is null)
            {
                continue;
            }

            if (Elsewhere.ContainsKey(key))
            {
                continue;
            }

            unexplained.Add($"{key} [{hit.Label}] {value}");
        }

        Assert.True(
            unexplained.Count == 0,
            "Rewrite these Dutch strings in B1, or allowlist them with a reason when they are not on the passport or test screens: "
            + string.Join(" | ", unexplained.Take(30)));
    }

    private static bool IsEmployerOrAdmin(string key)
        => key.StartsWith("Admin", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("Employer.", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("Insights.", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("Sales.", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("School.", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("Leraar.", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("Werkgever", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("Enterprise.", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("Talent.", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("Wg", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("EntUi.", StringComparison.OrdinalIgnoreCase)
           || key.StartsWith("Mail.", StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, Dictionary<string, string>> LoadCatalog()
    {
        var catalogField = typeof(UiStrings).GetField("Catalog", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(catalogField);
        return (Dictionary<string, Dictionary<string, string>>)catalogField!.GetValue(null)!;
    }
}
