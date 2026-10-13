using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Logged-in public pages keep the account menu closed and the header short.
/// Soft-skips without a running site.
/// </summary>
[Collection("PlaywrightSmoke")]
public class PublicAccountMenuPlaywrightTests
{
    [Theory]
    [InlineData(1366, 900)]
    [InlineData(390, 844)]
    public async Task Logged_in_public_page_keeps_account_menu_closed(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? "kandidaat@jobsy.local";
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? "Jobsy123!";
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
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

        await page.GotoAsync(baseUrl + "/hoe-werkt-lobsy", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });

        var result = await page.EvaluateAsync<string>(
            """
            () => {
              const header = document.querySelector('.pub-header');
              const panel = document.querySelector('.account-menu__panel');
              if (!header || !panel) return 'missing';
              const style = getComputedStyle(panel);
              const box = panel.getBoundingClientRect();
              const hidden = style.display === 'none' || box.height < 2;
              if (!hidden) return 'open';
              const h = header.getBoundingClientRect().height;
              if (h >= 90) return 'tall ' + Math.round(h);
              const cta = document.querySelector('.pub-header__cta, .pub-menu__cta');
              if (cta && getComputedStyle(cta).display !== 'none') return 'cta';
              return 'ok';
            }
            """);
        Assert.Equal("ok", result);
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
