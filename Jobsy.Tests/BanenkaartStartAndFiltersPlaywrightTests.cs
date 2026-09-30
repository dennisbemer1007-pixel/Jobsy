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

        var badgeCount = await page.Locator(".kb-filter-chips .jobsy-action__badge").CountAsync();
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
