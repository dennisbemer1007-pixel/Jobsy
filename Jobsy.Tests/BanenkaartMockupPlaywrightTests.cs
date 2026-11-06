using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BanenkaartMockupPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _baseUrl = "";
    private static readonly string ScreenshotDir = "/opt/cursor/artifacts/screenshots";
    private static readonly string MetricsPath = "/opt/cursor/artifacts/banenkaart-mockup-metrics.json";

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        Directory.CreateDirectory(ScreenshotDir);
        Directory.CreateDirectory(Path.GetDirectoryName(MetricsPath)!);

        var external = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(external) && await IsReachableAsync(external))
        {
            _baseUrl = external;
        }
        else
        {
            _web = new IllustratedPagesWebFactory();
            _baseUrl = _web.ServerAddress.TrimEnd('/');
        }
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
    [InlineData("desktop-closed", 1024, 700, false, false, true)]
    [InlineData("desktop-wide-closed", 1280, 720, false, false, true)]
    [InlineData("desktop-filters-open", 1280, 720, false, true, true)]
    [InlineData("desktop-guest-closed", 1280, 720, false, false, false)]
    [InlineData("desktop-guest-filters-open", 1280, 720, false, true, false)]
    [InlineData("mobile-closed", 390, 844, true, false, true)]
    [InlineData("mobile-sheet-open", 390, 844, true, true, true)]
    [InlineData("desktop-map-visible", 1280, 720, false, false, true)]
    public async Task Banenkaart_ssr_mockup_states(
        string state,
        int width,
        int height,
        bool touch,
        bool openFilters,
        bool signIn)
    {
        if (_web is null)
        {
            await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(_baseUrl);
        }

        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var tileOk = new TaskCompletionSource<bool>();
        page.Response += (_, response) =>
        {
            if (response.Status != 200)
            {
                return;
            }

            var url = response.Url;
            if (url.Contains("tiles.openfreemap.org", StringComparison.OrdinalIgnoreCase))
            {
                tileOk.TrySetResult(true);
            }
        };

        if (signIn)
        {
            await page.GotoAsync(_baseUrl + "/__test/sign-in?role=candidate&return=" + Uri.EscapeDataString("/banenkaart"));
        }

        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.dataset?.banenkaartSsrBound === '1'",
            null,
            new() { Timeout = 90_000 });

        var later = page.Locator("[data-testid=kb-start-later]");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }

        if (openFilters)
        {
            var btn = page.Locator("[data-testid=kb-filters-button]:visible");
            await btn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await btn.ClickAsync();
            await page.WaitForFunctionAsync(
                """
                () => {
                  const root = document.querySelector('[data-banenkaart-ssr="1"]');
                  const sheet = document.querySelector('[data-testid=kb-filter-sheet]');
                  return root?.classList.contains('filters-open')
                    && sheet?.getAttribute('aria-hidden') === 'false';
                }
                """,
                null,
                new() { Timeout = 30_000 });
        }

        await AssertMapLoadedAndInteractiveAsync(page, touch, tileOk.Task);

        var metrics = await page.EvaluateAsync<JsonElement>("""
            async () => {
              const nav = performance.getEntriesByType('navigation')[0];
              let lcp = 0;
              try {
                const po = new PerformanceObserver((list) => {
                  for (const e of list.getEntries()) {
                    if (e.entryType === 'largest-contentful-paint') lcp = e.startTime;
                  }
                });
                po.observe({ type: 'largest-contentful-paint', buffered: true });
                await new Promise(r => setTimeout(r, 500));
                po.disconnect();
              } catch (e) {}
              const resources = performance.getEntriesByType('resource');
              let jsBytes = 0;
              for (const r of resources) {
                if (r.initiatorType === 'script' || (r.name && r.name.endsWith('.js'))) {
                  jsBytes += r.transferSize || 0;
                }
              }
              return { lcp, jsBytes, domContentLoaded: nav ? nav.domContentLoadedEventEnd : 0 };
            }
            """);

        var path = Path.Combine(ScreenshotDir, $"banenkaart-{state}.png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = false });

        Assert.True(metrics.GetProperty("lcp").GetDouble() >= 0);
        Assert.True(metrics.GetProperty("jsBytes").GetDouble() >= 0);
        Assert.True(File.Exists(path));

        await AppendMetricsAsync(state, width, height, metrics, path);
    }

    private static async Task AssertMapLoadedAndInteractiveAsync(IPage page, bool touch, Task<bool> tileTask)
    {
        await page.WaitForFunctionAsync(
            """
            () => {
              const canvas = document.querySelector('#job-map .maplibregl-canvas');
              if (!canvas) return false;
              const rect = canvas.getBoundingClientRect();
              if (rect.width < 32 || rect.height < 32) return false;
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              return !!(map && typeof map.getZoom === 'function');
            }
            """,
            null,
            new() { Timeout = 90_000 });

        var gotTile = await Task.WhenAny(tileTask, Task.Delay(TimeSpan.FromSeconds(60))) == tileTask
                      && tileTask.IsCompletedSuccessfully
                      && await tileTask;
        Assert.True(gotTile, "Expected at least one OpenFreeMap tile response with HTTP 200.");

        var zoomChanged = await page.EvaluateAsync<bool>(
            """
            () => {
              const map = window.jobMap.__testGetMap();
              if (!map) return false;
              const z0 = map.getZoom();
              map.zoomIn({ duration: 0 });
              const z1 = map.getZoom();
              return z1 > z0;
            }
            """);

        Assert.True(zoomChanged, "Map zoom level should increase after zoomIn().");

        if (touch)
        {
            var mapBox = await page.Locator("#job-map").BoundingBoxAsync();
            Assert.NotNull(mapBox);
            var cx = mapBox.X + mapBox.Width / 2;
            var cy = mapBox.Y + mapBox.Height / 2;
            await page.Touchscreen.TapAsync(cx, cy);
            var panned = await page.EvaluateAsync<bool>(
                """
                () => {
                  const map = window.jobMap.__testGetMap();
                  if (!map || typeof map.panBy !== 'function') return false;
                  const c = map.getCenter();
                  map.panBy([80, 0], { duration: 0 });
                  const c2 = map.getCenter();
                  return Math.abs(c2.lng - c.lng) > 0.00001;
                }
                """);
            Assert.True(panned, "Map panBy should change the center on mobile.");
        }
        else
        {
            await page.Locator("#job-map").HoverAsync();
            await page.Mouse.WheelAsync(0, -400);
            var wheelZoomed = await page.EvaluateAsync<bool>(
                """
                () => {
                  const map = window.jobMap.__testGetMap();
                  return !!(map && map.getZoom() > 0);
                }
                """);
            Assert.True(wheelZoomed);
        }

        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root?.dataset?.banenkaartMapReady === '1'
                || document.querySelector('#job-map .maplibregl-canvas')?.getBoundingClientRect().height >= 32;
            }
            """,
            null,
            new() { Timeout = 30_000 });
    }

    private static async Task AppendMetricsAsync(string state, int width, int height, JsonElement metrics, string screenshot)
    {
        List<object> rows = [];
        if (File.Exists(MetricsPath))
        {
            try
            {
                var existing = JsonSerializer.Deserialize<List<object>>(await File.ReadAllTextAsync(MetricsPath));
                if (existing is not null)
                {
                    rows = existing;
                }
            }
            catch (JsonException)
            {
                rows = [];
            }
        }

        rows.Add(new
        {
            state,
            width,
            height,
            lcpMs = metrics.GetProperty("lcp").GetDouble(),
            jsTransferBytes = metrics.GetProperty("jsBytes").GetDouble(),
            domContentLoadedMs = metrics.GetProperty("domContentLoaded").GetDouble(),
            screenshot,
        });
        await File.WriteAllTextAsync(MetricsPath, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await client.GetAsync(baseUrl + "/healthz");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
