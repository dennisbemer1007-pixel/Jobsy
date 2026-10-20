using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingRustigStep09CaptureTests
{
    [Theory]
    [InlineData(1440, 900, false, "step-09-desktop")]
    [InlineData(390, 844, true, "step-09-mobile")]
    public async Task Capture_step09(int width, int height, bool mobile, string fileStem)
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
                  ?? "/opt/cursor/artifacts/onboarding-rustig-step09";
        Directory.CreateDirectory(dir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await OnboardingRustigStep09E2eFlow.ReachRustigStep09Async(page, baseUrl, mobile);
        await page.Locator("[data-ob-rustig-screen=step-09]").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(800);
        await page.ScreenshotAsync(new() { Path = Path.Combine(dir, $"{fileStem}-viewport.png"), FullPage = false });
    }
}
