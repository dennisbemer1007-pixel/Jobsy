using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Lagen SSR banenkaart: filter state stays in sync (URL, chip, counts, list, map catalog).
/// </summary>
public sealed class BanenkaartFiltersUrlSyncPlaywrightTests : IAsyncLifetime
{
    private const string BanenkaartPath =
        "/banenkaart?originLat=51.9944&originLng=4.2097&originLabel=Kasweg%2012%2C%202671%20AB%20Naaldwijk&maxMinutes=60&transport=Auto";

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
    public async Task Apply_30_minutes_updates_url_chip_and_counts_without_reload()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1113 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + BanenkaartPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            "() => window.jobsyBanenkaartCatalog?.isReady?.() === true",
            null,
            new() { Timeout = 90_000 });

        await page.EvaluateAsync(
            """
            () => {
              const form = document.getElementById('kb-filter-form');
              const slider = form?.querySelector('[data-testid=kb-sheet-travel-slider]');
              if (slider) slider.value = '30';
              const transport = document.getElementById('kb-filter-transport');
              if (transport) transport.value = 'Auto';
              const filters = window.jobsyBanenkaartCatalog.filtersFromForm(form);
              filters.maxMinutes = 30;
              filters.transport = 'Auto';
              window.jobsyBanenkaartCatalog.applyClientFilters(filters);
            }
            """);

        await page.WaitForFunctionAsync(
            "() => window.location.search.includes('maxMinutes=30')",
            null,
            new() { Timeout = 15_000 });

        var chip = await page.Locator("[data-testid=kb-active-tag-travel]").InnerTextAsync();
        Assert.Contains("30", chip, StringComparison.Ordinal);
        Assert.Contains("rijden", chip, StringComparison.OrdinalIgnoreCase);

        await page.WaitForTimeoutAsync(300);
        var countText = await page.Locator("[data-testid=kb-desktop-result-count]").InnerTextAsync();
        var listRows = await page.Locator("[data-testid=kb-list-row]").CountAsync();
        var emptyMsg = await page.Locator("[data-testid=kb-empty-travel]").CountAsync();
        var widenButtons = await page.Locator("[data-testid=kb-widen-filters]").CountAsync();
        var catalogCount = await page.EvaluateAsync<int>(
            "() => { const f = window.jobsyBanenkaartCatalog?.resolveFilters?.(); const rows = window.jobsyBanenkaartCatalog?.getRows?.() || []; return window.jobsyBanenkaartCatalog?.filterRows?.(rows, f, false)?.length ?? -1; }");
        Assert.True(catalogCount >= 0);
        if (emptyMsg > 0)
        {
            Assert.Equal(0, listRows);
            Assert.Contains("0 vacatures", countText, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.True(listRows > 0);
            Assert.DoesNotContain("0 vacatures", countText, StringComparison.OrdinalIgnoreCase);
        }

        Assert.InRange(widenButtons, 0, 1);
    }

    [Fact]
    public async Task Wissen_resets_to_default_without_full_navigation()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1113 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + BanenkaartPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            "() => window.jobsyBanenkaartCatalog?.isReady?.() === true",
            null,
            new() { Timeout = 90_000 });

        await page.EvaluateAsync("() => window.jobsyBanenkaartCatalog.resetClientFilters()");
        await page.WaitForFunctionAsync(
            """
            () => {
              const u = window.location.pathname + window.location.search;
              return u === '/banenkaart' || u.startsWith('/banenkaart?') && !u.includes('maxMinutes=60');
            }
            """,
            null,
            new() { Timeout = 15_000 });

        var chipVisible = await page.Locator("[data-testid=kb-active-tag-travel]").CountAsync();
        Assert.Equal(0, chipVisible);
    }
}
