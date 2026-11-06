using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Acc repro (Delft, ~181 vacatures): pins must survive zoom, cluster expand, and viewport pin reload.
/// </summary>
public sealed class BanenkaartLagenDelftPinsZoomPlaywrightTests : IAsyncLifetime
{
    private const string DelftPath =
        "/banenkaart?maxMinutes=60&originLat=51.99389784&originLng=4.37897242&originLabel=Delft%2C%20Delft";

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
    public async Task Delft_dense_fixture_pins_survive_zoom_cluster_and_reload()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + DelftPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
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

        await page.WaitForFunctionAsync(
            "() => (window.jobMap?.__testGetPinCount?.() ?? 0) >= 80",
            null,
            new() { Timeout = 90_000 });

        var viewportQs = await page.EvaluateAsync<string>(
            "() => window.jobMap?.__testViewportQueryString?.() ?? 'missing'");
        Assert.Equal("", viewportQs);

        var afterZoom = await page.EvaluateAsync<int>(
            """
            () => {
              window.jobMap?.__testMarkUserMapGesture?.();
              const map = window.jobMap?.__testGetMap?.();
              if (!map) return 0;
              map.setZoom(Math.min(map.getMaxZoom(), map.getZoom() + 3));
              return window.jobMap.__testGetPinCount?.() ?? 0;
            }
            """);
        Assert.True(afterZoom >= 80, "Pin data wiped after zoom-in.");

        await page.WaitForTimeoutAsync(400);
        await page.EvaluateAsync("() => window.jobMap?.__testMarkUserMapGesture?.()");
        await page.EvaluateAsync(
            """
            async () => {
              await window.jobMap?.reloadPins?.();
            }
            """);
        await page.WaitForTimeoutAsync(500);

        var afterReload = await page.EvaluateAsync<int>(
            "() => window.jobMap?.__testGetPinCount?.() ?? 0");
        Assert.True(afterReload >= 80, "Pin data wiped after reloadPins (lagen must not use viewport bbox).");

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
                  return data >= 80 && rendered >= 1;
                }
                """,
                null,
                new() { Timeout = 30_000 });
        }
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
        {
            var debug = await page.EvaluateAsync<string>(
                "() => JSON.stringify(window.jobMap?.__testLagenPinLayerDebug?.() ?? {})");
            Assert.Fail("Delft pins missing after cluster tap. Debug: " + debug + " (" + ex.Message + ")");
        }

        var pinsUrl = await page.EvaluateAsync<string>(
            "() => window.jobMap?.__testGetPinsUrl?.() ?? ''");
        Assert.DoesNotContain("minLat=", pinsUrl);
    }
}
