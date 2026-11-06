using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Seed DB at step 4 after registration (single navigation — no welcome MarkStarted race).</summary>
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
        await WaitForOnboardingWizardReadyAsync(page);
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

    internal static async Task WaitForOnboardingWizardReadyAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 120_000 });
        var loading = page.Locator(".ob-wizard__loading");
        if (await loading.CountAsync() > 0)
        {
            await loading.First.WaitForAsync(new()
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 120_000,
            });
        }
    }
}
