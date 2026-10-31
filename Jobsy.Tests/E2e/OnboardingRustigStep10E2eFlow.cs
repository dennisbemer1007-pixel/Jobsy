using Jobsy.Core.Enums;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Seed onboarding at step 10 (Waarden mini-test) with consent.</summary>
internal static class OnboardingRustigStep10E2eFlow
{
    internal static Task ReachRustigStep10Async(IPage page, string baseUrl, bool mobile) =>
        ReachSeededStep10Async(page, baseUrl, mobile, rustig: true);

    internal static Task ReachBaselineStep10Async(IPage page, string baseUrl, bool mobile) =>
        ReachSeededStep10Async(page, baseUrl, mobile, rustig: false);

    private static async Task ReachSeededStep10Async(IPage page, string baseUrl, bool mobile, bool rustig)
    {
        _ = mobile;
        var email = await RegisterFreshCandidateEmailAsync(page, baseUrl, "rustig-u10");
        await CandidateJourneyE2e.EnsureTestAiConsentForE2eAsync(email);
        await CandidateJourneyE2e.SeedMiniBlockCompleteForE2eAsync(email, AssessmentKind.Competence);
        await CandidateJourneyE2e.SeedMiniBlockCompleteForE2eAsync(email, AssessmentKind.Career);
        await CandidateJourneyE2e.SeedMiniBlockCompleteForE2eAsync(email, AssessmentKind.Culture);
        await CandidateJourneyE2e.SetDiscoveryJourneyStepsForE2eAsync(email, completedThrough: 9, currentStep: 10);
        await CandidateJourneyE2e.ResetDemoValuesProfileForE2eAsync(email);

        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await FunctionalOnboardingInteractivityHelpers.WaitForCircuitReadyAsync(page, 120_000);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);

        if (rustig)
        {
            await page.Locator("[data-ob-rustig-screen=step-10]").WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 90_000,
            });
        }
        else
        {
            await page.Locator("[data-onboarding-step=\"10\"]").WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 90_000,
            });
        }
    }

    private static async Task<string> RegisterFreshCandidateEmailAsync(IPage page, string baseUrl, string emailPrefix)
    {
        var email = $"{emailPrefix}.{Guid.NewGuid():N}@jobsy.local";
        await CandidateJourneyE2e.ClearCapturedEmailCodesAsync(baseUrl);
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Func");
        await page.Locator("button.login-submit, button.au-submit[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Expected OTP from E2E mail capture.");
        }

        await page.Locator("input[name=code]").FillAsync(code);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });
        return email;
    }
}
