using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OntdekkingsreisMockupStylePlaywrightTests : IAsyncLifetime
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
    public async Task Overview_route_clicks_all_buttons_within_viewport(bool mobile, int width, int height)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.OnboardingComplete = true;
        factory.CandidatePassportEnabled = true;
        factory.SavedStep = 10;
        factory.DiscoveryOverviewValuesAnswered = 0;
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
        page.SetDefaultTimeout(120_000);

        var t0 = DateTime.UtcNow;
        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/ontdekkingsreis"));
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 90_000 });
        await page.Locator("[data-discovery-ssr=\"1\"]").WaitForAsync(new() { Timeout = 30_000 });
        var elapsedMs = (DateTime.UtcNow - t0).TotalMilliseconds;
        Assert.True(elapsedMs < 8000, $"Discovery overview SSR took {elapsedMs}ms.");

        var routeRoot = mobile ? ".journey-route__mobile-only" : ".journey-route__desktop-only";
        await AssertButtonsWithinViewportAsync(page, width, routeRoot);

        await page.Locator($"{routeRoot} .journey-route__stop[data-phase=kust] .journey-route__stop-btn").ClickAsync();
        await AssertButtonsWithinViewportAsync(page, width, routeRoot);

        await page.Locator($"{routeRoot} .journey-route__stop[data-phase=light] .journey-route__stop-btn").ClickAsync();
        await AssertButtonsWithinViewportAsync(page, width, routeRoot);

        var adjust = mobile
            ? page.Locator($"{routeRoot} .journey-route-mobile__inline:not([hidden]) .journey-route__adjust").First
            : page.Locator($"{routeRoot} .journey-route__panel .journey-route__adjust").First;
        if (await adjust.CountAsync() > 0)
        {
            await adjust.ScrollIntoViewIfNeededAsync();
            await adjust.ClickAsync(new() { Force = mobile });
        }

        await page.GotoAsync(root + "/candidate/ontdekkingsreis?stap=overview&fase=diep");
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync();

        var stepLink = page.Locator($"{routeRoot} .journey-route__adjust").First;
        if (await stepLink.CountAsync() > 0)
        {
            await stepLink.ClickAsync();
            await page.WaitForURLAsync(u => u.Contains("stap=", StringComparison.OrdinalIgnoreCase), new() { Timeout = 60_000 });
        }
    }

    private static async Task AssertButtonsWithinViewportAsync(IPage page, int viewportWidth, string routeRoot)
    {
        var buttons = page.Locator($"{routeRoot} button, {routeRoot} a.journey-route__stop-btn");
        var count = await buttons.CountAsync();
        for (var i = 0; i < count; i++)
        {
            var btn = buttons.Nth(i);
            if (!await btn.IsVisibleAsync())
            {
                continue;
            }

            var box = await btn.BoundingBoxAsync();
            Assert.NotNull(box);
            var slack = viewportWidth <= 400 ? 0.5 : 2.5;
            Assert.True(box!.X >= -slack, $"Button {i} starts at {box.X}px");
            Assert.True(box.X + box.Width <= viewportWidth + slack,
                $"Button {i} ends at {box.X + box.Width}px (viewport {viewportWidth}px)");
        }
    }
}
