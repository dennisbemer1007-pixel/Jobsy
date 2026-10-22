using Jobsy.Web.Onboarding;

namespace Jobsy.Tests;

public sealed class OnboardingSsrMiniNavigationTests
{
    [Fact]
    public void Without_q_opens_first_unanswered_question()
    {
        var questions = new List<OnboardingClientStepSeed.MiniQuestionSeed>
        {
            new(1, "A"),
            new(2, "B"),
            new(3, "C"),
        };
        var answers = new Dictionary<int, int> { [1] = 4 };

        var index = OnboardingSsrMiniNavigation.ResolveActiveQuestionIndex(questions, answers, qQuery: null);
        Assert.Equal(1, index);
    }

    [Fact]
    public void With_q_uses_requested_question()
    {
        var questions = new List<OnboardingClientStepSeed.MiniQuestionSeed>
        {
            new(1, "A"),
            new(2, "B"),
        };

        var index = OnboardingSsrMiniNavigation.ResolveActiveQuestionIndex(questions, new Dictionary<int, int>(), qQuery: 2);
        Assert.Equal(1, index);
    }

    [Fact]
    public void StepQuestionUrl_includes_q_parameter()
    {
        Assert.Equal("/candidate/start?step=7&q=3", OnboardingSsrMiniNavigation.StepQuestionUrl(7, 3));
    }
}
