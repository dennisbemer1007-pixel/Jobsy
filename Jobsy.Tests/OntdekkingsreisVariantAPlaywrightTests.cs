using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Screenshots of the discovery journey question screen (variant A layout).</summary>
[Collection("PlaywrightSmoke")]
public class OntdekkingsreisVariantAPlaywrightTests
{
    [Fact]
    public async Task Question_screen_screenshots_when_e2e_available()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var artifacts = "/opt/cursor/artifacts/screenshots";
        Directory.CreateDirectory(artifacts);

        foreach (var (width, height, name, mobile) in new[]
                 {
                     (1280, 800, "ontdekkingsreis-vraag-desktop-1280x800.png", false),
                     (390, 844, "ontdekkingsreis-vraag-mobile-390x844.png", true),
                 })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = mobile,
                HasTouch = mobile,
                IgnoreHTTPSErrors = true,
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);

            await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            if (await page.Locator("input[name=\"email\"]").CountAsync() > 0)
            {
                await page.FillAsync("input[name=\"email\"]", "kandidaat@jobsy.local");
                await page.FillAsync("input[name=\"password\"]", "Jobsy123!");
                await page.ClickAsync("button.login-submit");
                await page.WaitForURLAsync(u => !u.ToString().Contains("/login", StringComparison.OrdinalIgnoreCase),
                    new() { Timeout = 60_000 });
            }

            await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis?stap=8", new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 90_000,
            });

            await page.WaitForSelectorAsync(".journey-step .test-flow--compact, .journey-step h1, .journey-shed",
                new() { Timeout = 45_000 });

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifacts, name),
                FullPage = false,
            });
        }
    }
}
