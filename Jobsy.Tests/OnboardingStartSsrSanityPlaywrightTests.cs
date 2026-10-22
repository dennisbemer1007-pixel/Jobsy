using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingStartSsrSanityPlaywrightTests : IAsyncLifetime
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

    [Fact]
    public async Task Ssr_step2_date_and_preset_tile_behavior()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(OnboardingClientStepsPlaywrightSupport.ContextOptions(mobile: false));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=1"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-ob-client-step=\"1\"]')?.getAttribute('data-ob-client-mounted') === '1'");
        Assert.Equal(0, await page.Locator("script[src*='blazor.web.js']").CountAsync());
        Assert.Equal(0, await page.GetByTestId("onboarding-back").CountAsync());
        Assert.Equal(0, await page.Locator("[data-ob-nav-next]").CountAsync());

        await FillStep1AndGoToStep2Async(page);

        await page.Locator("[data-ob-client-step=\"2\"]").WaitForAsync();
        Assert.Equal(1, await page.GetByTestId("onboarding-next").CountAsync());
        Assert.Equal(1, await page.GetByTestId("onboarding-back").CountAsync());
        Assert.Equal(0, await page.Locator("[data-ob-nav-next]").CountAsync());

        var hoursLabel = page.Locator("[data-ob-client=hoursLabel]");
        var hoursBefore = (await hoursLabel.InnerTextAsync()).Trim();
        var weekendTile = page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile").Filter(new() { HasText = "Weekenden" });
        await weekendTile.ClickAsync(new() { Force = true });
        await Assertions.Expect(hoursLabel).ToHaveTextAsync(hoursBefore);
        await Assertions.Expect(page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile[data-ob-code=custom]")).ToHaveAttributeAsync("aria-pressed", "false");
        await page.WaitForTimeoutAsync(800);

        await page.Locator("[data-ob-client=startFromDate]").CheckAsync();
        var dateInput = page.Locator("[data-ob-client=availableFrom]");
        await dateInput.FillAsync("15032027");
        await dateInput.BlurAsync();
        await page.WaitForTimeoutAsync(400);
        await Assertions.Expect(dateInput).ToHaveValueAsync("15-03-2027");

        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=1**");
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-ob-client-step=\"1\"]')?.getAttribute('data-ob-client-mounted') === '1'");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
        await page.Locator("[data-ob-client-step=\"2\"]").WaitForAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-ob-client-step=\"2\"]')?.getAttribute('data-ob-client-mounted') === '1'");

        weekendTile = page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile").Filter(new() { HasText = "Weekenden" });
        dateInput = page.Locator("[data-ob-client=availableFrom]");
        await Assertions.Expect(weekendTile).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(dateInput).ToHaveValueAsync("15-03-2027");

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start/wizard?step=3**");
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
