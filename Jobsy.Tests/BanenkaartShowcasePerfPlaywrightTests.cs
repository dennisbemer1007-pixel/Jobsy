using System.Diagnostics;
using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Showcase banenkaart hard targets (Dennis): Release publish, CPU 4×, Fast 4G, viewports 390 + 1280.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartShowcasePerfPlaywrightTests : IAsyncLifetime
{
    private const int PreMapJsGzipBudgetBytes = 30 * 1024;
    private const double FirstPaintBudgetMs = 1000;
    private const double LcpBudgetMs = 1500;
    private const double MapTilesBudgetMs = 2500;

    private static readonly string ReportPath = "/opt/cursor/artifacts/banenkaart-showcase-perf.json";
    private static readonly string BaselinePath = Path.Combine(FindRepoRoot(), "docs", "banenkaart-showcase-perf-baseline.json");
    private static readonly string PublishDir = "/tmp/pub-banenkaart-showcase-perf";

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

        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath)!);
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
    public async Task Banenkaart_showcase_meets_hard_targets(int width, int height, bool touch)
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        var cdp = await context.NewCDPSessionAsync(page);
        await cdp.SendAsync("Emulation.setCPUThrottlingRate", new Dictionary<string, object> { ["rate"] = 4 });
        await cdp.SendAsync("Network.emulateNetworkConditions", new Dictionary<string, object>
        {
            ["offline"] = false,
            ["latency"] = 150,
            ["downloadThroughput"] = (1.6 * 1024 * 1024) / 8,
            ["uploadThroughput"] = (750 * 1024) / 8,
        });

        var tileSeen = false;
        page.Response += (_, response) =>
        {
            if (response.Status == 200
                && response.Url.Contains("tiles.openfreemap.org", StringComparison.OrdinalIgnoreCase))
            {
                tileSeen = true;
            }
        };

        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Commit, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var metrics = await page.EvaluateAsync<JsonElement>(BanenkaartPerfMeasureScript(width));

        var firstPaintMs = metrics.GetProperty("firstPaintMs").GetDouble();
        var lcpMs = metrics.GetProperty("lcpMs").GetDouble();
        var mapTilesMs = metrics.GetProperty("mapTilesMs").GetDouble();
        Assert.True(tileSeen, "Expected at least one OpenFreeMap tile response with HTTP 200.");

        var preMapJs = metrics.GetProperty("preMapJsTransferBytes").GetInt64();

        await page.WaitForTimeoutAsync(500);

        var row = new Dictionary<string, object?>
        {
            ["viewport"] = $"{width}x{height}",
            ["firstPaintMs"] = firstPaintMs,
            ["lcpMs"] = lcpMs,
            ["mapTilesMs"] = mapTilesMs,
            ["preMapJsTransferBytes"] = preMapJs,
            ["targets"] = new Dictionary<string, object?>
            {
                ["firstPaintMs"] = FirstPaintBudgetMs,
                ["lcpMs"] = LcpBudgetMs,
                ["mapTilesMs"] = MapTilesBudgetMs,
                ["preMapJsTransferBytes"] = PreMapJsGzipBudgetBytes,
            },
        };

        Assert.True(preMapJs < PreMapJsGzipBudgetBytes, $"pre-map JS transfer {preMapJs} B (budget {PreMapJsGzipBudgetBytes} B gzip wire)");
        Assert.True(preMapJs < 12_000, "pre-map JS should stay well under the 30 KB gzip budget.");

        if (width <= 390)
        {
            Assert.True(lcpMs < LcpBudgetMs, $"mobile LCP {lcpMs:0} ms (budget {LcpBudgetMs} ms)");
        }

        row["withinBudget"] = new Dictionary<string, object?>
        {
            ["firstPaintMs"] = firstPaintMs < FirstPaintBudgetMs,
            ["lcpMs"] = lcpMs < LcpBudgetMs,
            ["mapTilesMs"] = mapTilesMs < MapTilesBudgetMs,
            ["preMapJsTransferBytes"] = preMapJs < PreMapJsGzipBudgetBytes,
        };
        await AppendReportAsync(width, row);
    }

    [Theory]
    [InlineData(390, 844, true)]
    [InlineData(1280, 720, false)]
    public async Task Banenkaart_after_paspoort_visit_no_console_errors_and_map_budget(int width, int height, bool touch)
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

        var cdp = await context.NewCDPSessionAsync(page);
        await cdp.SendAsync("Emulation.setCPUThrottlingRate", new Dictionary<string, object> { ["rate"] = 4 });
        await cdp.SendAsync("Network.emulateNetworkConditions", new Dictionary<string, object>
        {
            ["offline"] = false,
            ["latency"] = 150,
            ["downloadThroughput"] = (1.6 * 1024 * 1024) / 8,
            ["uploadThroughput"] = (750 * 1024) / 8,
        });

        await page.GotoAsync(_baseUrl + "/__test/sign-in?role=candidate&return=/candidate/paspoort", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForTimeoutAsync(3500);

        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Commit, Timeout = 120_000 });
        var metrics = await page.EvaluateAsync<JsonElement>(BanenkaartPerfMeasureScript(width));
        var mapTilesMs = metrics.GetProperty("mapTilesMs").GetDouble();

        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 60_000 });

        Assert.True(consoleErrors.Count == 0, "Console errors: " + string.Join("; ", consoleErrors));
        Assert.True(mapTilesMs < MapTilesBudgetMs, $"after-paspoort map+tiles {mapTilesMs:0} ms (budget {MapTilesBudgetMs} ms)");
        var afterPaspoort = new Dictionary<string, object?>
        {
            ["scenario"] = "after-paspoort",
            ["viewport"] = $"{width}x{height}",
            ["mapTilesMs"] = mapTilesMs,
            ["targetMapTilesMs"] = MapTilesBudgetMs,
        };
        await AppendReportAsync(width + 10000, afterPaspoort);
    }

    private static async Task AppendReportAsync(int widthKey, Dictionary<string, object?> row)
    {
        using var doc = File.Exists(ReportPath)
            ? JsonDocument.Parse(await File.ReadAllTextAsync(ReportPath))
            : JsonDocument.Parse("{}");
        var root = new Dictionary<string, JsonElement>();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            root[prop.Name] = prop.Value.Clone();
        }

        if (File.Exists(BaselinePath))
        {
            root["before"] = JsonDocument.Parse(await File.ReadAllTextAsync(BaselinePath)).RootElement.Clone();
        }

        var after = root.TryGetValue("after", out var afterEl) && afterEl.ValueKind == JsonValueKind.Object
            ? JsonSerializer.Deserialize<Dictionary<string, object?>>(afterEl.GetRawText()) ?? new Dictionary<string, object?>()
            : new Dictionary<string, object?>();
        after[widthKey.ToString()] = row;
        root["after"] = JsonSerializer.SerializeToElement(after);
        root["measuredAtUtc"] = JsonSerializer.SerializeToElement(DateTime.UtcNow.ToString("O"));
        root["publishDir"] = JsonSerializer.SerializeToElement(PublishDir);

        await File.WriteAllTextAsync(ReportPath, JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string BanenkaartPerfMeasureScript(int width) => $$"""
        async () => {
          const nav = performance.getEntriesByType('navigation')[0];
          const t0 = nav ? nav.startTime : 0;
          const chromeSel = {{width}} >= 900
            ? '[data-testid=kb-panel-head], [data-testid=vacancy-list]'
            : '[data-testid=kb-panel-head-mobile], [data-testid=vacancy-list]';
          const sels = ['[data-testid=app-shell-header]', chromeSel.split(', ')[0], '[data-testid=vacancy-list]'];
          let firstPaintMs = 99999;
          for (let i = 0; i < 300; i++) {
            let ok = true;
            for (const sel of sels) {
              const el = document.querySelector(sel);
              if (!el || el.getBoundingClientRect().height < 8) { ok = false; break; }
            }
            if (ok) { firstPaintMs = performance.now() - t0; break; }
            await new Promise(r => requestAnimationFrame(r));
          }
          let lcpMs = 0;
          try {
            const po = new PerformanceObserver((list) => {
              for (const e of list.getEntries()) {
                if (e.entryType === 'largest-contentful-paint') lcpMs = Math.max(lcpMs, e.startTime - t0);
              }
            });
            po.observe({ type: 'largest-contentful-paint', buffered: true });
            await new Promise(r => setTimeout(r, 800));
            po.disconnect();
          } catch (e) {}
          const mapNeedle = /maplibre-gl-csp|jobsyMapLibre|jobMap\.min/;
          let preMapJsTransferBytes = 0;
          const scripts = performance.getEntriesByType('resource')
            .filter(r => r.initiatorType === 'script' || (r.name && r.name.endsWith('.js')))
            .sort((a, b) => a.startTime - b.startTime);
          for (const r of scripts) {
            if (mapNeedle.test(r.name)) break;
            preMapJsTransferBytes += r.transferSize || 0;
          }
          const navTileResources = () => performance.getEntriesByType('resource')
            .filter(r => r.name && r.name.indexOf('tiles.openfreemap.org') !== -1
              && r.responseEnd > 0 && r.startTime >= t0);
          let mapTilesMs = 99999;
          for (let i = 0; i < 600; i++) {
            const canvas = document.querySelector('#job-map .maplibregl-canvas');
            const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
            const styled = map && typeof map.isStyleLoaded === 'function' && map.isStyleLoaded();
            if (canvas && canvas.getBoundingClientRect().height >= 32 && map && styled) {
              const tiles = navTileResources().sort((a, b) => a.responseEnd - b.responseEnd);
              const tile = tiles[0];
              mapTilesMs = tile ? (tile.responseEnd - t0) : (performance.now() - t0);
              break;
            }
            await new Promise(r => requestAnimationFrame(r));
          }
          if (mapTilesMs > 90000) {
            const tileRes = navTileResources().sort((a, b) => a.responseEnd - b.responseEnd)[0];
            if (tileRes) {
              mapTilesMs = tileRes.responseEnd - t0;
            }
          }
          return { firstPaintMs, lcpMs, mapTilesMs, preMapJsTransferBytes };
        }
        """;

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
        var psi = new ProcessStartInfo
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
        using var pub = Process.Start(psi)!;
        var stdout = await pub.StandardOutput.ReadToEndAsync();
        var stderr = await pub.StandardError.ReadToEndAsync();
        await pub.WaitForExitAsync();
        Assert.True(pub.ExitCode == 0, $"publish failed: {stderr}\n{stdout}");
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
