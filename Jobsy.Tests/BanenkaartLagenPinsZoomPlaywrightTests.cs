using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Lagen banenkaart: unclustered pins must stay visible after zoom-in and cluster expansion.
/// </summary>
public sealed class BanenkaartLagenPinsZoomPlaywrightTests : IAsyncLifetime
{
    private const string BanenkaartPath =
        "/banenkaart?originLat=51.9944&originLng=4.2097&originLabel=Kasweg%2012%2C%202671%20AB%20Naaldwijk&maxMinutes=20&transport=Fiets&minHours=32&maxHours=40";

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

    [Fact]
    public async Task Desktop_1920_pins_stay_rendered_after_zoom_in_and_cluster_tap()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + BanenkaartPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.dataset?.banenkaartSsrBound === '1'",
            null,
            new() { Timeout = 90_000 });
        await page.EvaluateAsync(
            """
            () => {
              window.jobMap?.invalidate?.();
              window.jobMap?.__testGetMap?.()?.resize?.();
            }
            """);
        await page.WaitForTimeoutAsync(500);
        await WaitForInitialPinsAsync(page);

        var afterZoom = await page.EvaluateAsync<int>(
            """
            () => {
              const map = window.jobMap?.__testGetMap?.();
              if (!map) return 0;
              map.setZoom(Math.min(map.getMaxZoom(), map.getZoom() + 2.5));
              return window.jobMap.__testGetPinCount?.() ?? 0;
            }
            """);
        Assert.True(afterZoom > 0, "Expected pin data after zoom-in.");
        try
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const n = window.jobMap?.__testGetRenderedPinCount?.();
                  return typeof n === 'number' && n >= 1;
                }
                """,
                null,
                new() { Timeout = 30_000 });
        }
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
        {
            var debug = await page.EvaluateAsync<string>(
                "() => JSON.stringify(window.jobMap?.__testLagenPinLayerDebug?.() ?? {})");
            Assert.Fail("No rendered pins after zoom-in. Debug: " + debug + " (" + ex.Message + ")");
        }

        await page.EvaluateAsync(
            """
            () => {
              const map = window.jobMap?.__testGetMap?.();
              if (map && map.getZoom() > 11) {
                map.setZoom(10.2);
              }
              window.jobMap?.__testTapLargestCluster?.();
            }
            """);
        await page.WaitForTimeoutAsync(1200);
        try
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const data = window.jobMap?.__testGetPinCount?.() ?? 0;
                  const rendered = window.jobMap?.__testGetRenderedPinCount?.() ?? 0;
                  return data > 0 && rendered >= 1;
                }
                """,
                null,
                new() { Timeout = 30_000 });
        }
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
        {
            var debug = await page.EvaluateAsync<string>(
                "() => JSON.stringify(window.jobMap?.__testLagenPinLayerDebug?.() ?? {})");
            Assert.Fail("No rendered pins after cluster tap. Debug: " + debug + " (" + ex.Message + ")");
        }
    }

    private static async Task WaitForInitialPinsAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => (window.jobMap?.__testGetPinCount?.() ?? 0) >= 5
            """,
            null,
            new() { Timeout = 90_000 });

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var rendered = await page.EvaluateAsync<int>(
                "() => window.jobMap?.__testGetRenderedPinCount?.() ?? 0");
            if (rendered >= 1)
            {
                return;
            }

            await page.EvaluateAsync(
                """
                () => {
                  const map = window.jobMap?.__testGetMap?.();
                  if (map && typeof map.setZoom === 'function') {
                    map.setZoom(Math.max(9, map.getZoom() - 1));
                  }
                }
                """);
            await page.WaitForTimeoutAsync(600);
        }

        await page.WaitForFunctionAsync(
            """
            () => (window.jobMap?.__testGetRenderedPinCount?.() ?? 0) >= 1
            """,
            null,
            new() { Timeout = 30_000 });
    }
}
