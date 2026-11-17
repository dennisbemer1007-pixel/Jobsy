using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Whether onboarding mini blocks have enough draft answers to unlock passport/fit features.</summary>
public static class QuickScanUnlockRules
{
    public static bool IsCompetenceMiniComplete(
        string? status,
        string? answersJson,
        int? quickScanVersion)
    {
        if (CandidateCompetencyStatuses.IsCompleted(status))
        {
            return true;
        }

        var version = AssessmentTestVersions.Normalize(quickScanVersion ?? AssessmentTestVersions.V1);
        var answers = QuickScanAssessmentBridge.ParseAnswers(AssessmentKind.Competence, answersJson, version);
        var miniIds = TestDepthRules.MiniIds(AssessmentKind.Competence, version);
        return miniIds.All(id => answers.TryGetValue(id, out var v) && v is >= 1 and <= 5);
    }

    public static bool IsCareerMiniComplete(
        string? status,
        string? answersJson,
        int? quickScanVersion)
    {
        if (CandidateCompetencyStatuses.IsCompleted(status))
        {
            return true;
        }

        var version = AssessmentTestVersions.Normalize(quickScanVersion ?? AssessmentTestVersions.V1);
        var answers = QuickScanAssessmentBridge.ParseAnswers(AssessmentKind.Career, answersJson, version);
        var miniIds = TestDepthRules.MiniIds(AssessmentKind.Career, version);
        return miniIds.All(id => answers.TryGetValue(id, out var v) && v is >= 1 and <= 5);
    }
}