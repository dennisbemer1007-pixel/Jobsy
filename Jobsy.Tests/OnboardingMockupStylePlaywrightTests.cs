using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingMockupStylePlaywrightTests : IAsyncLifetime
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
    [InlineData(false, 1024, 700)]
    [InlineData(false, 1280, 720)]
    [InlineData(true, 390, 844)]
    public async Task Start_step2_step5_step6_controls_respond(bool mobile, int width, int height)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = false;
        factory.SavedStep = 1;
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = mobile,
            IsMobile = mobile,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start"));
        await page.GetByTestId("onboarding-start-screen").WaitForAsync();
        await Assertions.Expect(page.GetByTestId("onboarding-start-journey-route")).ToBeVisibleAsync();
        await page.GetByTestId("onboarding-begin").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=1**");

        await page.GotoAsync(root + "/candidate/start?step=2");
        await page.Locator("[data-ob-client-step=\"2\"]").WaitForAsync();
        await page.Locator("button[name=\"obPreset\"]").First.ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
        await page.Locator("button[name=\"obMatrix\"][value=\"Do:Middag\"]").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
        await page.Locator("input[name=\"obStartMode\"][value=\"from\"]").CheckAsync();
        await page.GetByTestId("onboarding-start-save").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
        var saveBox = await page.GetByTestId("onboarding-start-save").BoundingBoxAsync();
        var dateBox = await page.GetByTestId("onboarding-available-from").BoundingBoxAsync();
        Assert.NotNull(saveBox);
        Assert.NotNull(dateBox);
        if (!mobile)
        {
            Assert.True(saveBox!.Width < dateBox!.Width * 1.5, "Opslaan should stay compact beside date field");
        }

        await page.GotoAsync(root + "/candidate/start?step=5");
        await page.Locator("[data-ob-client-step=\"5\"]").WaitForAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-ob-client-step=\"5\"]')?.getAttribute('data-ob-client-mounted') === '1'");
        var eduTile = page.Locator("[data-ob-client=educationGrid] button").First;
        await (mobile ? eduTile.TapAsync() : eduTile.ClickAsync());
        await page.WaitForTimeoutAsync(400);

        await page.GotoAsync(root + "/candidate/start?step=6");
        await page.Locator("[data-ob-client-step=\"6\"]").WaitForAsync();
        var dreamTile = page.Locator("[data-testid=onboarding-dream-tile]").First;
        if (await dreamTile.CountAsync() > 0)
        {
            await (mobile ? dreamTile.TapAsync() : dreamTile.ClickAsync());
            await page.WaitForTimeoutAsync(400);
        }

        await page.GetByTestId("onboarding-dream-input").FillAsync("Test droom");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=7**");
    }
}
