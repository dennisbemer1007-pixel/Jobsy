using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Components.Candidate.Discovery;

/// <summary>Pure helpers for ontdekkingsreis test depth, shed options, and end-screen facts.</summary>
public static class JourneyTestFlow
{
    public sealed record DepthOptionSpec(int Level, string TitleKey, string SubKey, int Dots, bool Disabled);

    public static IReadOnlyList<DepthOptionSpec> BuildDepthOptions(
        OnboardingWizardCatalog.OnboardingTestKind kind,
        int answeredCount)
    {
        var full = OnboardingWizardCatalog.FullLevelCount(kind);
        var miniDone = OnboardingWizardCatalog.IsLevelReached(answeredCount, OnboardingWizardCatalog.MiniLevelCount);
        var deeperDone = OnboardingWizardCatalog.IsLevelReached(answeredCount, OnboardingWizardCatalog.DeeperLevelCount);
        var fullDone = OnboardingWizardCatalog.IsLevelReached(answeredCount, full);

        var culture = kind == OnboardingWizardCatalog.OnboardingTestKind.Culture;
        return
        [
            new(OnboardingWizardCatalog.MiniLevelCount,
                "Discovery.Shed.Keep",
                "Discovery.Shed.KeepSub",
                1,
                Disabled: miniDone),
            new(OnboardingWizardCatalog.DeeperLevelCount,
                "Discovery.Shed.Deeper",
                "Discovery.Shed.DeeperSub10",
                2,
                Disabled: deeperDone),
            new(full,
                "Discovery.Shed.Deepest",
                culture ? "Discovery.Shed.DeepestSubCulture" : "Discovery.Shed.DeepestSub",
                5,
                Disabled: fullDone)
        ];
    }

    /// <summary>
    /// Default selection on the shed moment: keep current band unless a deeper band is the only remaining option.
    /// </summary>
    public static int DefaultSelectedLevel(int answeredCount, OnboardingWizardCatalog.OnboardingTestKind kind)
    {
        var reached = OnboardingWizardCatalog.ReachedLevel(answeredCount, kind);
        if (reached <= 0) return OnboardingWizardCatalog.MiniLevelCount;
        if (reached < OnboardingWizardCatalog.DeeperLevelCount) return OnboardingWizardCatalog.MiniLevelCount;
        if (reached < OnboardingWizardCatalog.FullLevelCount(kind)) return OnboardingWizardCatalog.DeeperLevelCount;
        return OnboardingWizardCatalog.FullLevelCount(kind);
    }

    public static bool IsFullyDone(int answeredCount, OnboardingWizardCatalog.OnboardingTestKind kind)
        => OnboardingWizardCatalog.IsLevelReached(answeredCount, OnboardingWizardCatalog.FullLevelCount(kind));

    public static IReadOnlyList<string> ImpressionLinesForTest(
        OnboardingWizardCatalog.OnboardingTestKind kind,
        OnboardingImpression? impression,
        Func<string, string> localize,
        string provisionalSuffix)
    {
        _ = localize;
        if (impression is null) return [];
        var lines = new List<string>();
        switch (kind)
        {
            case OnboardingWizardCatalog.OnboardingTestKind.Competency:
                foreach (var s in impression.Strengths.Take(3))
                {
                    var text = string.IsNullOrWhiteSpace(s.Sentence) ? s.Label : s.Sentence;
                    if (impression.CompetencyProvisional) text += provisionalSuffix;
                    lines.Add(text);
                }
                break;
            case OnboardingWizardCatalog.OnboardingTestKind.Career:
                foreach (var r in impression.Riasec.Take(3))
                {
                    var text = string.IsNullOrWhiteSpace(r.Sentence) ? r.Label : r.Sentence;
                    if (impression.CareerProvisional) text += provisionalSuffix;
                    lines.Add(text);
                }
                break;
            case OnboardingWizardCatalog.OnboardingTestKind.Culture:
                if (impression.CultureHighlight is { } ch)
                {
                    var text = string.IsNullOrWhiteSpace(ch.Sentence) ? ch.Label : ch.Sentence;
                    if (impression.CultureProvisional) text += provisionalSuffix;
                    lines.Add(text);
                }
                break;
            case OnboardingWizardCatalog.OnboardingTestKind.Values:
                if (impression.TopValue is { } tv)
                {
                    var text = string.IsNullOrWhiteSpace(tv.Sentence) ? tv.Label : tv.Sentence;
                    if (impression.ValuesProvisional) text += provisionalSuffix;
                    lines.Add(text);
                }
                break;
        }

        return lines;
    }

    public static (string? Strength, string? Work, string? Value) EndFacts(OnboardingImpression? impression)
    {
        if (impression is null) return (null, null, null);
        var strength = impression.Strengths.FirstOrDefault()?.Label;
        var work = impression.Riasec.Count == 0
            ? null
            : string.Join(" & ", impression.Riasec.Take(2).Select(r => r.Label));
        var value = impression.TopValue?.Label;
        return (strength, work, value);
    }

    public static string TestTitleKey(int step) => step switch
    {
        7 => "Discovery.Test.Competency.Title",
        8 => "Discovery.Test.Career.Title",
        9 => "Discovery.Test.Culture.Title",
        10 => "Discovery.Test.Values.Title",
        _ => "Discovery.Test.Competency.Title"
    };

    public static string TestHintKey(int step)
        => step == 9 ? "Discovery.Test.HintCulture" : "Discovery.Test.Hint";
}
