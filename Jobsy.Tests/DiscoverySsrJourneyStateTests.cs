using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Discovery;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

public sealed class DiscoverySsrJourneyStateTests
{
    [Fact]
    public void IsJourneyComplete_honours_finish_reached_even_when_current_step_is_low()
    {
        var state = new OnboardingState
        {
            CurrentStep = 2,
            FinishReached = true,
            IsComplete = false,
        };
        var draft = new OnboardingProfileDraft { HasConsent = true };

        Assert.True(DiscoverySsrJourneyState.IsJourneyComplete(state, draft));
    }

    [Fact]
    public void IsJourneyComplete_honours_is_complete_and_completed_at()
    {
        var draft = new OnboardingProfileDraft { HasConsent = true };
        Assert.True(DiscoverySsrJourneyState.IsJourneyComplete(
            new OnboardingState { IsComplete = true, CurrentStep = 2 },
            draft));
        Assert.True(DiscoverySsrJourneyState.IsJourneyComplete(
            new OnboardingState { CompletedAtUtc = DateTime.UtcNow, CurrentStep = 1 },
            draft));
    }
}
