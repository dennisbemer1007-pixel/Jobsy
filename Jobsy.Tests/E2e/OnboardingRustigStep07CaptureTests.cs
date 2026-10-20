using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingRustigStep07CaptureTests
{
    [Theory]
    [InlineData(1440, 900, false, "step-07-desktop")]
    [InlineData(390, 844, true, "step-07-mobile")]
    public async Task Capture_step07(int width, int height, bool mobile, string fileStem)
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
                  ?? "/opt/cursor/artifacts/onboarding-rustig-step07";
        Directory.CreateDirectory(dir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await OnboardingRustigStep07E2eFlow.ReachRustigStep07Async(page, baseUrl, mobile);
        await page.Locator("[data-ob-rustig-screen=step-07]").WaitForAsync(new() { Timeout = 60_000 });

        var consent = page.Locator("#onboarding-field-consent input[type=checkbox]");
        if (await consent.CountAsync() > 0 && !await consent.IsCheckedAsync())
        {
            await consent.ClickAsync(new() { Force = mobile });
        }

        await page.WaitForTimeoutAsync(800);
        await ResetRustigCaptureScrollAsync(page);
        await page.Locator(".ob-rustig__segments").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000,
        });
        await page.Locator(".ob-rustig__meta").Filter(new() { HasText = "Stap 7 van 10" }).WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000,
        });

        if (!mobile)
        {
            var chromeBox = await page.Locator(".ob-rustig__chrome").BoundingBoxAsync();
            Assert.NotNull(chromeBox);
            Assert.True(chromeBox.Y >= -2 && chromeBox.Y < 120,
                $"Desktop capture: progress chrome should be at top (y={chromeBox.Y}).");
        }

        await page.ScreenshotAsync(new() { Path = Path.Combine(dir, $"{fileStem}-viewport.png"), FullPage = false });
    }

    private static async Task ResetRustigCaptureScrollAsync(IPage page)
    {
        await page.EvaluateAsync(
            """
            () => {
              window.scrollTo(0, 0);
              document.documentElement.scrollTop = 0;
              document.body.scrollTop = 0;
              for (const sel of ['.app-main', '.app-main__content', '.ob-wizard__content', '.ob-rustig__body']) {
                const el = document.querySelector(sel);
                if (el) el.scrollTop = 0;
              }
            }
            """);
        await page.WaitForTimeoutAsync(150);
    }
}
