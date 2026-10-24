using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Desktop split (≥701px): scrollable list rail beside map, no Kaart/Lijst toggle. Mobile: toggle + Lijst scroll/tags.
/// </summary>
[Collection("BanenkaartPlaywright")]
public sealed class BanenkaartLijstScrollViewPlaywrightTests : IAsyncLifetime
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

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(760, 720)]
    public async Task Desktop_split_list_scrolls_and_shows_full_card_tags(int width, int height)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + DelftPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            "() => window.jobsyBanenkaartCatalog?.isReady?.() === true",
            null,
            new() { Timeout = 90_000 });

        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root && root.classList.contains('show-map')
                && root.getAttribute('data-kb-view') === 'map'
                && document.querySelectorAll('[data-testid=kb-list-row]').length >= 4;
            }
            """,
            null,
            new() { Timeout = 60_000 });

        var rowCount = await page.Locator("[data-testid=kb-list-row]").CountAsync();
        Assert.True(rowCount >= 4, "Expected multiple list rows for scroll test, got " + rowCount);

    }

    [Fact]
    public async Task Mobile_lijst_toggle_scrolls_tags_and_kaart_round_trip()
    {
        const int width = 390;
        const int height = 844;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + DelftPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            "() => window.jobsyBanenkaartCatalog?.isReady?.() === true",
            null,
            new() { Timeout = 90_000 });

        await page.EvaluateAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (!root || !window.applyBanenkaartViewMode) throw new Error('missing view API');
              window.applyBanenkaartViewMode(root, 'list');
            }
            """);
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root && root.classList.contains('show-list')
                && root.getAttribute('data-kb-view') === 'list'
                && window.location.search.includes('weergave=lijst');
            }
            """,
            null,
            new() { Timeout = 15_000 });

        await page.EvaluateAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (!root || !window.applyBanenkaartViewMode) throw new Error('missing view API');
              window.applyBanenkaartViewMode(root, 'map');
            }
            """);
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root && root.classList.contains('show-map')
                && root.getAttribute('data-kb-view') === 'map'
                && !window.location.search.includes('weergave=lijst');
            }
            """,
            null,
            new() { Timeout = 15_000 });
    }
}
