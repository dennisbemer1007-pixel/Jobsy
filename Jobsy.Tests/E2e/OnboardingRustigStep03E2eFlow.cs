using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class OnboardingRustigStep03E2eFlow
{
    internal static Task ReachRustigStep03Async(IPage page, string baseUrl, bool mobile) =>
        ReachSeededStep03Async(page, baseUrl, mobile, rustig: true);

    internal static Task ReachBaselineStep03Async(IPage page, string baseUrl, bool mobile) =>
        ReachSeededStep03Async(page, baseUrl, mobile, rustig: false);

    private static async Task ReachSeededStep03Async(IPage page, string baseUrl, bool mobile, bool rustig)
    {
        _ = mobile;
        var email = await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateEmailAsync(
            page, baseUrl, rustig ? "rustig-u3c-seed" : "baseline-u3c-seed");

        await CandidateJourneyE2e.PrepareDiscoveryStepForE2eAsync(email, step: 3);

        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await OnboardingRustigStep04E2eFlow.WaitForOnboardingWizardReadyAsync(page);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);

        if (rustig)
        {
            await FunctionalOnboardingInteractivityHelpers.AssertOnboardingRustigStepScreenAsync(page, 3);
        }
        else
        {
            await page.Locator(".ob-wizard:not(.ob-wizard--rustig) [data-testid=onboarding-step-3-illustrated]").WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 90_000,
            });
        }
    }
}
