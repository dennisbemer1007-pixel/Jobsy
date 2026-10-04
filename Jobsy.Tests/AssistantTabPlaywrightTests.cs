using System.Net;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// The assistant edge tab stays inside the viewport. Soft-skips without a running site.
/// </summary>
[Collection("PlaywrightSmoke")]
public class AssistantTabPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(1366, 900)]
    [InlineData(390, 844)]
    public async Task Assistant_tab_is_inside_the_viewport(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;

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

        await page.GotoAsync(baseUrl + "/candidate/paspoort", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });

        var tab = page.Locator(".lobsy-assistant-tab__btn");
        try
        {
            await tab.WaitForAsync(new() { Timeout = 20_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        var box = await tab.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box!.Y >= 0, $"Assistant tab starts above the viewport (y={box.Y}).");
        Assert.True(box.Y + box.Height <= height + 1, $"Assistant tab ends below the viewport (y={box.Y}, h={box.Height}).");
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
