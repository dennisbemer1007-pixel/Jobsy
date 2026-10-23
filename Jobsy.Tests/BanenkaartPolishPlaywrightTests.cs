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
