using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class OnboardingRustigStep06PlaywrightTests
{
    [Theory]
    [InlineData(1440, 900, false, "desk")]
    [InlineData(390, 844, true, "mob")]
    public async Task Step06_droom_navigation_and_hit_targets(int width, int height, bool mobile, string _)
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

        await OnboardingRustigStep06E2eFlow.ReachRustigStep06Async(page, baseUrl, mobile);
        await page.Locator("[data-ob-rustig-screen=step-06]").WaitForAsync(new() { Timeout = 60_000 });

        var dreamChip = page.Locator(".ob-rustig__dream").First;
        if (await dreamChip.CountAsync() > 0)
        {
            await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, dreamChip, "dream-chip");
            await dreamChip.ClickAsync(new() { Force = mobile });
        }

        var input = page.Locator("#onboarding-rustig-dream-input");
        await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, input, "dream-input");
        await input.FillAsync("Software developer");

        var next = page.GetByTestId("onboarding-next");
        await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, next, "volgende");
        await FunctionalOnboardingInteractivityHelpers.ClickOnboardingNextAsync(page, mobile);
        await page.Locator(".ob-wizard__consent, [data-ob-rustig-screen=\"step-07\"], .test-likert").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });

        await FunctionalOnboardingInteractivityHelpers.ClickOnboardingBackAsync(page, mobile);
        await page.Locator("[data-ob-rustig-screen=step-06]").WaitForAsync(new() { Timeout = 60_000 });
        var value = await input.InputValueAsync();
        Assert.Contains("Software", value, StringComparison.OrdinalIgnoreCase);
    }
}
