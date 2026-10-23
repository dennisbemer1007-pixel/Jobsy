using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BanenkaartLoggedInCatalogPlaywrightTests : IAsyncLifetime
{
    private const string PublishDir = "/tmp/pub-banenkaart-logged-in-catalog";

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IllustratedPagesWebFactory? _web;
    private string _baseUrl = "";

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        await EnsurePublishedAsync();
        _web = new IllustratedPagesWebFactory();
        _baseUrl = _web.ServerAddress.TrimEnd('/');
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Theory]
    [InlineData(390, 844, true)]
    [InlineData(1280, 720, false)]
    public async Task Logged_in_after_paspoort_shows_map_pins_ring_and_toolbar(int width, int height, bool touch)
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var consoleErrors = new List<string>();
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        await page.GotoAsync(_baseUrl + "/__test/sign-in?role=candidate&return=/candidate/paspoort", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForTimeoutAsync(2500);

        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var allOk = await page.APIRequest.GetAsync(_baseUrl + "/api/banenkaart-client/all");
        Assert.True(allOk.Ok, "GET /all must return 200 for logged-in candidate.");

        var emptyTravelMsg = await page.Locator("[data-testid=kb-empty-travel]").CountAsync();
        Assert.True(emptyTravelMsg > 0, "Expected travel empty-state when list has zero in-range vacancies.");

        var mapHeight = await page.Locator("#job-map").EvaluateAsync<double>(
            "el => el.getBoundingClientRect().height");
        Assert.True(mapHeight >= 300, $"#job-map height {mapHeight}px should be at least 300px.");

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

        await page.WaitForFunctionAsync(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              if (!map || !map.isStyleLoaded || !map.isStyleLoaded()) return false;
              const src = map.getSource && map.getSource('jobsy-pins');
              if (src && src._data && src._data.features && src._data.features.length > 0) return true;
              if (!map.getLayer || !map.getLayer('jobsy-pins-unclustered')) return false;
              const pins = map.queryRenderedFeatures({ layers: ['jobsy-pins-unclustered', 'jobsy-pins-clusters'] });
              return pins && pins.length > 0;
            }
            """,
            null,
            new() { Timeout = 90_000 });

        var hasRing = await page.EvaluateAsync<bool>(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              return !!(map && map.getSource && map.getSource('jobsy-travel-rings'));
            }
            """);

        Assert.True(hasRing, "Expected travel ring source on map for logged-in origin.");

        var filterBtnWidth = await page.Locator("[data-testid=kb-filters-button]").First.EvaluateAsync<string>(
            "el => getComputedStyle(el).width");
        Assert.NotEqual("100%", filterBtnWidth.Trim());

        var authChromeCss = await page.EvaluateAsync<bool>(
            """
            () => {
              const links = Array.from(document.querySelectorAll('link[rel=stylesheet]'));
              const hasAuth = links.some(l => (l.href || '').indexOf('banenkaart-auth-chrome.css') !== -1);
              const hasAppMin = links.some(l => (l.href || '').indexOf('app.min.css') !== -1);
              return hasAuth && !hasAppMin;
            }
            """);
        Assert.True(authChromeCss, "Logged-in banenkaart should load scoped auth chrome CSS, not app.min.css.");

        Assert.True(consoleErrors.Count == 0, "Console errors: " + string.Join("; ", consoleErrors));
    }

    private static async Task EnsurePublishedAsync()
    {
        var dll = Path.Combine(PublishDir, "Jobsy.Web.dll");
        if (File.Exists(dll) && File.GetLastWriteTimeUtc(dll) > DateTime.UtcNow.AddHours(-2))
        {
            return;
        }

        if (Directory.Exists(PublishDir))
        {
            Directory.Delete(PublishDir, recursive: true);
        }

        var root = FindRepoRoot();
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            ArgumentList =
            {
                "publish", "Jobsy.Web/Jobsy.Web.csproj",
                "-c", "Release",
                "-o", PublishDir,
                "/p:UseAppHost=false",
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var pub = System.Diagnostics.Process.Start(psi)!;
        var stderr = await pub.StandardError.ReadToEndAsync();
        await pub.WaitForExitAsync();
        Assert.True(pub.ExitCode == 0, "publish failed: " + stderr);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
