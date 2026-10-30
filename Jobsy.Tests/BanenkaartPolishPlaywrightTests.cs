using System.Diagnostics;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Banenkaart polish (footer, popup chrome, zoom −, viewport resize, glyphs) against Release publish at /tmp/pub.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartPolishPlaywrightTests : IAsyncLifetime
{
    private const string PublishDir = "/tmp/pub";

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
    public async Task Banenkaart_polish_footer_minus_console(int width, int height, bool touch)
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
        var font404 = 0;
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };
        page.Response += (_, resp) =>
        {
            if (resp.Status == 404 && resp.Url.Contains("/fonts/", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref font404);
            }
        };

        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 90_000 });

        if (touch)
        {
            var footerOk = await page.EvaluateAsync<bool>(
                """
                () => {
                  const map = document.querySelector('#job-map');
                  const footer = document.querySelector('.app-footer');
                  if (!map) return false;
                  const mr = map.getBoundingClientRect();
                  if (!footer) return true;
                  const fs = getComputedStyle(footer);
                  if (fs.display === 'none' || fs.visibility === 'hidden' || footer.getClientRects().length === 0) {
                    return true;
                  }
                  const fr = footer.getBoundingClientRect();
                  if (fr.height < 1) return true;
                  return fr.top >= mr.bottom - 2;
                }
                """);
            Assert.True(footerOk, "Mobile site footer must not overlap the map pane.");
        }
        else
        {
            var minusVisible = await page.EvaluateAsync<bool>(
                """
                () => {
                  const el = document.querySelector('.job-map .maplibregl-ctrl-zoom-out');
                  if (!el) return false;
                  const r = el.getBoundingClientRect();
                  const s = getComputedStyle(el);
                  return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none';
                }
                """);
            Assert.True(minusVisible, "Desktop map must show the zoom-out (−) control when no popup is open.");
        }

        Assert.Equal(0, font404);
        var badConsole = consoleErrors.Where(e =>
            !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(badConsole.Count == 0, "Console errors: " + string.Join("; ", badConsole));
    }

    [Fact]
    public async Task Banenkaart_map_visible_after_resize_mobile_to_desktop()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 90_000 });

        await page.EvaluateAsync(
            """
            () => {
              sessionStorage.setItem('jobsy.bk.ui.v1', JSON.stringify({ filtersOpen: false, showMap: false }));
            }
            """);

        await page.SetViewportSizeAsync(1280, 720);
        await page.WaitForTimeoutAsync(400);

        var mapOk = await page.EvaluateAsync<bool>(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (root && !root.classList.contains('show-map')) return false;
              const canvas = document.querySelector('#job-map canvas, #job-map .maplibregl-canvas');
              if (!canvas) return false;
              const h = canvas.getBoundingClientRect().height;
              return h > 300;
            }
            """);
        Assert.True(mapOk, "Map canvas should stay visible after resizing from mobile to desktop.");
    }

    [Fact]
    public async Task Desktop_list_thumbnails_stay_compact_after_hard_reload()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        var url = _baseUrl + "/banenkaart";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.GotoAsync(url + "?_nocache=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });

        await page.Locator("[data-testid=kb-list-row]").First.WaitForAsync(new() { Timeout = 60_000 });
        var gridCols = await page.EvaluateAsync<int>(
            """
            () => {
              const grid = document.querySelector('[data-testid=kb-list-rows]');
              if (!grid) return 0;
              return getComputedStyle(grid).gridTemplateColumns.split(' ').filter(Boolean).length;
            }
            """);
        Assert.Equal(2, gridCols);

        var rowHeights = await page.EvaluateAsync<double[]>(
            """
            () => Array.from(document.querySelectorAll('[data-testid=kb-list-row]'))
              .slice(0, 4)
              .map(el => el.getBoundingClientRect().height)
            """
        );
        Assert.True(rowHeights.Length >= 2);
        Assert.True(Math.Abs(rowHeights[0] - rowHeights[1]) < 4,
            "First row cards should share equal height (h0=" + rowHeights[0] + ", h1=" + rowHeights[1] + ").");

        var docWidth = await page.EvaluateAsync<double>("() => document.documentElement.scrollWidth");
        var viewWidth = await page.EvaluateAsync<double>("() => document.documentElement.clientWidth");
        Assert.True(docWidth <= viewWidth + 2, "Desktop list should not horizontal-scroll (doc=" + docWidth + ", view=" + viewWidth + ").");
    }

    [Fact]
    public async Task Mobile_cluster_taps_to_pin_level_without_console_noise()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var consoleErrors = new List<string>();
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type is "error" or "warning")
            {
                consoleErrors.Add(msg.Text);
            }
        };
        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && window.jobMap.__testTapLargestCluster && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 90_000 });

        for (var i = 0; i < 4; i++)
        {
            await page.EvaluateAsync("() => window.jobMap.__testTapLargestCluster && window.jobMap.__testTapLargestCluster()");
            await page.WaitForTimeoutAsync(450);
        }

        var bad = consoleErrors.Where(e =>
            !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)
            && !e.Contains("GL Driver Message", StringComparison.OrdinalIgnoreCase)
            && !e.Contains("GPU stall", StringComparison.OrdinalIgnoreCase)
            && !e.Contains("Expected value to be of type number", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.True(bad.Count == 0, "Console warnings/errors after cluster taps: " + string.Join("; ", bad));
    }

    [Fact]
    public async Task Mobile_list_view_scrolls_after_reload()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.EvaluateAsync(
            """
            () => {
              sessionStorage.setItem('jobsy.bk.ui.v1', JSON.stringify({ filtersOpen: false, showMap: false }));
            }
            """);
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForFunctionAsync(
            "() => document.querySelector('.jobsy-discovery.show-list') !== null",
            null,
            new() { Timeout = 30_000 });
        await page.Locator(".jobsy-discovery.show-list [data-testid=kb-list-row]").First
            .WaitForAsync(new() { Timeout = 60_000 });
        var rowCount = await page.Locator("[data-testid=kb-list-row]").CountAsync();
        Assert.True(rowCount >= 3, "Need multiple list rows to assert scroll (count=" + rowCount + ").");

        var scrollState = await page.EvaluateAsync<string>(
            """
            () => {
              const scroller = document.querySelector('.jobsy-discovery.show-list .vacancy-list')
                || document.querySelector('.jobsy-discovery.show-list .vacancy-pane');
              if (!scroller) return JSON.stringify({ error: 'no-scroller' });
              scroller.style.maxHeight = '280px';
              const style = getComputedStyle(scroller);
              const before = scroller.scrollTop;
              scroller.scrollTop = 9999;
              const after = scroller.scrollTop;
              return JSON.stringify({
                overflowY: style.overflowY,
                scrollHeight: scroller.scrollHeight,
                clientHeight: scroller.clientHeight,
                before: before,
                after: after
              });
            }
            """);
        using var scrollDoc = System.Text.Json.JsonDocument.Parse(scrollState);
        var root = scrollDoc.RootElement;
        Assert.True(
            root.GetProperty("overflowY").GetString() is "auto" or "scroll",
            "Mobile list scroller should use overflow-y auto (state=" + scrollState + ").");
        var canScroll = root.GetProperty("scrollHeight").GetDouble()
            > root.GetProperty("clientHeight").GetDouble() + 8;
        var moved = root.GetProperty("after").GetDouble() > 40;
        Assert.True(canScroll && moved,
            "Mobile list should scroll when content overflows (state=" + scrollState + ").");
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
