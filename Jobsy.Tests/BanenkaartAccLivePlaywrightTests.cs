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
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
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
        }

        await page.WaitForFunctionAsync(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              if (!map || !map.isStyleLoaded || !map.isStyleLoaded()) return false;
              const src = map.getSource && map.getSource('jobsy-pins');
              return !!(src && src._data && src._data.features && src._data.features.length > 0);
            }
            """,
            null,
            new() { Timeout = 90_000 });

        var badConsole = consoleErrors.Where(e =>
            !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(badConsole.Count == 0, "Console errors: " + string.Join("; ", badConsole));
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
