using Jobsy.Core.Rules;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OntdekkingsreisAccFixPlaywrightTests : IAsyncLifetime
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
    [InlineData(false, 1280, 720)]
    [InlineData(true, 390, 844)]
    public async Task Acc_fixes_redirect_shed_post_and_mobile_haltes(bool mobile, int width, int height)
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

        _ = await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/ontdekkingsreis"));
        await page.WaitForURLAsync(u => u.Contains("stap=overview", StringComparison.OrdinalIgnoreCase), new() { Timeout = 90_000 });
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-discovery-ssr=\"1\"]')?.dataset.discoveryHaltesBound === '1'",
            null,
            new() { Timeout = 30_000 });

        await page.GotoAsync(
            $"{root}/candidate/ontdekkingsreis?stap=overview&fase=kust",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 45_000 });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-discovery-ssr=\"1\"]')?.dataset.discoveryHaltesBound === '1'",
            null,
            new() { Timeout = 30_000 });

        if (mobile)
        {
            await DiscoveryMobileLayoutAssert.AssertMobile390LayoutAsync(page);
        }

        await DiscoveryHalteTapAssert.AssertHalteTapDoesNotReloadAsync(page, mobile);

        await page.GotoAsync(root + "/candidate/ontdekkingsreis?stap=overview&fase=diep");
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync();
        var adjust = mobile
            ? page.Locator(".discovery-mobile-stop.is-open .journey-route__adjust[href*='stap=7']").First
            : page.Locator(".discovery-phase-panel.is-active .journey-route__adjust[href*='stap=7']").First;
        await adjust.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 45_000 });
        await adjust.ClickAsync(new() { Force = mobile });
        await page.WaitForURLAsync(u => u.Contains("stap=7", StringComparison.OrdinalIgnoreCase), new() { Timeout = 45_000 });
        Assert.DoesNotContain("stap=overview", page.Url, StringComparison.OrdinalIgnoreCase);
        await page.Locator("[data-testid=\"discovery-test-step\"]").WaitForAsync(new() { Timeout = 60_000 });

        await page.GotoAsync(root + "/candidate/ontdekkingsreis?stap=shed-7");
        await page.Locator("[data-testid=\"discovery-shed\"]").WaitForAsync(new() { Timeout = 60_000 });
        var deepestForm = page.Locator("form[action$='/diepte']").Last;
        await deepestForm.Locator("button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(u => u.Contains("stap=shed-7", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
        Assert.DoesNotContain("Er ging iets mis", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);

        var duik = page.Locator("form[action$='/duik'] button[type=submit]");
        if (await duik.CountAsync() > 0)
        {
            await duik.ClickAsync();
            await page.WaitForURLAsync(
                u => u.Contains("stap=shed-7", StringComparison.OrdinalIgnoreCase)
                     || u.Contains("stap=7", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
            Assert.DoesNotContain("Er ging iets mis", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
        }

        await page.GotoAsync(root + "/candidate/ontdekkingsreis?stap=overview&fase=kust");
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 60_000 });
        if (!mobile)
        {
            await DiscoveryRouteLabelPathAssert.AssertLabelsClearOfRoutePathAsync(page, mobile);
        }
    }
}
