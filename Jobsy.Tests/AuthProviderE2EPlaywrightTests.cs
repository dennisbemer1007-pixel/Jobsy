using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Auth provider E2E (file 08 flows 10–11). Soft-skips without host / fake OIDC.
/// Never production. Full admin Google block is covered by unit AuthHardening / role tests.
/// </summary>
[Collection("PlaywrightSmoke")]
public class AuthProviderE2EPlaywrightTests
{
    [Fact]
    public async Task Never_targets_production_host()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Assert.DoesNotContain("lobsy.nl", baseUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_returnUrl_hides_google_when_configured()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Assert.DoesNotContain("lobsy.nl", baseUrl, StringComparison.OrdinalIgnoreCase);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 }
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            baseUrl + "/login?returnUrl=/admin/users",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        // Google must not appear on admin-targeted login (02/03).
        var google = page.Locator("button:has-text('Google'), a:has-text('Google'), [data-provider=google]");
        Assert.Equal(0, await google.CountAsync());
        await Assertions.Expect(page.Locator("h1").First).ToBeVisibleAsync();
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl + "/login");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
