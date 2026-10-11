using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Optional screenshot capture (set JOBSY_CAPTURE_AVAILABILITY_SCREENSHOTS=1).</summary>
[Collection("PlaywrightSmoke")]
public sealed class AvailabilityStartDatePlaywrightTests
{
    [Theory]
    [InlineData(390, 844, "mobile")]
    public async Task Capture_availability_start_date_screenshots(int width, int height, string label)
    {
        if (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_AVAILABILITY_SCREENSHOTS") != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await KbE2e.IsReachableAsync(baseUrl))
        {
            return;
        }

        var outDir = Environment.GetEnvironmentVariable("JOBSY_SCREENSHOT_DIR")
            ?? "/opt/cursor/artifacts/screenshots/availability-start-date";
        Directory.CreateDirectory(outDir);

        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        var capture = KbE2e.AttachCapture(page);

        var journeyEmail = Environment.GetEnvironmentVariable("JOBSY_E2E_INCOMPLETE_EMAIL") ?? KbE2e.DefaultIncompleteEmail;
        if (!await KbE2e.TryLoginAsync(page, baseUrl, journeyEmail, KbE2e.DefaultPassword))
        {
            return;
        }

        await page.GotoAsync(
            baseUrl + "/candidate/ontdekkingsreis?stap=2",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page, "app.min.css", "onboarding-wizard.css");
        await page.Locator(".ob-wizard__start-option").First.WaitForAsync(new() { Timeout = 60_000 });

        await page.Locator(".ob-wizard__start-option").Filter(new() { HasText = "Per direct" }).First.ClickAsync();
        await KbE2e.AssertSelectorLooksStyledAsync(page, ".ob-wizard__start-option.is-selected");
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"before-onboarding-beschikbaarheid-immediate-{label}.png"),
            FullPage = true
        });

        await page.Locator(".ob-wizard__start-option").Filter(new() { HasText = "Vanaf een datum" }).First.ClickAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7).ToString("yyyy-MM-dd");
        await page.Locator(".ob-wizard__start-date input[type=date]").FillAsync(startDate);
        await KbE2e.AssertSelectorLooksStyledAsync(page, ".ob-wizard__start-date input");
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"after-onboarding-beschikbaarheid-from-date-{label}.png"),
            FullPage = true
        });

        if (!await KbE2e.TryLoginAsync(page, baseUrl, KbE2e.DefaultCompleteEmail, KbE2e.DefaultPassword))
        {
            return;
        }

        await page.GotoAsync(
            baseUrl + "/candidate/paspoort?tab=data",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page, "app.min.css");
        var availToggle = page.Locator("button.admin-sublink").Filter(new() { HasText = "Beschikbaarheid" });
        if (await availToggle.CountAsync() == 0)
        {
            return;
        }

        await availToggle.ClickAsync();
        await page.Locator(".ob-wizard__start-option").First.WaitForAsync(new() { Timeout = 30_000 });
        await page.Locator(".ob-wizard__start-option").Filter(new() { HasText = "Vanaf een datum" }).First.ClickAsync();
        await page.Locator(".ob-wizard__start-date input[type=date]").FillAsync(startDate);
        await KbE2e.AssertSelectorLooksStyledAsync(page, ".ob-wizard__start-option.is-selected");
        await page.WaitForTimeoutAsync(2_000);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"after-profile-availability-from-date-{label}.png"),
            FullPage = true
        });

        KbE2e.AssertHealthy(capture, requireNoIsochrone404: false);
    }
}
