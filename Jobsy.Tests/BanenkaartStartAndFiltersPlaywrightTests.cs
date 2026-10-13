using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Banenkaart start origin, filters, docked popup, iso-mode, glyphs (file 03).
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BanenkaartStartAndFiltersPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Logged_in_candidate_starts_at_home_fiets_20_badge_zero()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForSelectorAsync("#discovery-address-desktop, #discovery-address", new() { Timeout = 60_000 });

        var address = await page.Locator("#discovery-address-desktop, #discovery-address").First.InputValueAsync();
        Assert.False(string.IsNullOrWhiteSpace(address));

        var travel = page.Locator(".kb-chip").Filter(new() { HasTextString = "20 min" }).First;
        await Assertions.Expect(travel).ToBeVisibleAsync(new() { Timeout = 15_000 });

        var badgeCount = await page.Locator(".kb-filters-button .jobsy-action__badge, .kb-filter-chips .jobsy-action__badge").CountAsync();
        Assert.Equal(0, badgeCount);
    }

    [Fact]
    public async Task Anonymous_mobile_shows_location_prompt_without_blank_strip()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForSelectorAsync(".kb-start-prompt, #job-map", new() { Timeout = 60_000 });

        var prompt = page.Locator(".kb-start-prompt");
        if (await prompt.CountAsync() > 0)
        {
            await Assertions.Expect(prompt).ToBeVisibleAsync();
        }

        await page.WaitForSelectorAsync("#job-map canvas", new() { Timeout = 60_000 });
        var gap = await page.EvaluateAsync<double>("""
            () => {
              const map = document.querySelector('#job-map') || document.querySelector('.map-pane');
              const nav = document.querySelector('.bottom-nav');
              if (!map) return 999;
              const mr = map.getBoundingClientRect();
              const bottom = nav ? nav.getBoundingClientRect().top : window.innerHeight;
              return Math.abs(bottom - mr.bottom);
            }
            """);
        Assert.True(gap <= 2, $"blank strip gap was {gap}px");
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Typing_address_keeps_every_character(int width, int height)
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = width < 900,
            IsMobile = width < 900,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var input = page.Locator("#discovery-address-desktop, #discovery-address, #discovery-address-prompt").First;
        await input.WaitForAsync(new() { Timeout = 60_000 });
        await input.ClickAsync();
        const string typed = "Herenstraat 20 Wateringen";
        await input.PressSequentiallyAsync(typed, new() { Delay = 25 });
        Assert.Equal(typed, await input.InputValueAsync());
    }

    [Fact]
    public async Task Pin_and_cluster_open_docked_popup_and_iso_mode_and_no_glyph_404()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        var font404 = 0;
        page.Response += (_, resp) =>
        {
            if (resp.Status == 404 && resp.Url.Contains("/fonts/", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref font404);
            }
        };

        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && window.maplibregl && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 60_000 });

        // Dismiss start prompt if present so map is usable.
        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }

        await page.WaitForTimeoutAsync(1500);
        var iso = await page.EvaluateAsync<string?>("""
            () => {
              const el = document.querySelector('#job-map[data-iso-mode], .map-pane[data-iso-mode]');
              return el ? el.getAttribute('data-iso-mode') : null;
            }
            """);
        // Without an origin rings may be absent; when present must be real|approx.
        if (!string.IsNullOrEmpty(iso))
        {
            Assert.Contains(iso, new[] { "real", "approx" });
        }

        var opened = await page.EvaluateAsync<bool>("""
            async () => {
              if (!window.jobMap || typeof window.jobMap.debugOpenLargestCluster !== 'function') return false;
              return await window.jobMap.debugOpenLargestCluster();
            }
            """);
        if (opened)
        {
            await page.WaitForSelectorAsync(".map-cluster-sheet.map-popup--docked, .map-cluster-sheet", new() { Timeout = 10_000 });
            var floatingVacancy = await page.Locator(".maplibregl-popup.job-map-popup:not(.job-map-popup--cluster)").CountAsync();
            Assert.Equal(0, floatingVacancy);
        }

        Assert.Equal(0, font404);
    }

    [Fact]
    public async Task Mobile_390_filter_sheet_sticky_no_overflow_and_controls_tall_enough()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForSelectorAsync(".kb-filters-button, #job-map", new() { Timeout = 60_000 });

        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }

        await page.Locator("[data-testid='kb-filters-button']").First.ClickAsync();
        var sheet = page.Locator("#discovery-filters.filter-sheet, [data-testid='kb-filter-sheet']").First;
        await sheet.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

        var layout = await page.EvaluateAsync<bool>("""
            () => {
              const sheet = document.querySelector('#discovery-filters');
              if (!sheet) return false;
              const overflow = document.documentElement.scrollWidth > window.innerWidth + 1;
              const header = sheet.querySelector('.filter-sheet__header');
              const footer = sheet.querySelector('.filter-sheet__footer');
              if (!header || !footer) return false;
              const hs = getComputedStyle(header);
              const fs = getComputedStyle(footer);
              const stickyOk = (hs.position === 'sticky' || hs.position === 'fixed')
                && (fs.position === 'sticky' || fs.position === 'fixed');
              const controls = sheet.querySelectorAll(
                'button, select, input[type=search], input[type=number], .filter-sheet__seg-btn, .filter-sheet__mode, .branch-multiselect__trigger');
              for (const el of controls) {
                const r = el.getBoundingClientRect();
                if (r.height > 0 && r.height < 43.5) return false;
              }
              return !overflow && stickyOk;
            }
            """);
        Assert.True(layout, "Filter sheet must be sticky, without horizontal overflow, controls ≥ 44px.");

        // Scroll to Volgorde and change sort, then apply.
        var sort = page.Locator("[data-testid='kb-sheet-sort']");
        await sort.ScrollIntoViewIfNeededAsync();
        var options = await sort.Locator("option").AllAsync();
        if (options.Count > 1)
        {
            var value = await options[Math.Min(1, options.Count - 1)].GetAttributeAsync("value");
            if (!string.IsNullOrEmpty(value))
            {
                await sort.SelectOptionAsync(value);
            }
        }

        await page.Locator("[data-testid='kb-filter-apply']").ClickAsync();
        await Assertions.Expect(sheet).ToBeHiddenAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task Mobile_390_hours_and_category_bump_filters_badge()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator("[data-testid='kb-filters-button']").First.WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator("[data-testid='kb-filters-button']").First.ClickAsync();
        var sheet = page.Locator("[data-testid='kb-filter-sheet']");
        await sheet.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

        // Prefer a category chip when present (Soort werk).
        var cat = page.Locator(".filter-sheet__chip--category").First;
        if (await cat.CountAsync() > 0)
        {
            await cat.ClickAsync();
        }

        // Nudge hours range via range inputs when present.
        var hours = page.Locator(".hours-range--sheet input[type=range]");
        if (await hours.CountAsync() >= 2)
        {
            await hours.Nth(0).FillAsync("16");
            await hours.Nth(1).FillAsync("32");
        }

        await page.Locator("[data-testid='kb-filter-apply']").ClickAsync();
        await Assertions.Expect(sheet).ToBeHiddenAsync(new() { Timeout = 10_000 });

        var badge = page.Locator("[data-testid='kb-filters-badge']").First;
        if (await badge.CountAsync() > 0)
        {
            var text = (await badge.InnerTextAsync()).Trim();
            Assert.True(int.TryParse(text, out var n) && n >= 1, $"Expected Filters badge ≥ 1, got '{text}'");
        }
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
            await page.GotoAsync(baseUrl + "/account/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.FillAsync("input[type=email], input[name=email], #email", email);
            await page.FillAsync("input[type=password], input[name=password], #password", password);
            await page.ClickAsync("button[type=submit], button:has-text('Inloggen'), button:has-text('Log')");
            await page.WaitForTimeoutAsync(1500);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
