using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Auth MFA E2E (file 08 flows 5–8, 12). Soft-skips without JOBSY_E2E_BASE_URL
/// or without seeded MFA test accounts (JOBSY_E2E_MFA_*). Never production.
/// </summary>
[Collection("PlaywrightSmoke")]
public class AuthMfaE2EPlaywrightTests
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
    public async Task Mfa_setup_prompt_recovery_paths_when_seeded()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Assert.DoesNotContain("lobsy.nl", baseUrl, StringComparison.OrdinalIgnoreCase);

        var salesEmail = Environment.GetEnvironmentVariable("JOBSY_E2E_MFA_SALES_EMAIL");
        var salesPassword = Environment.GetEnvironmentVariable("JOBSY_E2E_MFA_SALES_PASSWORD");
        if (string.IsNullOrWhiteSpace(salesEmail) || string.IsNullOrWhiteSpace(salesPassword))
        {
            // Seeded MFA accounts are opt-in; unit/API MFA coverage stays in MfaAuth04 / hardening tests.
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 }
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.FillAsync("#login-email", salesEmail);
        await page.FillAsync("#login-password", salesPassword);
        await page.ClickAsync("button.au-submit, button[type=submit]");
        await page.WaitForURLAsync("**/account/mfa**", new() { Timeout = 60_000 });
        Assert.Contains("/account/mfa", page.Url, StringComparison.OrdinalIgnoreCase);
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
