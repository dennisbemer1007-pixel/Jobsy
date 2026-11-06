using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("BanenkaartPlaywright")]
public sealed class BanenkaartMapCenterWithoutOriginPlaywrightTests : IAsyncLifetime
{
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
    public async Task MaxMinutes_without_origin_centers_map_on_netherlands_not_stored_origin()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + "/banenkaart?maxMinutes=30", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            "() => window.jobsyBanenkaartCatalog?.isReady?.() === true",
            null,
            new() { Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const map = window.jobMap?.__testGetMap?.();
              if (!map || typeof map.getCenter !== 'function') return false;
              const c = map.getCenter();
              return c.lat >= 50.5 && c.lat <= 53.8 && c.lng >= 3.0 && c.lng <= 7.5;
            }
            """,
            null,
            new() { Timeout = 90_000 });

        var center = await page.EvaluateAsync<double[]>(
            """
            () => {
              const c = window.jobMap.__testGetMap().getCenter();
              return [c.lat, c.lng];
            }
            """);
        Assert.InRange(center[0], 50.5, 53.8);
        Assert.InRange(center[1], 3.0, 7.5);
    }
}
