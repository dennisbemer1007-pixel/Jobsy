using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public class LivegangFixesPlaywrightTests
{
    [Fact]
    public async Task Notifications_route_is_not_404_when_logged_in()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? "").Trim();
        var password = (Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword).Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl, email, password))
        {
            return;
        }

        var response = await page.GotoAsync($"{baseUrl}/notifications",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        Assert.NotNull(response);
        Assert.NotEqual(404, response.Status);
        await page.Locator("h1").Filter(new() { HasText = "Meldingen" }).WaitForAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task Werkgever_vacatures_list_avoids_horizontal_overflow_at_1440_desktop()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_BM_EMAIL")
                     ?? Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_EMAIL")
                     ?? "").Trim();
        var password = (Environment.GetEnvironmentVariable("JOBSY_E2E_BM_PASSWORD")
                        ?? Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_PASSWORD")
                        ?? DefaultPassword).Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl, email, password))
        {
            return;
        }

        await page.GotoAsync($"{baseUrl}/werkgever/vacatures",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(800);
        var overflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        Assert.False(overflow, "Werkgever vacatures list should not horizontally overflow at 1440px desktop.");
    }

    [Fact]
    public async Task Werkgever_vacatures_list_avoids_horizontal_overflow_at_1280_desktop()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_BM_EMAIL")
                     ?? Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_EMAIL")
                     ?? "").Trim();
        var password = (Environment.GetEnvironmentVariable("JOBSY_E2E_BM_PASSWORD")
                        ?? Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_PASSWORD")
                        ?? DefaultPassword).Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl, email, password))
        {
            return;
        }

        await page.GotoAsync($"{baseUrl}/werkgever/vacatures",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(800);
        var overflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        Assert.False(overflow, "Werkgever vacatures list should not horizontally overflow at 1280px desktop.");
    }

    private const string DefaultPassword = "Jobsy123!";

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl, string email, string password)
    {
        try
        {
            await page.GotoAsync(baseUrl + "/login",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.FillAsync("input[type=email], input[name=email], #email", email);
            await page.FillAsync("input[type=password], input[name=password], #password", password);
            await page.ClickAsync("button[type=submit], button:has-text('Inloggen'), button:has-text('Log in')");
            await page.WaitForTimeoutAsync(1200);
            return !page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
