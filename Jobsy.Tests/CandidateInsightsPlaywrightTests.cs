using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c> / employer credentials.
/// </summary>
[Collection("PlaywrightSmoke")]
public class CandidateInsightsPlaywrightTests
{
    [Fact]
    public async Task Employer_insights_page_screenshots_and_no_overflow()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_EMAIL") ?? "").Trim();
        var password = (Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_PASSWORD") ?? "").Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (w, h, name) in new[] { (1440, 900, "desktop"), (390, 844, "mobile") })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = w, Height = h },
                IgnoreHTTPSErrors = true
            });
            var page = await context.NewPageAsync();
            if (!await TryLoginAsync(page, baseUrl, email, password))
            {
                return;
            }

            await page.GotoAsync(baseUrl + "/employer/kandidaatinzichten",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await page.WaitForTimeoutAsync(800);
            var body = await page.InnerTextAsync("body");
            Assert.DoesNotContain("Voorbeelddata", body, StringComparison.OrdinalIgnoreCase);
            await page.ScreenshotAsync(new() { Path = $"kandidaatinzichten-{name}.png", FullPage = true });
        }

        await using var storyCtx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        var storyPage = await storyCtx.NewPageAsync();
        if (!await TryLoginAsync(storyPage, baseUrl, email, password))
        {
            return;
        }

        await storyPage.GotoAsync(baseUrl + "/employer/kandidaatinzichten?weergave=story",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await storyPage.WaitForTimeoutAsync(800);
        Assert.DoesNotContain("Voorbeelddata", await storyPage.InnerTextAsync("body"), StringComparison.OrdinalIgnoreCase);
        await storyPage.ScreenshotAsync(new() { Path = "kandidaatinzichten-story-mobile.png", FullPage = true });

        foreach (var width in new[] { 360, 390, 430 })
        {
            await storyPage.SetViewportSizeAsync(width, 844);
            var overflow = await storyPage.EvaluateAsync<bool>("""
                () => {
                  const doc = document.documentElement;
                  return doc.scrollWidth > doc.clientWidth + 1;
                }
                """);
            Assert.False(overflow, $"Horizontal overflow at {width}px");
        }
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl, string email, string password)
    {
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.FillAsync("input[type=email], input[name=email], #email", email);
            await page.FillAsync("input[type=password], input[name=password], #password", password);
            await page.ClickAsync("button[type=submit], .login-submit");
            await page.WaitForTimeoutAsync(1500);
            return !page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
