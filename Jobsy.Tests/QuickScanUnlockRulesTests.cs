using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public sealed class QuickScanUnlockRulesTests
{
    [Fact]
    public void Career_mini_complete_with_all_mini_answers_unlocks()
    {
        var version = AssessmentTestVersions.V3;
        var miniIds = TestDepthRules.MiniIds(AssessmentKind.Career, version);
        var answers = miniIds.ToDictionary(id => id, _ => 4);
        var json = QuickScanAssessmentBridge.SerializeAnswers(AssessmentKind.Career, answers, version);
        Assert.True(QuickScanUnlockRules.IsCareerMiniComplete("Draft", json, version));
    }

    [Fact]
    public void Competence_completed_status_unlocks_without_answers()
    {
        Assert.True(QuickScanUnlockRules.IsCompetenceMiniComplete(
            CandidateCompetencyStatuses.Completed,
            answersJson: null,
            quickScanVersion: null));
    }
}
