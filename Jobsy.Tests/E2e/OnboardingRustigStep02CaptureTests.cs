using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingRustigStep02CaptureTests
{
    [Theory]
    [InlineData(1440, 900, false, "step-02-desktop")]
    [InlineData(390, 844, true, "step-02-mobile")]
    public async Task Capture_step02(int width, int height, bool mobile, string fileStem)
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

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, "rustig-u6s");
        await FunctionalOnboardingInteractivityHelpers.OpenStartWelcomeAsync(page, baseUrl);
        await page.Locator("[data-ob-rustig-screen=start]").WaitForAsync(new() { Timeout = 60_000 });
        await page.GetByTestId("onboarding-begin").ClickAsync(new() { Force = mobile });
        await page.Locator("[data-testid=onboarding-step-1-illustrated]").WaitForAsync(new() { Timeout = 90_000 });
        await FunctionalOnboardingInteractivityHelpers.FillKennismakenBasicsExceptWorkStatusAsync(page);
        var workChip = page.Locator(".work-status-chips .lobsy-choice-tile, .work-status-chips .work-status-chip").First;
        await workChip.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await workChip.ClickAsync(new() { Force = mobile });
        await page.GetByTestId("onboarding-next").ClickAsync(new() { Force = mobile });
        await page.Locator("[data-ob-rustig-screen=step-02]").WaitForAsync(new() { Timeout = 90_000 });
        await page.Locator("[data-testid=onboarding-step-2-illustrated]").WaitForAsync(new() { Timeout = 90_000 });
        await page.WaitForTimeoutAsync(800);

        await page.ScreenshotAsync(new() { Path = Path.Combine(dir, $"{fileStem}-viewport.png"), FullPage = false });
    }
}
