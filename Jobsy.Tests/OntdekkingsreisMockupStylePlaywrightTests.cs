using Jobsy.Core.Rules;
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
    public async Task Overview_ssr_status_margin_haltes_and_single_navigation(bool mobile, int width, int height)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.OnboardingComplete = true;
        factory.CandidatePassportEnabled = true;
        factory.SavedStep = 10;
        factory.DiscoveryOverviewValuesAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.StoredProfile.PhoneNumber = "+31612345678";
        factory.StoredProfile.Preferences ??= new();
        factory.StoredProfile.Preferences.HomeAddress = "Zaandam";
        factory.StoredProfile.Preferences.AboutMe = "Kok in opleiding";
        factory.StoredProfile.AvailableFromDate = new DateOnly(2027, 4, 20);
        factory.StoredProfile.Preferences.MaxTravelMinutes = 30;
        factory.StoredProfile.TestAiConsentAt = DateTime.UtcNow;

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

        var navigations = 0;
        page.FrameNavigated += (_, _) => navigations++;

        var t0 = DateTime.UtcNow;
        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/ontdekkingsreis?stap=9"));
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 90_000 });
        await page.Locator("[data-discovery-ssr=\"1\"]").WaitForAsync(new() { Timeout = 30_000 });
        var elapsedMs = (DateTime.UtcNow - t0).TotalMilliseconds;
        Assert.True(elapsedMs < 8000, $"Discovery overview SSR took {elapsedMs}ms.");
        Assert.True(navigations <= 2, $"Expected at most one redirect after sign-in, got {navigations} frame navigations.");
        await page.WaitForURLAsync(u => u.Contains("stap=overview", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });

        await DiscoveryRouteLabelPathAssert.AssertLabelsClearOfRoutePathAsync(page, mobile);

        var container = page.Locator(".discovery-ssr-container").First;
        var box = await container.BoundingBoxAsync();
        Assert.NotNull(box);
        var padLeft = await container.EvaluateAsync<double>(
            "el => parseFloat(getComputedStyle(el).paddingLeft)");
        Assert.True(padLeft >= 12, $"Overview container padding-left should be >= 12px, got {padLeft}");

        var routeRoot = mobile ? ".journey-route__mobile-only" : ".journey-route__desktop-only";
        var kustLink = mobile
            ? page.Locator($"{routeRoot} .journey-route-mobile__stop[data-phase=kust] a.journey-route__stop-link")
            : page.Locator($"{routeRoot} a.journey-route__stop-link[data-phase=kust]");
        await kustLink.ClickAsync();
        await page.WaitForURLAsync(u => u.Contains("fase=kust", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
        if (!mobile)
        {
            var kustPanel = await page.Locator(".journey-route__panel").InnerTextAsync();
            Assert.DoesNotContain("Nog niet ingevuld", kustPanel, StringComparison.OrdinalIgnoreCase);
            Assert.True(kustPanel.Contains("+316", StringComparison.Ordinal) || kustPanel.Contains("Zaandam", StringComparison.OrdinalIgnoreCase),
                $"Expected filled kust summaries, got: {kustPanel}");
        }

        var rotsLink = mobile
            ? page.Locator($"{routeRoot} .journey-route-mobile__stop[data-phase=rots] a.journey-route__stop-link")
            : page.Locator($"{routeRoot} a.journey-route__stop-link[data-phase=rots]");
        await rotsLink.ClickAsync();
        await page.WaitForURLAsync(u => u.Contains("fase=rots", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });

        var lightLink = mobile
            ? page.Locator($"{routeRoot} .journey-route-mobile__stop[data-phase=light] a.journey-route__stop-link")
            : page.Locator($"{routeRoot} a.journey-route__stop-link[data-phase=light]");
        await lightLink.ClickAsync();
        await page.WaitForURLAsync(u => u.Contains("fase=light", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });

        await page.GotoAsync(root + "/candidate/ontdekkingsreis?stap=overview&fase=diep");
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync();
        var donePills = page.Locator(".journey-route__tests .status-pill--success");
        Assert.True(await donePills.CountAsync() >= 4, "Expected four completed test rows on overview.");

        await page.GotoAsync(root + "/candidate/ontdekkingsreis?stap=shed-10");
        await page.Locator("[data-testid=\"discovery-shed\"]").WaitForAsync(new() { Timeout = 60_000 });

        await page.GotoAsync(root + "/candidate/ontdekkingsreis?stap=overview&fase=light");
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 60_000 });
        await AssertButtonsWithinViewportAsync(page, width, routeRoot);
    }

    private static async Task AssertButtonsWithinViewportAsync(IPage page, int viewportWidth, string routeRoot)
    {
        var buttons = page.Locator($"{routeRoot} button, {routeRoot} a.journey-route__stop-link[data-phase]");
        var count = await buttons.CountAsync();
        for (var i = 0; i < count; i++)
        {
            var btn = buttons.Nth(i);
            if (!await btn.IsVisibleAsync())
            {
                continue;
            }

            var btnBox = await btn.BoundingBoxAsync();
            Assert.NotNull(btnBox);
            var slack = viewportWidth <= 400 ? 0.5 : 2.5;
            Assert.True(btnBox!.X >= -slack, $"Button {i} starts at {btnBox.X}px");
            Assert.True(btnBox.X + btnBox.Width <= viewportWidth + slack,
                $"Button {i} ends at {btnBox.X + btnBox.Width}px (viewport {viewportWidth}px)");
        }
    }
}
