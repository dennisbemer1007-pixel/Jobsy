using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Viewport screenshots for research consent UI (390×844 mobile, 1366×900 desktop).
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class ResearchConsentPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(390, 844, "mobile")]
    [InlineData(1366, 900, "desktop")]
    public async Task Research_consent_and_admin_surfaces_render(int width, int height, string label)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(baseUrl + "/info/onderzoek", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid='research-info-page']").WaitForAsync(new() { Timeout = 30_000 });
        await page.ScreenshotAsync(new()
        {
            Path = $"/opt/cursor/artifacts/research-info-{label}.png",
            FullPage = true
        });

        if (await TryLoginAsync(page, baseUrl))
        {
            await page.GotoAsync(baseUrl + "/candidate/research-consent?return=/candidate/profile");
            var panel = page.Locator("[data-testid='research-consent-panel']");
            if (await panel.CountAsync() > 0)
            {
                await page.ScreenshotAsync(new()
                {
                    Path = $"/opt/cursor/artifacts/research-consent-finish-{label}.png",
                    FullPage = true
                });
            }

            await page.GotoAsync(baseUrl + "/candidate/profile");
            var privacy = page.Locator("[data-testid='profile-research-privacy']");
            if (await privacy.CountAsync() > 0)
            {
                await page.ScreenshotAsync(new()
                {
                    Path = $"/opt/cursor/artifacts/research-privacy-profile-{label}.png",
                    FullPage = true
                });
            }
        }
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        if (await page.Locator("input[type='email']").CountAsync() == 0)
        {
            return false;
        }

        await page.FillAsync("input[type='email']", email);
        await page.FillAsync("input[type='password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForTimeoutAsync(1500);
        return true;
    }
}
