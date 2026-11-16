using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class OnboardingV3LayoutPlaywrightTests
{
    [Theory]
    [InlineData(1440, 900, false)]
    [InlineData(390, 844, true)]
    public async Task Onboarding_steps_1_to_7_coach_does_not_overlap_content(int width, int height, bool mobile)
    {
        var (run, baseUrl) = await FunctionalOnboardingPlaywrightTestSupport.TryBeginAsync();
        if (!run)
        {
            return;
        }

        await FunctionalE2eAuth.WarmupStackAsync(baseUrl);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var email = await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateEmailAsync(page, baseUrl, $"layout-{width}");
        await CandidateJourneyE2e.SetDiscoveryJourneyStepsForE2eAsync(email, completedThrough: 0, currentStep: 1);

        for (var step = 1; step <= 7; step++)
        {
            await page.GotoAsync($"{baseUrl}/candidate/start?step={step}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await FunctionalOnboardingInteractivityHelpers.WaitForOnboardingStartStepAsync(page, step, 90_000);
            await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
            var overlap = await CoachVariantBOverlapAuditSupport.EvaluateOnboardingCoachOverlapAsync(page);
            if (mobile)
            {
                Assert.True(overlap.Skip,
                    $"Step {step} @ {width}px: mobile coach must be hidden (got overlap: {overlap.Reason}).");
                Assert.True(
                    string.Equals(overlap.Reason, "no-coach", StringComparison.Ordinal)
                    || string.Equals(overlap.Reason, "coach-tiny", StringComparison.Ordinal),
                    $"Step {step} @ {width}px: unexpected mobile coach skip reason: {overlap.Reason}.");
            }
            else
            {
                Assert.False(overlap.Skip && string.Equals(overlap.Reason, "no-coach", StringComparison.Ordinal),
                    $"Step {step} @ {width}px: expected onboarding coach dock.");
                Assert.True(overlap.Ok || overlap.Skip,
                    $"Step {step} @ {width}px: coach overlaps content: {overlap.Reason}");
                var bubble = page.Locator("[data-ob-coach-bubble]");
                if (await bubble.CountAsync() > 0)
                {
                    var text = (await bubble.InnerTextAsync()).Trim();
                    Assert.False(string.IsNullOrWhiteSpace(text),
                        $"Step {step} @ {width}px: coach bubble must not be empty.");
                }
            }
        }
    }

    [Fact]
    public async Task Step_6_dream_tiles_are_readable_on_mobile_390()
    {
        var (run, baseUrl) = await FunctionalOnboardingPlaywrightTestSupport.TryBeginAsync();
        if (!run)
        {
            return;
        }

        await FunctionalE2eAuth.WarmupStackAsync(baseUrl);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, 390, 844, mobile: true));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await OnboardingRustigStep06E2eFlow.ReachRustigStep06Async(page, baseUrl, mobile: true);
        var tiles = page.GetByTestId("onboarding-dream-tile");
        var count = await tiles.CountAsync();
        Assert.True(count >= 2, "Expected dream job tiles on step 6.");
        for (var i = 0; i < count; i++)
        {
            var box = await tiles.Nth(i).BoundingBoxAsync();
            Assert.NotNull(box);
            Assert.True(box!.Width >= 140, $"Dream tile {i} width {box.Width:F0}px < 140px.");
        }

        var missingGlyphs = await page.EvaluateAsync<string>(
            """
            () => {
              const tiles = [...document.querySelectorAll('[data-testid=onboarding-dream-tile]')];
              for (const tile of tiles) {
                const svg = tile.querySelector('span svg');
                if (!svg) return tile.querySelector('small')?.textContent?.trim() || 'tile';
                const marks = svg.querySelectorAll('path,circle,rect,line,polyline');
                if (marks.length === 0) return tile.querySelector('small')?.textContent?.trim() || 'tile';
              }
              return '';
            }
            """);
        Assert.True(string.IsNullOrEmpty(missingGlyphs),
            $"Dream tile(s) missing icon glyph: {missingGlyphs}");
    }

    [Fact]
    public async Task Step_7_consent_volgende_is_dutch_not_english_next()
    {
        var (run, baseUrl) = await FunctionalOnboardingPlaywrightTestSupport.TryBeginAsync();
        if (!run)
        {
            return;
        }

        await FunctionalE2eAuth.WarmupStackAsync(baseUrl);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, 1440, 900, mobile: false));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var email = await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateEmailAsync(page, baseUrl, "step7-label");
        await CandidateJourneyE2e.SetDiscoveryJourneyStepsForE2eAsync(email, completedThrough: 6, currentStep: 7);
        await page.GotoAsync($"{baseUrl}/candidate/start?step=7", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await FunctionalOnboardingInteractivityHelpers.WaitForOnboardingConsentScreenAsync(page);

        var next = page.GetByTestId("onboarding-next");
        await next.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        var label = (await next.InnerTextAsync()).Trim();
        Assert.DoesNotContain("next", label, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Volgende", label, StringComparison.Ordinal);
    }

}
