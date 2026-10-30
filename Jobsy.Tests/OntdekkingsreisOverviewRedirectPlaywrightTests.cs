using Jobsy.Core.Rules;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OntdekkingsreisOverviewRedirectPlaywrightTests : IAsyncLifetime
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
    public async Task Completed_onboarding_after_shed10_bare_get_lands_on_overview(bool mobile, int width, int height)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.FinishReached = true;
        factory.OnboardingComplete = false;
        factory.CandidatePassportEnabled = true;
        factory.SavedStep = 2;
        factory.StoredProfile.TestAiConsentAt = DateTime.UtcNow;
        factory.StoredProfile.Preferences ??= new();
        factory.StoredProfile.AvailableFromDate = new DateOnly(2027, 4, 20);
        factory.StoredProfile.Preferences.MaxTravelMinutes = 30;
        factory.DiscoveryOverviewCompetencyAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewCareerAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewCultureAnswered = OnboardingWizardCatalog.MiniLevelCount;
        factory.DiscoveryOverviewValuesAnswered = OnboardingWizardCatalog.MiniLevelCount;

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

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/ontdekkingsreis?stap=shed-10"));
        await page.Locator("[data-testid=\"discovery-shed\"]").WaitForAsync(new() { Timeout = 60_000 });

        var response = await page.GotoAsync(root + "/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        Assert.NotNull(response);
        Assert.True(
            response!.Status is 200 or 303,
            $"Expected overview render or redirect, got {response.Status}");
        await page.WaitForURLAsync(
            u => u.Contains("stap=overview", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 45_000 });
        Assert.DoesNotContain("/candidate/start", page.Url, StringComparison.OrdinalIgnoreCase);
        await page.Locator("[data-testid=\"discovery-overview-route\"]").WaitForAsync(new() { Timeout = 45_000 });
    }
}
