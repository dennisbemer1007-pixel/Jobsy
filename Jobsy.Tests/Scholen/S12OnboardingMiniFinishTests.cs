using Jobsy.Web.Components.Candidate.Onboarding;

namespace Jobsy.Tests.Scholen;

public sealed class S12OnboardingMiniFinishTests
{
    [Theory]
    [InlineData(7, 1)]
    [InlineData(10, 5)]
    public void Last_mini_answer_must_advance_wizard_step(int step, int questionIndex)
    {
        var miniIds = OnboardingMiniBlockCompletion.MiniQuestionIds(step, quickScanVersion: 1);
        Assert.True(miniIds.Length >= questionIndex);
        var answers = new Dictionary<int, int>();
        for (var i = 0; i < questionIndex; i++)
        {
            answers[miniIds[i]] = 3;
        }

        Assert.False(OnboardingMiniBlockCompletion.IsMiniBlockComplete(step, 1, answers));
        answers[miniIds[questionIndex - 1]] = 4;
        Assert.True(OnboardingMiniBlockCompletion.IsMiniBlockComplete(step, 1, answers));
    }
}
