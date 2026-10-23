using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Live acceptatie banenkaart layout/map checks (guest + logged-in candidate).
/// Set <c>JOBSY_E2E_BASE_URL</c> to override; defaults to https://acceptatie.lobsy.nl.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartAccLivePlaywrightTests
{
    private const string DefaultAccUrl = "https://acceptatie.lobsy.nl";
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(1280, 720, false, false)]
    [InlineData(390, 844, true, false)]
    [InlineData(1280, 720, false, true)]
    [InlineData(390, 844, true, true)]
    public async Task Banenkaart_on_acc_has_map_height_pins_and_clean_console(
        int width,
        int height,
        bool touch,
        bool loggedIn)
    {
        var baseUrl = ResolveBaseUrl();
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });

        var consoleErrors = new List<string>();
        var cspViolations = new List<string>();
        var pinRequests = new List<string>();
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }

            if (msg.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase)
                || msg.Text.Contains("script-src", StringComparison.OrdinalIgnoreCase)
                || msg.Text.Contains("script-src-attr", StringComparison.OrdinalIgnoreCase))
            {
                cspViolations.Add(msg.Text);
            }
        };
        page.Request += (_, req) =>
        {
            if (req.Url.Contains("/api/vacancies/pins", StringComparison.OrdinalIgnoreCase))
            {
                pinRequests.Add(req.Url);
            }
        };

        if (loggedIn)
        {
            var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
            var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
            if (!await KbE2e.TryLoginAsync(page, baseUrl, email, password)
                && !await KbE2e.TryLoginAsync(page, baseUrl))
            {
                Assert.Fail("Candidate login failed on acceptatie.");
            }

            await page.GotoAsync(baseUrl + "/candidate/paspoort", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
            await page.WaitForTimeoutAsync(1500);
        }

        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var mapHeight = await page.Locator("#job-map").EvaluateAsync<double>(
            "el => el.getBoundingClientRect().height");
        Assert.True(mapHeight >= 300, $"#job-map height {mapHeight}px should be at least 300px on acc.");

        var visibleToolbars = await page.EvaluateAsync<int>(
            """
            () => {
              const vis = (el) => {
                if (!el) return false;
                const s = getComputedStyle(el);
                return s.display !== 'none' && s.visibility !== 'hidden' && el.getClientRects().length > 0;
              };
              let n = 0;
              if (vis(document.querySelector('[data-testid=kb-panel-head-mobile]'))) n++;
              if (vis(document.querySelector('[data-testid=kb-panel-head]'))) n++;
              return n;
            }
            """);
        Assert.Equal(1, visibleToolbars);

        if (loggedIn)
        {
            var hasAuthChrome = await page.EvaluateAsync<bool>(
                """
                () => !Array.from(document.querySelectorAll('link[rel=stylesheet]'))
                  .some(l => (l.href || '').indexOf('app.min.css') !== -1)
                """);
            Assert.True(hasAuthChrome, "Logged-in NoBlazor banenkaart should not load full app.min.css.");

            var authChromeCss = await page.EvaluateAsync<bool>(
                """
                () => Array.from(document.querySelectorAll('link[rel=stylesheet]'))
                  .some(l => (l.href || '').indexOf('banenkaart-auth-chrome.css') !== -1)
                """);
            Assert.True(authChromeCss, "Logged-in banenkaart should load banenkaart-auth-chrome.css.");
        }

        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (root && root.dataset.banenkaartMapReady === '1') return true;
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              if (!map || !map.isStyleLoaded || !map.isStyleLoaded()) return false;
              const src = map.getSource && map.getSource('jobsy-pins');
              if (src && src._data && src._data.features && src._data.features.length > 0) return true;
              const canvas = document.querySelector('#job-map .maplibregl-canvas');
              return !!(canvas && canvas.width > 0 && canvas.height > 0);
            }
            """,
            null,
            new() { Timeout = 120_000 });

        Assert.True(pinRequests.Count == 0,
            "Catalog mode should not call /api/vacancies/pins. Saw: " + string.Join("; ", pinRequests));

        if (loggedIn && width >= 900)
        {
            await ApplyTravelFiltersAsync(page, 60, "Auto");
            await page.WaitForLoadStateAsync(LoadState.Load);

            var emptyTravel = await page.Locator("[data-testid=kb-empty-travel]").CountAsync();
            Assert.Equal(0, emptyTravel);

            var transportVal = await page.Locator("#kb-filter-transport").InputValueAsync();
            var maxMinutes = await page.Locator("[data-testid=kb-sheet-travel-slider]").InputValueAsync();
            Assert.Equal("Auto", transportVal);
            Assert.Equal("60", maxMinutes);

            await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await page.WaitForTimeoutAsync(800);

            transportVal = await page.Locator("#kb-filter-transport").InputValueAsync();
            maxMinutes = await page.Locator("[data-testid=kb-sheet-travel-slider]").InputValueAsync();
            Assert.Equal("Auto", transportVal);
            Assert.Equal("60", maxMinutes);

            await OpenFilterDrawerAsync(page);
            var footerVisible = await page.EvaluateAsync<bool>(
                """
                () => {
                  const btn = document.querySelector('[data-testid=kb-filter-apply]');
                  if (!btn) return false;
                  const r = btn.getBoundingClientRect();
                  const sheet = document.querySelector('[data-testid=kb-filter-sheet]');
                  const sr = sheet ? sheet.getBoundingClientRect() : null;
                  if (!sr) return false;
                  return r.top >= sr.top && r.bottom <= sr.bottom + 1;
                }
                """);
            Assert.True(footerVisible, "Filter drawer apply button should stay inside the visible drawer (sticky footer).");
        }

        var badConsole = consoleErrors.Where(e =>
            !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(cspViolations.Count == 0,
            "CSP violations: " + string.Join("; ", cspViolations));
        Assert.True(badConsole.Count == 0, "Console errors: " + string.Join("; ", badConsole));
    }

    [Fact]
    public async Task Onboarding_step_10_has_no_script_src_attr_violation_on_acc()
    {
        var baseUrl = ResolveBaseUrl();
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var cspViolations = new List<string>();
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Text.Contains("script-src-attr", StringComparison.OrdinalIgnoreCase)
                || msg.Text.Contains("inline event handler", StringComparison.OrdinalIgnoreCase))
            {
                cspViolations.Add(msg.Text);
            }
        };

        await page.GotoAsync(baseUrl + "/candidate/start?step=10", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForTimeoutAsync(1500);

        Assert.True(cspViolations.Count == 0,
            "Onboarding step 10 CSP script-src-attr: " + string.Join("; ", cspViolations));
    }

    private static async Task OpenFilterDrawerAsync(IPage page)
    {
        var btn = page.Locator("[data-testid=kb-filters-button]").First;
        await btn.ClickAsync();
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root && root.classList.contains('filters-open');
            }
            """);
    }

    private static async Task ApplyTravelFiltersAsync(IPage page, int minutes, string transport)
    {
        await OpenFilterDrawerAsync(page);
        await page.Locator($"[data-testid=kb-sheet-travel-{minutes}]").ClickAsync();
        await page.Locator($"[data-testid=kb-sheet-transport-{transport}]").ClickAsync();
        await page.Locator("[data-testid=kb-filter-apply]").ClickAsync();
        await page.WaitForURLAsync(url => url.Contains("/banenkaart"), new() { Timeout = 60_000 });
    }

    private static string ResolveBaseUrl()
    {
        var fromEnv = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(fromEnv) ? DefaultAccUrl : fromEnv;
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            using var res = await http.GetAsync(baseUrl + "/health");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
