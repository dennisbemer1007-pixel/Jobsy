using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Components.Candidate.Onboarding;

/// <summary>
/// Per-block mini (5/5) completion for onboarding steps 7–10. Answers are scoped per assessment store, not by id overlap across blocks.
/// </summary>
internal static class OnboardingMiniBlockCompletion
{
    /// <summary>Matches wizard SSR/BFF: stored version when set, else V3 when TestsV2 is on.</summary>
    public static int ResolveQuickScanVersion(int? storedVersion, bool testsV2Enabled)
    {
        if (storedVersion is int v && v > 0)
        {
            return AssessmentTestVersions.Normalize(v);
        }

        return testsV2Enabled ? AssessmentTestVersions.V3 : AssessmentTestVersions.V1;
    }

    public static AssessmentKind AssessmentKindForWizardStep(int step) => step switch
    {
        8 => AssessmentKind.Career,
        9 => AssessmentKind.Culture,
        10 => AssessmentKind.Values,
        _ => AssessmentKind.Competence
    };

    public static int[] MiniQuestionIds(int wizardStep, int quickScanVersion)
        => TestDepthRules.MiniIds(AssessmentKindForWizardStep(wizardStep), quickScanVersion);

    public static bool IsMiniBlockComplete(
        int wizardStep,
        int quickScanVersion,
        IReadOnlyDictionary<int, int> answers)
    {
        var miniIds = MiniQuestionIds(wizardStep, quickScanVersion);
        return miniIds.All(id =>
            answers.TryGetValue(id, out var value) && value is >= 1 and <= 5);
    }

    public static bool IsMiniBlockComplete(
        AssessmentKind kind,
        int quickScanVersion,
        Dictionary<string, int>? answers)
    {
        if (answers is not { Count: > 0 })
        {
            return false;
        }

        var miniIds = TestDepthRules.MiniIds(kind, quickScanVersion);
        return miniIds.All(id =>
            answers.TryGetValue(id.ToString(), out var value) && value is >= 1 and <= 5);
    }

    public static int FirstUnansweredMiniIndex(
        int wizardStep,
        int quickScanVersion,
        IReadOnlyDictionary<int, int> answers)
    {
        var miniIds = MiniQuestionIds(wizardStep, quickScanVersion);
        for (var i = 0; i < miniIds.Length; i++)
        {
            if (!answers.TryGetValue(miniIds[i], out var value) || value is < 1 or > 5)
            {
                return i;
            }
        }

        return Math.Max(0, miniIds.Length - 1);
    }

    public static Dictionary<int, int> ScopeToMiniIds(
        AssessmentKind kind,
        int quickScanVersion,
        IReadOnlyDictionary<int, int> source)
    {
        var allowed = TestDepthRules.MiniIds(kind, quickScanVersion).ToHashSet();
        var scoped = new Dictionary<int, int>();
        foreach (var (id, value) in source)
        {
            if (allowed.Contains(id) && value is >= 1 and <= 5)
            {
                scoped[id] = value;
            }
        }

        return scoped;
    }

    public static Dictionary<int, int> MergeVisible(
        int wizardStep,
        int quickScanVersion,
        IReadOnlyDictionary<int, int> serverAnswers,
        IReadOnlyDictionary<int, int> sessionAnswers)
    {
        var miniIds = MiniQuestionIds(wizardStep, quickScanVersion);
        var merged = new Dictionary<int, int>();
        foreach (var id in miniIds)
        {
            if (sessionAnswers.TryGetValue(id, out var session))
            {
                merged[id] = session;
            }
            else if (serverAnswers.TryGetValue(id, out var server))
            {
                merged[id] = server;
            }
        }

        return merged;
    }
}
