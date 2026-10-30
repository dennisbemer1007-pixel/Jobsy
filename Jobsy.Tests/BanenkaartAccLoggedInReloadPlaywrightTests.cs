using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Repro for logged-in candidate banenkaart broken after hard reload on acceptatie (ec45303).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartAccLoggedInReloadPlaywrightTests
{
    private const string DefaultAccUrl = "https://acceptatie.lobsy.nl";
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";
    private static readonly string ArtifactDir = "/opt/cursor/artifacts/banenkaart-acc-logged-in-reload";

    [Theory]
    [InlineData(1280, 720, false)]
    [InlineData(390, 844, true)]
    public async Task Logged_in_hard_reload_restores_map_layout_and_chrome(int width, int height, bool touch)
    {
        var baseUrl = Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? DefaultAccUrl;
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        Directory.CreateDirectory(ArtifactDir);
        var tag = $"{width}x{height}";

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
        var notFound = new List<string>();
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }

            if (msg.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase))
            {
                cspViolations.Add(msg.Text);
            }
        };
        page.Response += (_, resp) =>
        {
            if (resp.Status == 404)
            {
                notFound.Add(resp.Url);
            }
        };

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        if (!await KbE2e.TryLoginAsync(page, baseUrl, email, password)
            && !await KbE2e.TryLoginAsync(page, baseUrl))
        {
            Assert.Fail("Candidate login failed on acceptatie.");
        }

        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        await page.EvaluateAsync(
            """
            () => {
              sessionStorage.setItem('jobsy.bk.ui.v1', JSON.stringify({ filtersOpen: true, showMap: false, desktopList: true }));
              sessionStorage.setItem('jobsy.bk.ui.v2', JSON.stringify({ filtersOpen: true, showMap: false, desktopList: true }));
            }
            """);

        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForTimeoutAsync(2500);

        var diag = await page.EvaluateAsync<ReloadDiag>(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              const shell = document.querySelector('.app-shell');
              const nav = document.querySelector('[data-testid=candidate-bottom-nav-ssr]');
              const styles = Array.from(document.querySelectorAll('link[rel=stylesheet]')).map(l => l.href);
              const row = document.querySelector('[data-testid=kb-list-row]');
              const rowStyle = row ? getComputedStyle(row) : null;
              const map = document.querySelector('#job-map');
              const canvas = document.querySelector('#job-map canvas, #job-map .maplibregl-canvas');
              const fab = document.querySelector('[data-testid=kb-map-fab]');
              return {
                shellBanenkaart: shell ? shell.classList.contains('app-shell--banenkaart-shell') : false,
                hasBottomNavClass: shell ? shell.classList.contains('has-bottom-nav') : false,
                showMap: root ? root.classList.contains('show-map') : false,
                listMode: root ? root.classList.contains('jobsy-discovery--list') : false,
                mapReady: root ? root.dataset.banenkaartMapReady : null,
                hasJobMap: !!window.jobMap,
                authChrome: styles.some(h => h.indexOf('banenkaart-auth-chrome.css') !== -1),
                noBlazorChrome: styles.some(h => h.indexOf('candidate-no-blazor-chrome.css') !== -1),
                banenkaartPageCss: styles.some(h => h.indexOf('banenkaart-page.css') !== -1),
                navWidth: nav ? nav.getBoundingClientRect().width : 0,
                viewportWidth: window.innerWidth,
                mapHeight: map ? map.getBoundingClientRect().height : 0,
                canvasHeight: canvas ? canvas.getBoundingClientRect().height : 0,
                fabVisible: fab ? (getComputedStyle(fab).display !== 'none' && fab.getClientRects().length > 0) : false,
                rowDisplay: rowStyle ? rowStyle.display : null,
                listRowCount: document.querySelectorAll('[data-testid=kb-list-row]').length,
                kbListRowsWrappers: document.querySelectorAll('[data-testid=kb-list-rows]').length,
              };
            }
            """);

        await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactDir, $"reload-{tag}.png") });
        await File.WriteAllTextAsync(
            Path.Combine(ArtifactDir, $"reload-{tag}.json"),
            System.Text.Json.JsonSerializer.Serialize(new { diag, consoleErrors, cspViolations, notFound }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        Assert.True(diag.ShellBanenkaart, "Logged-in banenkaart needs app-shell--banenkaart-shell.");
        Assert.True(diag.AuthChrome, "Missing banenkaart-auth-chrome.css.");
        Assert.True(diag.NoBlazorChrome, "Missing candidate-no-blazor-chrome.css.");
        Assert.True(diag.BanenkaartPageCss, "Missing banenkaart-page.css.");
        Assert.True(diag.ShowMap, "Hard reload must restore show-map (legacy session had list-only mode).");
        Assert.False(diag.ListMode, "Hard reload must not stay in desktop list-only mode.");
        Assert.Empty(cspViolations);
        Assert.True(diag.ListRowCount >= 1, "Expected at least one list row.");
        Assert.Equal(1, diag.KbListRowsWrappers);

        if (touch)
        {
            Assert.True(diag.FabVisible, "Mobile map FAB should show in map mode after reload.");
        }
        else
        {
            Assert.True(diag.NavWidth >= diag.ViewportWidth * 0.9,
                $"Bottom nav should span viewport (nav={diag.NavWidth}, vw={diag.ViewportWidth}).");
            Assert.True(diag.MapHeight > 320 || diag.CanvasHeight > 320,
                $"Desktop map should be visible after reload (map={diag.MapHeight}, canvas={diag.CanvasHeight}).");
        }

        var bad404 = notFound.Where(u =>
            u.Contains("/css/features/banenkaart", StringComparison.OrdinalIgnoreCase)
            || u.Contains("/js/banenkaart", StringComparison.OrdinalIgnoreCase)
            || u.Contains("/js/jobMap", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(bad404.Count == 0, "404 on banenkaart assets: " + string.Join("; ", bad404));

        var badConsole = consoleErrors.Where(e =>
            !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(badConsole.Count == 0, "Console errors: " + string.Join("; ", badConsole));
    }

    private sealed class ReloadDiag
    {
        public bool ShellBanenkaart { get; set; }
        public bool HasBottomNavClass { get; set; }
        public bool ShowMap { get; set; }
        public bool ListMode { get; set; }
        public string? MapReady { get; set; }
        public bool HasJobMap { get; set; }
        public bool AuthChrome { get; set; }
        public bool NoBlazorChrome { get; set; }
        public bool BanenkaartPageCss { get; set; }
        public double NavWidth { get; set; }
        public double ViewportWidth { get; set; }
        public double MapHeight { get; set; }
        public double CanvasHeight { get; set; }
        public bool FabVisible { get; set; }
        public string? RowDisplay { get; set; }
        public int ListRowCount { get; set; }
        public int KbListRowsWrappers { get; set; }
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
