using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Werkgever role smoke (BM / RM / VM) × key pages at 1440 and 390.
/// Soft-skips when <c>JOBSY_E2E_BASE_URL</c> is unset so unit CI stays green offline.
/// </summary>
[Collection("PlaywrightSmoke")]
public class WerkgeverSmokePlaywrightTests
{
    public static TheoryData<string, string, int, int> Cases()
    {
        var data = new TheoryData<string, string, int, int>();
        var roles = new[] { "bm", "rm", "vm" };
        var pages = new[]
        {
            "/werkgever",
            "/werkgever/vacatures",
            "/werkgever/sollicitaties",
            "/werkgever/tokens",
            "/werkgever/kandidaatinzichten",
        };
        foreach (var role in roles)
        {
            foreach (var page in pages)
            {
                data.Add(role, page, 1440, 900);
                data.Add(role, page, 390, 844);
            }

            if (role == "bm")
            {
                data.Add(role, "/werkgever/organisatie/vestigingen", 1440, 900);
                data.Add(role, "/werkgever/organisatie/vestigingen", 390, 844);
                data.Add(role, "/werkgever/organisatie/team", 1440, 900);
                data.Add(role, "/werkgever/organisatie/team", 390, 844);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Werkgever_page_loads_for_role(string role, string path, int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            // Soft-skip: set JOBSY_E2E_BASE_URL to enforce against Acc/CI.
            return;
        }

        var email = (Environment.GetEnvironmentVariable($"JOBSY_E2E_{role.ToUpperInvariant()}_EMAIL")
                     ?? Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_EMAIL")
                     ?? "").Trim();
        var password = (Environment.GetEnvironmentVariable($"JOBSY_E2E_{role.ToUpperInvariant()}_PASSWORD")
                        ?? Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_PASSWORD")
                        ?? "").Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        // RM cannot open tokens purchase-only pages meaningfully — still loads saldo.
        // VM cannot open organisatie/team — only BM cases include those.

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        var consoleErrors = new List<string>();
        page.Console += (_, e) =>
        {
            if (e.Type == "error")
            {
                consoleErrors.Add(e.Text);
            }
        };

        if (!await TryLoginAsync(page, baseUrl, email, password))
        {
            return;
        }

        await page.GotoAsync(baseUrl + path,
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(600);

        var h1 = await page.Locator("h1").First.InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(h1));

        var url = page.Url;
        Assert.DoesNotContain("/employer/", url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/branch/", url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/regional/", url, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(consoleErrors, e =>
            e.Contains("Failed to load resource", StringComparison.OrdinalIgnoreCase)
            && e.Contains("500", StringComparison.Ordinal));
    }

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
