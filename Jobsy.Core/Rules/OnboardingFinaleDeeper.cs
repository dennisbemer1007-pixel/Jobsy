using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class OnboardingFinaleDeeper
{
    /// <summary>True when every Quick-Scan test reached at least the free deepening band.</summary>
    public static bool AllKindsReachedDeeper(
        IReadOnlyDictionary<string, int>? competencyAnswers,
        int? competencyVersion,
        IReadOnlyDictionary<string, int>? careerAnswers,
        int? careerVersion,
        IReadOnlyDictionary<string, int>? cultureAnswers,
        int? cultureVersion,
        IReadOnlyDictionary<string, int>? valuesAnswers,
        int? valuesVersion,
        bool testsV2Enabled)
    {
        return ReachedDeeper(AssessmentKind.Competence, competencyAnswers, competencyVersion, testsV2Enabled)
               && ReachedDeeper(AssessmentKind.Career, careerAnswers, careerVersion, testsV2Enabled)
               && ReachedDeeper(AssessmentKind.Culture, cultureAnswers, cultureVersion, testsV2Enabled)
               && ReachedDeeper(AssessmentKind.Values, valuesAnswers, valuesVersion, testsV2Enabled);
    }

    private static bool ReachedDeeper(
        AssessmentKind kind,
        IReadOnlyDictionary<string, int>? answers,
        int? storedVersion,
        bool testsV2Enabled)
    {
        var version = storedVersion is > 0
            ? AssessmentTestVersions.Normalize(storedVersion.Value)
            : testsV2Enabled ? AssessmentTestVersions.V3 : AssessmentTestVersions.V1;
        var map = ParseAnswers(answers);
        var count = map.Count;
        var reached = TestDepthRules.Reached(kind, count, version);
        return reached is TestDepthLevel.Deeper or TestDepthLevel.Full or TestDepthLevel.Bottom;
    }

    private static Dictionary<int, int> ParseAnswers(IReadOnlyDictionary<string, int>? answers)
    {
        var result = new Dictionary<int, int>();
        if (answers is null)
        {
            return result;
        }

        foreach (var pair in answers)
        {
            if (int.TryParse(pair.Key, out var id) && pair.Value is >= 1 and <= 5)
            {
                result[id] = pair.Value;
            }
        }

        return result;
    }
}
