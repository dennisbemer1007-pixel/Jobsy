using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Click targets on calm onboarding start screen (PR 1).</summary>
[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class OnboardingRustigWelcomePlaywrightTests
{
    [Theory]
    [InlineData(1440, 900, false, "desk")]
    [InlineData(390, 844, true, "mob")]
    public async Task Rustig_welcome_hit_targets_and_begin_opens_step1(int width, int height, bool mobile, string tag)
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

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, $"rustig-welcome-{tag}");
        await FunctionalOnboardingInteractivityHelpers.OpenStartWelcomeAsync(page, baseUrl);

        await page.Locator("[data-ob-rustig-screen=start]").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000,
        });

        var later = page.GetByRole(AriaRole.Button, new() { Name = "Later verder" });
        await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, later, "later-verder");

        var privacy = page.Locator("a.ob-rustig__link--underline");
        await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, privacy, "privacy-link");

        var begin = page.GetByTestId("onboarding-begin");
        await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, begin, "onboarding-begin");
        await begin.ClickAsync(new() { Timeout = 30_000, Force = mobile });
        await page.Locator("[data-testid=onboarding-step-1-illustrated]").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });
    }
}
