using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Filter sheet Toepassen must be hittable above the bottom nav on mobile.
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class FilterSheetFooterPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(360, 800)]
    [InlineData(390, 844)]
    [InlineData(430, 932)]
    public async Task Mobile_filter_apply_is_not_covered_by_bottom_nav(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + E2eRoutes.Banenkaart, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await OpenFiltersAsync(page);

        var apply = page.Locator(".filter-sheet__apply").First;
        await apply.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

        // Footer must sit inside the viewport.
        var inViewport = await apply.EvaluateAsync<bool>("""
            el => {
              const r = el.getBoundingClientRect();
              return r.top >= 0 && r.bottom <= (window.innerHeight + 1) && r.height > 0;
            }
            """);
        Assert.True(inViewport, "filter-sheet__apply must stay inside the viewport.");

        var hitIsApply = await page.EvaluateAsync<bool>("""
            () => {
              const btn = document.querySelector('.filter-sheet__apply');
              if (!btn) return false;
              const r = btn.getBoundingClientRect();
              const x = r.left + r.width / 2;
              const y = r.top + r.height / 2;
              const el = document.elementFromPoint(x, y);
              if (!el) return false;
              if (el.closest && el.closest('.bottom-nav')) return false;
              return !!(el === btn || (el.closest && el.closest('.filter-sheet__apply')));
            }
            """);
        Assert.True(hitIsApply, "elementFromPoint at Toepassen centre must be the apply button, not .bottom-nav.");

        var urlBefore = page.Url;
        await apply.ClickAsync();
        await page.WaitForTimeoutAsync(500);

        Assert.DoesNotContain("/carriere", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            page.Url.TrimEnd('/').Equals(baseUrl, StringComparison.OrdinalIgnoreCase)
            || page.Url.Contains(baseUrl + E2eRoutes.Banenkaart, StringComparison.OrdinalIgnoreCase)
            || new Uri(page.Url).AbsolutePath is "/" or "" or "/banenkaart",
            $"URL should stay on home after Apply; was {urlBefore} → {page.Url}");

        await Assertions.Expect(page.Locator("#discovery-filters")).ToBeHiddenAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task Desktop_inline_filter_search_button_remains_visible()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
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
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + E2eRoutes.Banenkaart, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        var search = page.Locator(".filter-bar__search").First;
        await search.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        Assert.True(await search.IsVisibleAsync());
    }

    private static async Task OpenFiltersAsync(IPage page)
    {
        var filter = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Filter", RegexOptions.IgnoreCase) }).First;
        if (await filter.CountAsync() == 0)
        {
            filter = page.Locator("button:has-text('Filters'), .jobsy-action:has-text('Filters'), .discovery-filter-toggle").First;
        }

        await filter.ClickAsync();
        await page.Locator("#discovery-filters.filter-sheet, .filter-sheet").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            await page.ClickAsync("button.login-submit");
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl.TrimEnd('/') + E2eRoutes.Banenkaart);
            return (int)response.StatusCode is >= 200 and < 500;
        }
        catch
        {
            return false;
        }
    }
}
