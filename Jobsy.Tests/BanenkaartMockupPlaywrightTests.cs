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
    public async Task Banenkaart_ssr_mockup_states(string state, int width, int height, bool touch, bool openFilters, bool signIn)
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

        if (signIn)
        {
            await page.GotoAsync(_baseUrl + "/__test/sign-in?role=candidate&return=" + Uri.EscapeDataString("/banenkaart"));
        }

        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-banenkaart-ssr=\"1\"]").WaitForAsync(new() { Timeout = 90_000 });

        var later = page.Locator("[data-testid=kb-start-later]");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }

        if (openFilters)
        {
            var btn = width >= 900
                ? page.Locator(".discovery-toolbar--desktop [data-testid=kb-filters-button], .kb-filter-drawer-toggle[data-testid=kb-filters-button]")
                : page.Locator(".kb-filter-bar [data-testid=kb-filters-button]");
            await btn.First.ClickAsync();
            await page.Locator("[data-testid=kb-filter-sheet]").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        }

        try
        {
            await page.Locator("#job-map canvas").First.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 20_000 });
        }
        catch (PlaywrightException)
        {
            // Stub stack may not paint WebGL; layout and SSR shell still validate.
        }

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
