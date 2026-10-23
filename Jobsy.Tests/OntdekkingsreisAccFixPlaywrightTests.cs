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

        var response = await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/ontdekkingsreis"));
        Assert.NotNull(response);
        await page.WaitForURLAsync(u => u.Contains("stap=overview", StringComparison.OrdinalIgnoreCase), new() { Timeout = 90_000 });
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 60_000 });

        var routeRoot = mobile ? ".journey-route__mobile-only" : ".journey-route__desktop-only";
        foreach (var phase in new[] { "kust", "rots", "diep", "light" })
        {
            var link = mobile
                ? page.Locator($"{routeRoot} .journey-route-mobile__stop[data-phase={phase}] a.journey-route__stop-link")
                : page.Locator($"{routeRoot} a.journey-route__stop-link[data-phase={phase}]");
            await link.ClickAsync(new() { Force = mobile });
            await page.WaitForURLAsync(u => u.Contains($"fase={phase}", StringComparison.OrdinalIgnoreCase), new() { Timeout = 45_000 });
            if (mobile)
            {
                var panel = page.Locator($"#discovery-panel-{phase}");
                await panel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 45_000 });
            }
        }

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
            await page.WaitForURLAsync(u => u.Contains("/candidate/ontdekkingsreis", StringComparison.OrdinalIgnoreCase), new() { Timeout = 60_000 });
            Assert.DoesNotContain("Er ging iets mis", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
        }

        await page.GotoAsync(root + "/candidate/ontdekkingsreis?stap=overview&fase=kust");
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 60_000 });
        await DiscoveryRouteLabelPathAssert.AssertLabelsClearOfRoutePathAsync(page, mobile);

    }

}
