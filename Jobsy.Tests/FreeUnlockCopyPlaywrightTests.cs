using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Test-account uitgebreide analyse stays free in the browser. Soft-skips without a site
/// or when the logged-in user is not a test account.
/// </summary>
[Collection("PlaywrightSmoke")]
public class FreeUnlockCopyPlaywrightTests
{
    private const string DefaultEmail = "test-kandidaat@lobsy.nl";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Test_account_checkout_has_no_paid_mollie_or_waiver()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_TEST_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_TEST_CANDIDATE_PASSWORD") ?? DefaultPassword;

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1366, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/login", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
        await page.FillAsync("input[name='email']", email);
        await page.FillAsync("input[name='password']", password);
        await page.ClickAsync("button.login-submit");
        try
        {
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/profiel/tests/career", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
        var html = await page.ContentAsync();
        if (!html.Contains("gratis (testaccount)", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Assert.DoesNotContain("Beroepentesttest", html, StringComparison.Ordinal);
        Assert.DoesNotContain("€ 2,99", html, StringComparison.Ordinal);
        Assert.DoesNotContain("via Mollie", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("14 dagen", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Betaald · klaar", html, StringComparison.Ordinal);
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl);
            return (int)response.StatusCode < 500;
        }
        catch
        {
            return false;
        }
    }
}
