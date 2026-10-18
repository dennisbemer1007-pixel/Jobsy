using Jobsy.Core.Rules;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Optional screenshot capture (set JOBSY_CAPTURE_AVAILABILITY_SCREENSHOTS=1).</summary>
[Collection("PlaywrightSmoke")]
public sealed class AvailabilityStartDatePlaywrightTests
{
    [Theory]
    [InlineData(390, 844, "mobile")]
    [InlineData(1280, 900, "desktop")]
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
            baseUrl + "/candidate/ontdekkingsreis?stap=1",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page, "app.min.css", "onboarding-wizard.css", "form-fields.css");
        await page.Locator(".ob-fields input").First.WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".ob-fields input").Nth(0).FillAsync("Alex");
        await page.Locator(".ob-fields input").Nth(1).FillAsync("Jansen");
        await page.Locator(".ob-fields input").Nth(2).FillAsync("1234AB");
        var birth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-22);
        await page.Locator("[data-testid=onboarding-birth-date] .jobsy-date-field__text").First.FillAsync(
            JobsyDateInputRules.FormatDisplayDate(birth, "nl"));
        var birthDisplay = await page.Locator("[data-testid=onboarding-birth-date] .jobsy-date-field__text").InputValueAsync();
        Assert.Contains(JobsyDateInputRules.FormatDisplayDate(birth, "nl"), birthDisplay, StringComparison.Ordinal);
        await page.Locator(".ob-fields input[type=tel]").FillAsync("0612345678");
        await KbE2e.AssertSelectorLooksStyledAsync(page, ".ob-fields input");
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"01-after-onboarding-personal-filled-{label}.png"),
            FullPage = true
        });

        await page.GotoAsync(
            baseUrl + "/candidate/ontdekkingsreis?stap=2",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page, "app.min.css", "onboarding-wizard.css", "form-fields.css");
        await page.Locator(".ob-wizard__start-option").First.WaitForAsync(new() { Timeout = 60_000 });

        await page.Locator(".ob-wizard__start-option").Filter(new() { HasText = "Per direct" }).First.ClickAsync();
        await KbE2e.AssertSelectorLooksStyledAsync(page, ".ob-wizard__start-option.is-selected");
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"02-after-onboarding-availability-immediate-{label}.png"),
            FullPage = true
        });

        await page.Locator(".ob-wizard__start-option").Filter(new() { HasText = "Vanaf een datum" }).First.ClickAsync();
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        await page.Locator("[data-testid=availability-start-date] .jobsy-date-field__text").First.FillAsync(
            JobsyDateInputRules.FormatDisplayDate(start, "nl"));
        Assert.Equal(1, await page.Locator(".ob-wizard__start-option.is-selected").CountAsync());
        var selectedText = await page.Locator(".ob-wizard__start-option.is-selected").InnerTextAsync();
        Assert.Contains("Vanaf een datum", selectedText, StringComparison.Ordinal);
        var dateDisplay = await page.Locator("[data-testid=availability-start-date] .jobsy-date-field__text").InputValueAsync();
        Assert.Equal(JobsyDateInputRules.FormatDisplayDate(start, "nl"), dateDisplay.Trim());
        await page.Locator(".ob-wizard__start-date").ScrollIntoViewIfNeededAsync();
        await CandidateJourneyE2e.ShotViewportAsync(page, Path.Combine(outDir, $"03-after-onboarding-availability-date-{label}.png"));

        await using var profileContext = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
        await PlaywrightCookieConsent.AcceptAsync(profileContext);
        var profilePage = await profileContext.NewPageAsync();
        if (await KbE2e.TryLoginAsync(profilePage, baseUrl, KbE2e.DefaultCompleteEmail, KbE2e.DefaultPassword))
        {
            await profilePage.GotoAsync(
                baseUrl + "/candidate/profile",
                new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
            await KbE2e.WaitForStyledAppAsync(profilePage, "app.min.css", "form-fields.css");
            var kompasProfile = profilePage.Locator(".kompas-tabs button.admin-sublink").Filter(new() { HasText = "Profiel" });
            if (await kompasProfile.CountAsync() > 0)
            {
                await kompasProfile.First.ClickAsync();
            }

            var availToggle = profilePage.Locator(".profile-section-nav button.admin-sublink")
                .Filter(new() { HasText = "Beschikbaarheid" });
            if (await availToggle.CountAsync() > 0)
            {
                await availToggle.First.ClickAsync();
                await profilePage.Locator("#profile-section-availability .ob-wizard__start-option").First
                    .WaitForAsync(new() { Timeout = 30_000 });
                await profilePage.Locator(".ob-wizard__start-option").Filter(new() { HasText = "Vanaf een datum" }).First
                    .ClickAsync();
                await profilePage.Locator("[data-testid=availability-start-date] input[type=date]").First
                    .FillAsync(start.ToString("yyyy-MM-dd"));
                await KbE2e.AssertSelectorLooksStyledAsync(profilePage, ".jobsy-date-field__input");
                await profilePage.WaitForTimeoutAsync(1_500);
                await profilePage.ScreenshotAsync(new()
                {
                    Path = Path.Combine(outDir, $"04-after-profile-availability-date-{label}.png"),
                    FullPage = true
                });
            }
        }

        KbE2e.AssertHealthy(capture, requireNoIsochrone404: false);
    }
}
