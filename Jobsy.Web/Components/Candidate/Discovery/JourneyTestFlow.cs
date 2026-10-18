using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Components.Candidate.Discovery;

/// <summary>Pure helpers for ontdekkingsreis test depth, shed options, and end-screen facts.</summary>
public static class JourneyTestFlow
{
    public sealed record DepthOptionSpec(int Level, string TitleKey, string SubKey, int Dots, bool Disabled);

    public static int FullLevelCount(
        OnboardingWizardCatalog.OnboardingTestKind kind,
        int quickScanVersion = AssessmentTestVersions.V1)
        => TestDepthRules.FullCount(TestDepthRules.ToAssessmentKind(kind), quickScanVersion);

    public static int ReachedLevel(
        int answeredCount,
        OnboardingWizardCatalog.OnboardingTestKind kind,
        int quickScanVersion = AssessmentTestVersions.V1)
    {
        var reached = TestDepthRules.Reached(
            TestDepthRules.ToAssessmentKind(kind), answeredCount, quickScanVersion);
        return reached switch
        {
            TestDepthLevel.Full => FullLevelCount(kind, quickScanVersion),
            TestDepthLevel.Deeper => TestDepthRules.DeeperCount,
            TestDepthLevel.First => OnboardingWizardCatalog.MiniLevelCount,
            _ => 0
        };
    }

    public static IReadOnlyList<DepthOptionSpec> BuildDepthOptions(
        OnboardingWizardCatalog.OnboardingTestKind kind,
        int answeredCount,
        int quickScanVersion = AssessmentTestVersions.V1)
    {
        var full = FullLevelCount(kind, quickScanVersion);
        var miniDone = OnboardingWizardCatalog.IsLevelReached(answeredCount, OnboardingWizardCatalog.MiniLevelCount);
        var deeperDone = OnboardingWizardCatalog.IsLevelReached(answeredCount, OnboardingWizardCatalog.DeeperLevelCount);
        var fullDone = OnboardingWizardCatalog.IsLevelReached(answeredCount, full);

        var culture = kind == OnboardingWizardCatalog.OnboardingTestKind.Culture;
        return
        [
            new(OnboardingWizardCatalog.MiniLevelCount,
                "Discovery.Shed.Keep",
                miniDone ? "Discovery.Shed.DoneSub" : "Discovery.Shed.KeepSub",
                1,
                Disabled: miniDone),
            new(OnboardingWizardCatalog.DeeperLevelCount,
                "Discovery.Shed.Deeper",
                deeperDone ? "Discovery.Shed.DoneSub" : "Discovery.Shed.DeeperSub10",
                2,
                Disabled: deeperDone),
            new(full,
                "Discovery.Shed.Deepest",
                fullDone
                    ? "Discovery.Shed.DoneSub"
                    : culture ? "Discovery.Shed.DeepestSubCulture" : "Discovery.Shed.DeepestSub",
                5,
                Disabled: fullDone)
        ];
    }

    /// <summary>
    /// Default selection on the shed: the next band that is not done yet.
    /// A finished band stays visible as Gedaan and is not preselected.
    /// </summary>
    public static int DefaultSelectedLevel(
        int answeredCount,
        OnboardingWizardCatalog.OnboardingTestKind kind,
        int quickScanVersion = AssessmentTestVersions.V1)
    {
        var full = FullLevelCount(kind, quickScanVersion);
        if (answeredCount >= full) return full;
        if (answeredCount >= OnboardingWizardCatalog.DeeperLevelCount) return full;
        if (answeredCount >= OnboardingWizardCatalog.MiniLevelCount) return OnboardingWizardCatalog.DeeperLevelCount;
        return OnboardingWizardCatalog.MiniLevelCount;
    }

    public static bool IsFullyDone(
        int answeredCount,
        OnboardingWizardCatalog.OnboardingTestKind kind,
        int quickScanVersion = AssessmentTestVersions.V1)
        => OnboardingWizardCatalog.IsLevelReached(answeredCount, FullLevelCount(kind, quickScanVersion));

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

    /// <summary>
    /// Depth label for an end-screen card: Eerste indruk, Iets dieper, or Helemaal gedaan.
    /// </summary>
    public static string DepthLabelKey(
        int answeredCount,
        OnboardingWizardCatalog.OnboardingTestKind kind,
        int quickScanVersion = AssessmentTestVersions.V1)
    {
        if (answeredCount >= FullLevelCount(kind, quickScanVersion))
        {
            return "Discovery.Overview.FullyDone";
        }

        if (answeredCount >= OnboardingWizardCatalog.DeeperLevelCount)
        {
            return "TestDepth.Deeper";
        }

        if (answeredCount >= OnboardingWizardCatalog.MiniLevelCount)
        {
            return "TestDepth.First";
        }

        return "Discovery.End.NotDiscovered";
    }

    /// <summary>
    /// Footnote follows the shallowest of the three end-screen tests.
    /// </summary>
    public static string EndHintKey(int competencyAnswered, int careerAnswered, int valuesAnswered)
    {
        var keys = new[]
        {
            DepthLabelKey(competencyAnswered, OnboardingWizardCatalog.OnboardingTestKind.Competency),
            DepthLabelKey(careerAnswered, OnboardingWizardCatalog.OnboardingTestKind.Career),
            DepthLabelKey(valuesAnswered, OnboardingWizardCatalog.OnboardingTestKind.Values)
        };
        if (keys.All(k => k == "Discovery.Overview.FullyDone"))
        {
            return "Discovery.End.HintFull";
        }

        if (keys.All(k => k is "Discovery.Overview.FullyDone" or "TestDepth.Deeper"))
        {
            return "Discovery.End.HintDeeper";
        }

        return "Discovery.End.HintFirst";
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

    /// <summary>URL token for the shed after step <paramref name="step"/> (7–10).</summary>
    public static string ShedQuery(int step) => $"shed-{Math.Clamp(step, 7, 10)}";

    /// <summary>Query value for <c>?diepte=</c>. Only the free-test bands are accepted.</summary>
    public static bool TryParseDepth(string? raw, out int level)
    {
        level = 0;
        if (!int.TryParse(raw, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        if (parsed is not (OnboardingWizardCatalog.MiniLevelCount
            or OnboardingWizardCatalog.DeeperLevelCount
            or 18
            or 25))
        {
            return false;
        }

        level = parsed;
        return true;
    }

    public static string DepthQueryValue(int level)
        => level.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Restores the shed step from <c>?stap=</c>. Bare <c>shed</c> stays on step 7;
    /// <c>shed-8</c> restores step 8 so a refresh does not rewind to the first test.
    /// </summary>
    public static bool TryParseShedStep(string? stap, out int step)
    {
        step = 0;
        if (string.IsNullOrWhiteSpace(stap))
        {
            return false;
        }

        var value = stap.Trim();
        if (value.Equals("shed", StringComparison.OrdinalIgnoreCase))
        {
            step = 7;
            return true;
        }

        const string prefix = "shed-";
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(value[prefix.Length..], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            && parsed is >= 7 and <= 10)
        {
            step = parsed;
            return true;
        }

        return false;
    }
}
