using Jobsy.Tests.E2e;
using Jobsy.Web.Onboarding;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingMockupStylePlaywrightTests : IAsyncLifetime
{
    private static readonly string[] AccLikeDreamTitles =
    [
        "Accountmanager",
        "HR-adviseur",
        "Verpleegkundige",
        "Monteur",
        "Kassière",
        "Administratief medewerker",
        "Logistiek medewerker",
        "Kok",
        "Leraar basisonderwijs",
        "Verkoper",
    ];

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
        OnboardingSsrE2eOverrides.DreamTitles = null;
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
        OnboardingSsrE2eOverrides.DreamTitles = AccLikeDreamTitles;
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
        await page.GetByTestId("onboarding-begin").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=1**");

        await page.GotoAsync(root + "/candidate/start?step=2");
        await page.Locator("[data-ob-client-step=\"2\"]").WaitForAsync();
        await page.Locator("input[name=\"obStartMode\"][value=\"from\"]").CheckAsync();
        var save = page.GetByTestId("onboarding-start-save");
        var saveBox = await save.BoundingBoxAsync();
        Assert.NotNull(saveBox);
        var scrollW = await save.EvaluateAsync<double>("el => el.scrollWidth");
        Assert.True(saveBox!.Width + 1 >= scrollW, $"Opslaan width {saveBox.Width}px vs scroll {scrollW}px");
        Assert.False(
            await save.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth + 1"),
            "Opslaan label should not be ellipsized");

        await page.Locator("button[name=\"obPreset\"]").First.ClickAsync();
        await page.Locator("button[name=\"obMatrix\"][value=\"Do:Middag\"]").ClickAsync();
        await save.ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");

        await page.GotoAsync(root + "/candidate/start?step=5");
        await page.Locator("[data-ob-client-step=\"5\"]").WaitForAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-ob-client-step=\"5\"]')?.getAttribute('data-ob-client-mounted') === '1'");
        var eduTile = page.Locator("[data-ob-client=educationGrid] button").First;
        await (mobile ? eduTile.TapAsync() : eduTile.ClickAsync());
        await page.WaitForTimeoutAsync(400);

        await page.GotoAsync(root + "/candidate/start?step=6");
        await page.Locator("[data-ob-client-step=\"6\"]").WaitForAsync();
        var tiles = page.Locator("[data-testid=onboarding-dream-tile]");
        Assert.True(await tiles.CountAsync() >= 2, "Expected acc-like dream tile set for layout measurement");
        var box0 = await tiles.Nth(0).BoundingBoxAsync();
        var box1 = await tiles.Nth(1).BoundingBoxAsync();
        Assert.NotNull(box0);
        Assert.NotNull(box1);
        Assert.True(box0!.Width < width * 0.85, "Dream tile should not span full viewport width");
        if (!mobile)
        {
            Assert.True(Math.Abs(box0.Y - box1!.Y) < 4, "First two dream tiles should share a row on desktop");
            Assert.True(box1.X > box0.X + box0.Width * 0.5, "Second tile should sit to the right of the first");
        }

        var dreamTile = tiles.First;
        await (mobile ? dreamTile.TapAsync() : dreamTile.ClickAsync());
        await page.WaitForTimeoutAsync(400);

        await page.GetByTestId("onboarding-dream-input").FillAsync("Test droom");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=7**");
        await Assertions.Expect(page.Locator(".ob-rustig__test-tab.is-current")).ToContainTextAsync("Hoe je werkt");
        var mini7 = page.GetByTestId("onboarding-step-7-mini");
        await Assertions.Expect(mini7.GetByText("Past niet").First).ToBeVisibleAsync();
        await Assertions.Expect(mini7.GetByText("Past heel goed").First).ToBeVisibleAsync();

        await page.GotoAsync(root + "/candidate/start?step=8");
        await page.Locator("[data-ob-client-step=\"8\"]").WaitForAsync();
        await Assertions.Expect(page.Locator(".ob-rustig__test-tab.is-current")).ToContainTextAsync("Leuk");
        await Assertions.Expect(page.Locator(".ob-rustig__test-tab.is-done").First).ToContainTextAsync("Hoe je werkt");
    }
}
