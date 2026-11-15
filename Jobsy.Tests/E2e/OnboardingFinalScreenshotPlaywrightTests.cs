using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class OnboardingFinalScreenshotPlaywrightTests
{
    [Theory]
    [InlineData(1440, 900, false, "1440")]
    [InlineData(390, 844, true, "390")]
    public async Task Fresh_candidate_onboarding_steps_1_to_7_screenshots(int width, int height, bool mobile, string tag)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_FINAL_SCREENSHOTS"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var (run, baseUrl) = await FunctionalOnboardingPlaywrightTestSupport.TryBeginAsync();
        if (!run)
        {
            return;
        }

        var outDir = "/opt/cursor/artifacts/onboarding-final";
        Directory.CreateDirectory(outDir);

        await FunctionalE2eAuth.WarmupStackAsync(baseUrl);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(300_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, $"final-{tag}");
        await FunctionalOnboardingInteractivityHelpers.BeginKennismakenStepAsync(page, baseUrl);
        await FunctionalOnboardingInteractivityHelpers.FillKennismakenBasicsExceptWorkStatusAsync(page);
        await FunctionalOnboardingInteractivityHelpers.SelectAnyWorkStatusChipAsync(page);

        for (var step = 1; step <= 7; step++)
        {
            await FunctionalOnboardingInteractivityHelpers.WaitForOnboardingStartStepAsync(page, step, 180_000);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(outDir, $"step-{step:D2}-{tag}.png"),
                FullPage = true,
            });

            if (step == 7)
            {
                break;
            }

            if (step == 1)
            {
                await FunctionalOnboardingInteractivityHelpers.AdvanceFromStep1ToStep2Async(page);
            }
            else if (step == 2)
            {
                await FunctionalOnboardingInteractivityHelpers.AdvanceFromStep2ToStep3Async(page, mobile);
            }
            else if (step == 3)
            {
                await FunctionalOnboardingInteractivityHelpers.SelectTransportAndTravel30Async(page);
                await VroegResultaatPlaywrightTests.ClickNextAsync(page);
                await FunctionalOnboardingInteractivityHelpers.WaitForOnboardingStartStepAsync(page, 4, 180_000);
            }
            else if (step is 4 or 5)
            {
                await FunctionalOnboardingInteractivityHelpers.ClickOptionalSkipOrNextAsync(page);
                await FunctionalOnboardingInteractivityHelpers.WaitForOnboardingStartStepAsync(page, step + 1, 180_000);
            }
            else if (step == 6)
            {
                await FunctionalOnboardingInteractivityHelpers.ClickDreamUnknownOrNextAsync(page);
                await FunctionalOnboardingInteractivityHelpers.WaitForOnboardingStartStepAsync(page, 7, 180_000);
            }
        }
    }
}