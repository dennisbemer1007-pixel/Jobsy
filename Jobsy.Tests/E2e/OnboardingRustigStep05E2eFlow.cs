using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class OnboardingRustigStep05E2eFlow
{
    internal static Task ReachRustigStep05Async(IPage page, string baseUrl, bool mobile) =>
        ReachSeededStep05Async(page, baseUrl, mobile, rustig: true);

    internal static Task ReachBaselineStep05Async(IPage page, string baseUrl, bool mobile) =>
        ReachSeededStep05Async(page, baseUrl, mobile, rustig: false);

    private static async Task ReachSeededStep05Async(IPage page, string baseUrl, bool mobile, bool rustig)
    {
        _ = mobile;
        var email = await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateEmailAsync(
            page, baseUrl, rustig ? "rustig-u6y1" : "baseline-u6y1");

        await CandidateJourneyE2e.PrepareDiscoveryStepForE2eAsync(email, step: 5);

        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await OnboardingRustigStep04E2eFlow.WaitForOnboardingWizardReadyAsync(page);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);

        if (rustig)
        {
            await FunctionalOnboardingInteractivityHelpers.AssertOnboardingRustigStepScreenAsync(page, 5);
        }
        else
        {
            await page.Locator(".ob-wizard:not(.ob-wizard--rustig) [data-testid=onboarding-step-5-illustrated]").WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 90_000,
            });
        }
    }
}
