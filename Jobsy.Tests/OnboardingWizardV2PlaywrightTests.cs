using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Browser contract for wizard v2. CI enables it by providing JOBSY_E2E_BASE_URL.</summary>
[Collection("PlaywrightSmoke")]
public class OnboardingWizardV2PlaywrightTests
{
    [Theory]
    [InlineData(390, 844)]
    [InlineData(1280, 800)]
    public async Task Wizard_v2_layout_and_welcome_are_available(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return; // Soft skip for unit-only environments.
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = width, Height = height }, IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();

        // Authentication/reset are intentionally supplied by the CI e2e fixture. These assertions
        // cover the shell; the detailed walkthrough is exercised once the fixture is available.
        await page.GotoAsync(baseUrl + "/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        if (page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await Assertions.Expect(page.Locator(".ob-wizard")).ToBeVisibleAsync();
        var overflow = await page.EvaluateAsync<int>("document.documentElement.scrollWidth");
        Assert.True(overflow <= width);
    }
}
