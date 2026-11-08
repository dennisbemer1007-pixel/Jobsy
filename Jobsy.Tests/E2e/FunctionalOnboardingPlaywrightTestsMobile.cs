using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Mobile (390) onboarding gate — runs after <see cref="FunctionalOnboardingPlaywrightTests"/> in the FunctionalE2e collection.
/// </summary>
[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class FunctionalOnboardingPlaywrightTestsMobile
{
    private const int Width = 390;
    private const int Height = 844;
    private const bool Mobile = true;
    private const string Tag = "mobile-390";

    [Fact]
    public async Task Kennismaken_step1_controls_are_interactive_mobile_390()
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
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, Width, Height, Mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, "func-kennismaken-mobile");
        await FunctionalOnboardingInteractivityHelpers.BeginKennismakenStepAsync(page, baseUrl);

        await FunctionalOnboardingInteractivityHelpers.AssertWorkStatusChipTogglesAsync(page);
        await FunctionalOnboardingInteractivityHelpers.AssertBirthDateFieldInteractiveAsync(page);
        await FunctionalOnboardingInteractivityHelpers.FillKennismakenBasicsExceptWorkStatusAsync(page);
        await FunctionalOnboardingInteractivityHelpers.SelectAnyWorkStatusChipAsync(page);

        var next = page.GetByTestId("onboarding-next");
        await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 20_000 });
        await FunctionalOnboardingInteractivityHelpers.ClickOnboardingNextAsync(page, Mobile);
        await FunctionalOnboardingInteractivityHelpers.OnboardingStepIllustrated(page, 2).WaitForAsync(new() { Timeout = 90_000 });

        await FunctionalE2eArtifacts.CaptureAsync(page, "kennismaken-after-next", Tag);
    }

    [Fact]
    public async Task Kennismaken_step1_stays_interactive_after_slow_typing_mobile_390()
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
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, Width, Height, Mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, $"func-slow-{Tag}");
        await FunctionalOnboardingInteractivityHelpers.BeginKennismakenStepAsync(page, baseUrl);

        await FunctionalOnboardingInteractivityHelpers.TypeBirthDateSlowlyAsync(page);
        await FunctionalOnboardingInteractivityHelpers.TypeHomeAddressSlowlyAsync(page);

        var address = page.Locator("#onboarding-home-address").First;
        await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, address, "address-after-typing");
        await FunctionalOnboardingInteractivityHelpers.AssertWorkStatusChipTogglesAsync(page);

        await FunctionalE2eArtifacts.CaptureAsync(page, "kennismaken-after-slow-typing", Tag);
    }

    [Fact]
    public async Task Full_onboarding_registration_through_later_exits_to_banenkaart_mobile_390()
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
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, Width, Height, Mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(180_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, "func-full-mobile");
        await FunctionalOnboardingInteractivityHelpers.RunThroughWizardToFinishAsync(page, baseUrl);

        Assert.True(
            page.Url.Contains("/banenkaart", StringComparison.OrdinalIgnoreCase)
            || await page.Locator(".ob-wizard__finish").CountAsync() > 0,
            "Expected onboarding to reach banenkaart (tests later) or finish screen.");

        await FunctionalE2eArtifacts.CaptureAsync(page, "onboarding-complete", Tag);
    }
}
