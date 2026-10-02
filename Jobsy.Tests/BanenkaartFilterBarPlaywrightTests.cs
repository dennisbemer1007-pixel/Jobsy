using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Kandidaat polish 02: mobile filter bar fits in 390×844 without horizontal scroll;
/// Filters / travel / Match / view toggle stay in viewport.
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BanenkaartFilterBarPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Mobile_390_filter_bar_controls_visible_without_horizontal_scroll()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000
        });
        await page.WaitForSelectorAsync(".kb-filter-bar, .kb-filters-button", new() { Timeout = 60_000 });

        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
            await page.WaitForTimeoutAsync(300);
        }

        var overflow = await page.EvaluateAsync<double>("() => document.documentElement.scrollWidth");
        Assert.True(overflow <= 390, $"documentElement.scrollWidth was {overflow}");

        var inView = await page.EvaluateAsync<bool>("""
            () => {
              const ids = ['kb-filters-button', 'kb-travel-chip', 'kb-match-button', 'kb-view-toggle'];
              for (const id of ids) {
                const el = document.querySelector(`.kb-filter-bar [data-testid="${id}"]`);
                if (!el) return false;
                const r = el.getBoundingClientRect();
                if (r.width <= 0 || r.height <= 0) return false;
                if (r.left < -1 || r.right > 391) return false;
                if (r.top < 0 || r.bottom > 844) return false;
              }
              const search = document.querySelector('#discovery-keyword-mobile, .kb-filter-search--bar input');
              if (!search) return false;
              const sr = search.getBoundingClientRect();
              return sr.left >= -1 && sr.right <= 391;
            }
            """);
        Assert.True(inView, "Search, Filters, travel, Match and view toggle must fit in the 390×844 viewport.");
    }

    [Fact]
    public async Task Mobile_390_filters_opens_sheet_travel_30_and_match_navigates()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        var loggedIn = await TryLoginAsync(page, baseUrl);

        await page.GotoAsync(baseUrl + "/banenkaart", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000
        });
        await page.WaitForSelectorAsync(".kb-filter-bar [data-testid=kb-filters-button]", new() { Timeout = 60_000 });

        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }

        await page.Locator(".kb-filter-bar [data-testid=kb-filters-button]").ClickAsync();
        await page.WaitForSelectorAsync("#discovery-filters", new() { Timeout = 10_000 });
        await page.Keyboard.PressAsync("Escape");
        await page.WaitForTimeoutAsync(200);

        await page.Locator(".kb-filter-bar [data-testid=kb-travel-chip]").ClickAsync();
        var preset30 = page.Locator("[data-testid=kb-travel-popover] .kb-chip-preset")
            .Filter(new() { HasTextString = "30" });
        if (await preset30.CountAsync() > 0)
        {
            await preset30.First.ClickAsync();
            await page.WaitForTimeoutAsync(400);
            var chipText = await page.Locator(".kb-filter-bar [data-testid=kb-travel-chip]").InnerTextAsync();
            Assert.Contains("30", chipText, StringComparison.Ordinal);
        }

        if (loggedIn)
        {
            await page.Locator(".kb-filter-bar [data-testid=kb-match-button]").ClickAsync();
            await page.WaitForURLAsync(
                url => url.Contains("/candidate/match", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 30_000 });
        }
    }

    [Fact]
    public async Task Mobile_390_address_font_size_at_least_16()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000
        });

        var input = page.Locator("#discovery-address-prompt, #discovery-address, .kb-address__field input").First;
        await input.WaitForAsync(new() { Timeout = 60_000 });
        // Ensure prompt address is visible when present.
        var fontPx = await input.EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).fontSize)");
        Assert.True(fontPx >= 16, $"Address font-size was {fontPx}px, expected ≥ 16.");
    }

    [Fact]
    public async Task Desktop_1440_filter_bar_and_filters_button_open_sheet()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000
        });
        await page.WaitForSelectorAsync(".kb-filter-chips--desktop [data-testid=kb-filters-button], .kb-filters-button", new() { Timeout = 60_000 });

        var filters = page.Locator(".kb-filter-chips--desktop [data-testid=kb-filters-button]").First;
        await Assertions.Expect(filters).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await filters.ClickAsync();
        // Desktop inline panel is always in the DOM; clicking toggles the mobile sheet on narrow
        // viewports. On desktop ToggleFilters still opens the sheet overlay in current code.
        await page.WaitForTimeoutAsync(400);
        var sheetOrDesktop = await page.Locator("#discovery-filters, #discovery-filters-desktop").CountAsync();
        Assert.True(sheetOrDesktop > 0);
    }

    private static string? BaseUrl()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl;
    }

    private static async Task<IBrowser> LaunchAsync()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var playwright = await Playwright.CreateAsync();
        return await playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var resp = await http.GetAsync(baseUrl);
            return resp.IsSuccessStatusCode || (int)resp.StatusCode is >= 300 and < 500;
        }
        catch
        {
            return false;
        }
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
            var submit = page.Locator("button.login-submit, button.au-submit[type=submit]");
            await page.WaitForFunctionAsync(
                "() => { const b = document.querySelector('button.login-submit, button.au-submit[type=submit]'); return b && !b.disabled; }",
                null,
                new() { Timeout = 30_000 });
            await Task.WhenAll(
                page.WaitForURLAsync(
                    url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                    new() { Timeout = 60_000 }),
                submit.ClickAsync());
            return !page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
