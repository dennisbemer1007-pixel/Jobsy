using Jobsy.Core.Rules;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Mobile halte circle alignment and tap target (390px).</summary>
[Collection("PlaywrightSmoke")]
public sealed class OntdekkingsreisMobileCirclePlaywrightTests : IAsyncLifetime
{
    public const string MobileCircle390ScreenshotPath = "/opt/cursor/artifacts/screenshots/ontdekkingsreis-mobile-circle-390.png";

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
    public async Task Mobile_390_circle_aligned_and_image_tap_activates_halte()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.OnboardingComplete = true;
        factory.CandidatePassportEnabled = true;
        factory.SavedStep = 10;
        factory.DiscoveryOverviewCompetencyAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewCareerAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewCultureAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewValuesAnswered = OnboardingWizardCatalog.MiniLevelCount;

        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/ontdekkingsreis?stap=overview&fase=kust"));
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-discovery-ssr=\"1\"]')?.dataset.discoveryHaltesBound === '1'",
            null,
            new() { Timeout = 30_000 });

        var activeImg = page.Locator("[data-testid=\"discovery-active-vignette\"] img");
        await activeImg.WaitForAsync(new() { Timeout = 30_000 });
        var imgRadius = await activeImg.EvaluateAsync<string>(
            "el => getComputedStyle(el).borderRadius");
        Assert.Equal("50%", imgRadius);

        Directory.CreateDirectory(Path.GetDirectoryName(MobileCircle390ScreenshotPath)!);
        var activeStop = page.Locator(".discovery-mobile-stop.is-open .discovery-mobile-stop__circle");
        await activeStop.ScrollIntoViewIfNeededAsync();
        await activeStop.ScreenshotAsync(new() { Path = MobileCircle390ScreenshotPath });

        await DiscoveryMobileLayoutAssert.AssertMobile390LayoutAsync(page);
        await DiscoveryHalteTapAssert.AssertHalteTapDoesNotReloadAsync(page, mobile: true);
    }
}
