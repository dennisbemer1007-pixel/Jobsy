using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class DiscoveryOverviewRouteE2eFlow
{
    internal static async Task<string> ReachDiscoveryOverviewAsync(IPage page, string baseUrl, bool mobile)
    {
        _ = mobile;
        var email = await RegisterFreshCandidateEmailAsync(page, baseUrl, "discovery-overview");
        await CandidateJourneyE2e.EnsureTestAiConsentForE2eAsync(email);
        await CandidateJourneyE2e.SetDiscoveryJourneyStepsForE2eAsync(email, completedThrough: 10, currentStep: 10);
        await CandidateJourneyE2e.SeedFullTestBlockForE2eAsync(email, AssessmentKind.Competence);
        await CandidateJourneyE2e.SeedFullTestBlockForE2eAsync(email, AssessmentKind.Career);
        await CandidateJourneyE2e.SeedFullTestBlockForE2eAsync(email, AssessmentKind.Culture);
        await CandidateJourneyE2e.ResetDemoValuesProfileForE2eAsync(email);
        await CandidateJourneyE2e.MarkDiscoveryJourneyCompleteForOverviewE2eAsync(email);

        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000,
        });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 120_000 });
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });
        await page.WaitForFunctionAsync(
            """
            () => {
              const p = document.querySelector('#discovery-route-path');
              return p && p.getAttribute('d') && p.getAttribute('d').length > 8;
            }
            """,
            null,
            new() { Timeout = 30_000 });
        await page.WaitForTimeoutAsync(600);
        return email;
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
