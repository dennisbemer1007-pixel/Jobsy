using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Lagen banenkaart: isochrone rings follow travel-time filter (1 ring vs 3 rings).
/// </summary>
public sealed class BanenkaartTravelRingsPlaywrightTests : IAsyncLifetime
{
    private const string Origin20Path =
        "/banenkaart?originLat=51.9944&originLng=4.2097&originLabel=Kasweg%2012%2C%202671%20AB%20Naaldwijk&maxMinutes=20&transport=Fiets";

    private const string Origin30Path =
        "/banenkaart?originLat=51.9944&originLng=4.2097&originLabel=Kasweg%2012%2C%202671%20AB%20Naaldwijk&maxMinutes=30&transport=Fiets";

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
    public async Task Default_maxMinutes_20_shows_one_travel_ring()
    {
        await using var context = await NewDesktopContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + Origin20Path, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await WaitCatalogAndRingsAsync(page);

        Assert.Equal(1, await WaitRingCountAsync(page, 1));
        Assert.True(await page.EvaluateAsync<bool>("() => window.jobMap.__testLagenTravelTimeFilterActive()"));
    }

    [Fact]
    public async Task Travel_chip_remove_shows_three_rings_without_time_filter()
    {
        await using var context = await NewDesktopContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + Origin20Path, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await WaitCatalogAndRingsAsync(page);

        await page.Locator("[data-testid=kb-active-tag-travel]").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => window.jobsyBanenkaartCatalog?.resolveFilters?.()?.maxMinutes == null",
            null,
            new() { Timeout = 15_000 });

        Assert.Equal(3, await WaitRingCountAsync(page, 3));
        Assert.False(await page.EvaluateAsync<bool>("() => window.jobMap.__testLagenTravelTimeFilterActive()"));
    }

    [Fact]
    public async Task MaxMinutes_30_shows_one_travel_ring()
    {
        await using var context = await NewDesktopContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + Origin30Path, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await WaitCatalogAndRingsAsync(page);

        Assert.Equal(1, await WaitRingCountAsync(page, 1));
    }

    private Task<IBrowserContext> NewDesktopContextAsync()
        => _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1113 },
            IgnoreHTTPSErrors = true
        });

    private static async Task WaitCatalogAndRingsAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => window.jobsyBanenkaartCatalog?.isReady?.() === true && window.jobMap?.__testGetTravelRingFeatureCount",
            null,
            new() { Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const n = window.jobMap.__testGetTravelRingFeatureCount();
              return n >= 1;
            }
            """,
            null,
            new() { Timeout = 90_000 });
    }

    private static async Task<int> WaitRingCountAsync(IPage page, int expected)
    {
        await page.WaitForFunctionAsync(
            $"() => window.jobMap.__testGetTravelRingFeatureCount() === {expected}",
            null,
            new() { Timeout = 90_000 });
        return await page.EvaluateAsync<int>("() => window.jobMap.__testGetTravelRingFeatureCount()");
    }
}
