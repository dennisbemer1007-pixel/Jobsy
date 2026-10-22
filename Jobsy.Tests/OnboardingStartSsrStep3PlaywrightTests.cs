using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingStartSsrStep3PlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ssr_step3_tiles_legend_and_step4_ssr(bool mobile)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(OnboardingClientStepsPlaywrightSupport.ContextOptions(mobile));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=1"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync();
        await FillStep1AndGoToStep2Async(page);
        await page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile").Filter(new() { HasText = "Weekenden" })
            .ClickAsync(new() { Force = true });
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=3**");

        await page.Locator("[data-ob-client-step=\"3\"]").WaitForAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-ob-client-step=\"3\"]')?.getAttribute('data-ob-client-mounted') === '1'");
        Assert.Equal(0, await page.Locator("script[src*='blazor.web.js']").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-next").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-back").CountAsync());

        var bikeTile = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile[data-ob-code=Fiets]");
        await bikeTile.ClickAsync(new() { Force = true });
        var travel20 = page.Locator("[data-ob-client=travelGrid] .lobsy-choice-tile[data-ob-travel=\"20\"]");
        await travel20.ClickAsync(new() { Force = true });
        await Assertions.Expect(travel20).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
        await Assertions.Expect(page.Locator("[data-ob-client=travelLegendValue]")).ToHaveTextAsync("20");
        await Assertions.Expect(bikeTile).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
        await page.WaitForTimeoutAsync(500);

        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=3**");
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-ob-client-step=\"3\"]')?.getAttribute('data-ob-client-mounted') === '1'");

        bikeTile = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile[data-ob-code=Fiets]");
        await Assertions.Expect(bikeTile).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(page.Locator("[data-ob-client=travelGrid] .lobsy-choice-tile[data-ob-travel=\"20\"]"))
            .ToHaveAttributeAsync("aria-pressed", "true");

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=4**");
        await page.Locator("[data-ob-client-step=\"4\"]").WaitForAsync();
        Assert.Equal(0, await page.Locator("script[src*='blazor.web.js']").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-next").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-back").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-skip").CountAsync());

        await page.GetByTestId("onboarding-skip").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start/wizard?step=5**");
    }

    private static async Task FillStep1AndGoToStep2Async(IPage page)
    {
        await page.Locator("[data-ob-client=firstName]").FillAsync("Onboarding");
        await page.Locator("[data-ob-client=lastName]").FillAsync("Tester");
        await page.Locator("#onboarding-home-address").FillAsync("2671GB");
        await page.Locator(".address-suggestions__item").First.ClickAsync(new() { Force = true });
        await page.Locator("[data-ob-client=birthDate]").FillAsync("12-03-1990");
        await page.Locator(".work-status-chip[data-ob-code=scholier]").ClickAsync();
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
    }
}
