using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Seed onboarding at rustig step 4 (u6x2) after fresh registration.</summary>
internal static class OnboardingRustigStep04E2eFlow
{
    internal static Task ReachRustigStep04Async(IPage page, string baseUrl, bool mobile) =>
        ReachSeededStep04Async(page, baseUrl, mobile, rustig: true);

    internal static Task ReachBaselineStep04Async(IPage page, string baseUrl, bool mobile) =>
        ReachSeededStep04Async(page, baseUrl, mobile, rustig: false);

    private static async Task ReachSeededStep04Async(IPage page, string baseUrl, bool mobile, bool rustig)
    {
        _ = mobile;
        var email = await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateEmailAsync(
            page, baseUrl, rustig ? "rustig-u6x2" : "baseline-u6x2");
        await CandidateJourneyE2e.PrepareDiscoveryStepForE2eAsync(email, step: 4);

        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 120_000 });
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);

        if (rustig)
        {
            await FunctionalOnboardingInteractivityHelpers.AssertOnboardingRustigStepScreenAsync(page, 4);
        }
        else
        {
            await page.Locator(".ob-wizard:not(.ob-wizard--rustig) [data-testid=onboarding-step-4-illustrated]").WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 90_000,
            });
        }
    }
}
