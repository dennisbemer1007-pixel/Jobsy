using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Employers OFF (decision 20): anonymous employer routes show the binnenkort page,
/// and the candidate journey has no employer links. Soft-skips without JOBSY_E2E_BASE_URL.
/// </summary>
[Collection("PlaywrightSmoke")]
public class WerkgeversUitPlaywrightTests
{
    public static TheoryData<int, int> Viewports() => new()
    {
        { 390, 844 },
        { 1440, 900 }
    };

    [Theory]
    [MemberData(nameof(Viewports))]
    public async Task Anonymous_employer_routes_show_binnenkort(int width, int height)
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null)
        {
            return;
        }

        if (!await EmployersPlaywrightGuard.EmployersAreOffAsync(baseUrl))
        {
            Assert.Skip("Employers are ON; binnenkort coverage needs the decision-20 default.");
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        foreach (var path in new[] { "/employer/talent", "/branch/applicants", "/werkgever/sollicitaties", "/register/bedrijf", "/sales" })
        {
            await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.WaitForTimeoutAsync(400);
            Assert.Contains("/werkgevers/binnenkort", page.Url, StringComparison.OrdinalIgnoreCase);
            var html = await page.ContentAsync();
            Assert.Contains("Voor werkgevers: binnenkort", html, StringComparison.Ordinal);
            Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
            await AssertNoOverflowAsync(page);
        }
    }

    [Theory]
    [MemberData(nameof(Viewports))]
    public async Task Candidate_journey_has_no_employer_links(int width, int height)
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null)
        {
            return;
        }

        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? "").Trim();
        var password = (Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? "").Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (!await EmployersPlaywrightGuard.EmployersAreOffAsync(baseUrl))
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        var consoleErrors = new List<string>();
        page.Console += (_, e) =>
        {
            if (e.Type == "error")
            {
                consoleErrors.Add(e.Text);
            }
        };

        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await page.FillAsync("input[type=email], input[name=email]", email);
        await page.FillAsync("input[type=password], input[name=password]", password);
        await page.Locator("button[type=submit]").First.ClickAsync();
        await page.WaitForTimeoutAsync(1500);

        foreach (var path in new[] { "/candidate/paspoort", "/carriere" })
        {
            await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.WaitForTimeoutAsync(500);
            var hrefs = await page.EvalOnSelectorAllAsync<string[]>(
                "a[href]",
                "els => els.map(e => e.getAttribute('href') || '')");
            Assert.DoesNotContain(hrefs, h =>
                h.Contains("/employer", StringComparison.OrdinalIgnoreCase)
                || h.Contains("/werkgever", StringComparison.OrdinalIgnoreCase)
                || h.Contains("/branch", StringComparison.OrdinalIgnoreCase)
                || h.StartsWith("/sales", StringComparison.OrdinalIgnoreCase));
            await AssertNoOverflowAsync(page);
        }

        Assert.DoesNotContain(consoleErrors, e =>
            e.Contains("feature_disabled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Arabic_binnenkort_is_rtl()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null)
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/werkgevers/binnenkort?lang=ar", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        var dir = await page.EvalOnSelectorAsync<string>("html", "el => el.getAttribute('dir') || el.dir || ''");
        Assert.Equal("rtl", dir);
        await AssertNoOverflowAsync(page);
    }

    private static string? BaseUrl()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl;
    }

    private static async Task AssertNoOverflowAsync(IPage page)
    {
        var overflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        Assert.False(overflow);
    }
}
