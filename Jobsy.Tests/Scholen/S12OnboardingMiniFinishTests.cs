using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Components.Candidate.Onboarding;

namespace Jobsy.Tests.Scholen;

public sealed class S12OnboardingMiniFinishTests
{
    [Theory]
    [InlineData(7)]
    [InlineData(10)]
    public void Last_mini_answer_must_advance_wizard_step(int step)
    {
        var version = AssessmentTestVersions.V3;
        var miniIds = OnboardingMiniBlockCompletion.MiniQuestionIds(step, version);
        Assert.True(miniIds.Length >= 2, "Mini block should have at least two questions for this guard.");

        var answers = new Dictionary<int, int>();
        for (var i = 0; i < miniIds.Length - 1; i++)
        {
            answers[miniIds[i]] = 3;
        }

        Assert.False(OnboardingMiniBlockCompletion.IsMiniBlockComplete(step, version, answers));
        answers[miniIds[^1]] = 4;
        Assert.True(OnboardingMiniBlockCompletion.IsMiniBlockComplete(step, version, answers));
    }
}
