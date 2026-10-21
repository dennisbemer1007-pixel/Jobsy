using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Screenshots for calm onboarding start (set JOBSY_CAPTURE_ONBOARDING_RUSTIG=1).</summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingRustigWelcomeCaptureTests
{
    [Theory]
    [InlineData(1440, 900, false, "desktop-1440.png")]
    [InlineData(390, 844, true, "mobile-390.png")]
    public async Task Capture_rustig_welcome(int width, int height, bool mobile, string fileName)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_ONBOARDING_RUSTIG") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        var dir = Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_RUSTIG_ARTIFACTS")
                  ?? "/opt/cursor/artifacts/screenshots/onboarding-rustig-start";
        Directory.CreateDirectory(dir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, "rustig-cap");
        await FunctionalOnboardingInteractivityHelpers.OpenStartWelcomeAsync(page, baseUrl);
        await page.Locator("[data-ob-rustig-screen=start-journey]").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(800);
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        await page.ScreenshotAsync(new() { Path = Path.Combine(dir, $"{baseName}-full.png"), FullPage = true });
        await page.ScreenshotAsync(new() { Path = Path.Combine(dir, $"{baseName}-viewport.png"), FullPage = false });
    }
}
