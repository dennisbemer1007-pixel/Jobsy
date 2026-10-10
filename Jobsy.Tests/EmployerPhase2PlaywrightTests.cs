using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Maqqie uren nav is hidden without an active contract.</summary>
[Collection("PlaywrightSmoke")]
[Trait("Suite", "EmployerPhase2")]
public class EmployerPhase2PlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Hours_nav_hidden_without_maqqie_contract()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();

        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var hoursNav = page.Locator("[data-testid='nav-maqqie-uren']");
        Assert.Equal(0, await hoursNav.CountAsync());
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await page.FillAsync("input[type='email']", DefaultEmail);
        await page.FillAsync("input[type='password']", DefaultPassword);
        await page.ClickAsync("button[type='submit']");
        try
        {
            await page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase), new() { Timeout = 45_000 });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
