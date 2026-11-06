using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingRustigStep05CaptureTests
{
    [Theory]
    [InlineData(1440, 900, false, "step-05-desktop")]
    [InlineData(390, 844, true, "step-05-mobile")]
    public async Task Capture_step05(int width, int height, bool mobile, string fileStem)
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
                  ?? "/opt/cursor/artifacts/onboarding-rustig-start";
        Directory.CreateDirectory(dir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await OnboardingRustigStep05E2eFlow.ReachRustigStep05Async(page, baseUrl, mobile);
        await page.Locator(".ob-wizard__education-grid .lobsy-choice-tile").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.WaitForTimeoutAsync(400);

        await page.ScreenshotAsync(new() { Path = Path.Combine(dir, $"{fileStem}-viewport.png"), FullPage = false });
    }
}
