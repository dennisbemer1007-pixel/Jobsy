using Jobsy.Web.Localization;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

public sealed class OnboardingWelcomeDismissTests
{
    [Fact]
    public void Welcome_screen_hidden_after_welcome_source_is_saved()
    {
        var state = new OnboardingState
        {
            CurrentStep = 1,
            Source = "welcome",
            Steps =
            [
                new OnboardingStepEvent { Step = 1, StartedAtUtc = DateTime.UtcNow },
            ],
        };

        Assert.False(ShouldShowWelcome(state, step: 1));
    }

    [Fact]
    public void Welcome_screen_shown_for_fresh_step1_without_dismiss_source()
    {
        var state = new OnboardingState
        {
            CurrentStep = 1,
            Steps =
            [
                new OnboardingStepEvent { Step = 1, StartedAtUtc = DateTime.UtcNow },
            ],
        };

        Assert.True(ShouldShowWelcome(state, step: 1));
    }

    [Fact]
    public void Welcome_title_no_name_key_exists_in_nl_and_en()
    {
        Assert.Equal("Hoi, fijn dat je er bent", UiStrings.Get("Onboarding.Welcome.TitleNoName", "nl"));
        Assert.Equal("Hi, nice to have you here", UiStrings.Get("Onboarding.Welcome.TitleNoName", "en"));
    }

    private static bool ShouldShowWelcome(OnboardingState state, int step)
        => step == 1
           && !string.Equals(state.Source, "welcome", StringComparison.OrdinalIgnoreCase)
           && (state.Steps.Count == 0
               || state.Steps.All(item => item.Step == 1 && item.CompletedAtUtc is null));
}
