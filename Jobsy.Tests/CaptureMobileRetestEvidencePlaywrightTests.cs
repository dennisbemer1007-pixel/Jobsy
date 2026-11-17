using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Run with CAPTURE_PR_SCREENSHOTS=1 to write before/after PNGs under /opt/cursor/artifacts/.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CaptureMobileRetestEvidencePlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const string ArtifactsDir = "/opt/cursor/artifacts";

    public async ValueTask InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("CAPTURE_PR_SCREENSHOTS") != "1")
        {
            return;
        }

        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        Directory.CreateDirectory(ArtifactsDir);
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
    public async Task Capture_mobile_retest_evidence_pngs()
    {
        if (Environment.GetEnvironmentVariable("CAPTURE_PR_SCREENSHOTS") != "1")
        {
            return;
        }

        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            SavedStep = 1,
        };
        factory.StoredProfile.PhoneNumber = "+31612345678";
        factory.FinishReached = true;
        factory.OnboardingComplete = true;
        factory.CandidatePassportEnabled = true;
        factory.DiscoveryOverviewValuesAnswered = Jobsy.Core.Rules.OnboardingWizardCatalog.MiniLevelCount;

        var root = factory.EnsureServerAddress();

        await CaptureOnboardingStep1Async();
        await CaptureOnboardingStep4Async();
        await CaptureOnboardingStep8TerugAsync();
        await CapturePaspoortAsync(root);
        await CaptureDesktopHelpAsync();
        await CaptureStartKompasAsync();
    }

    private async Task CaptureOnboardingStep1Async()
    {
        await using var stepFactory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            SavedStep = 1,
        };
        var stepRoot = stepFactory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await page.GotoAsync(stepRoot + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=1"));
        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync();
        var address = page.Locator("#onboarding-home-address");
        await address.FillAsync("");
        await address.PressSequentiallyAsync("Jan van", new() { Delay = 40 });
        await page.Locator(".address-suggestions__item").First.WaitForAsync(new() { Timeout = 15_000 });
        await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactsDir, "onboarding-step1-address-suggest-390-after.png"), FullPage = true });
    }

    private async Task CaptureOnboardingStep4Async()
    {
        await using var stepFactory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            SavedStep = 4,
        };
        var stepRoot = stepFactory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(stepRoot + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=4"));
        await page.Locator("[data-ob-client-step=\"4\"]").WaitForAsync();
        var noWork = page.Locator("[data-testid=onboarding-no-work]");
        if (await noWork.IsCheckedAsync())
        {
            await noWork.UncheckAsync();
        }

        await page.Locator("[data-ob-client=\"jobEmployer\"]").First.WaitForAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactsDir, "onboarding-step4-full-width-390-after.png"), FullPage = true });
    }

    private async Task CaptureOnboardingStep8TerugAsync()
    {
        await using var stepFactory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            SavedStep = 8,
        };
        var stepRoot = stepFactory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(stepRoot + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=8&q=1"));
        await page.Locator("[data-ob-client-step=\"8\"]").WaitForAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactsDir, "onboarding-step8-terug-390-after.png"), FullPage = true });
    }

    private async Task CapturePaspoortAsync(string root)
    {
        await using var context = await _browser!.NewContextAsync(MobileContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/paspoort"));
        await page.GotoAsync(root + "/candidate/paspoort");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactsDir, "paspoort-lobster-clearance-390-after.png"), FullPage = true });
    }

    private async Task CaptureDesktopHelpAsync()
    {
        await using var stepFactory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            SavedStep = 1,
        };
        var stepRoot = stepFactory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(stepRoot + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=1"));
        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync();
        await page.Locator(".admin-topbar--candidate-onboarding .page-help__btn").ScreenshotAsync(
            new() { Path = Path.Combine(ArtifactsDir, "onboarding-help-icon-1440-after.png") });
    }

    private async Task CaptureStartKompasAsync()
    {
        await using var kompasFactory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            FinishReached = true,
        };
        var kompasRoot = kompasFactory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(kompasRoot + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start/kompas"));
        await page.Locator("[data-ob-rustig-screen=\"kompas\"]").WaitForAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactsDir, "start-kompas-coach-390-after.png"), FullPage = true });
    }

    private static BrowserNewContextOptions MobileContext()
        => new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
        };
}
