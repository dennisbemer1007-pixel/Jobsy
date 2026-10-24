using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Desktop Lijst view: SSOT view toggle, scrollable grid, full card tags, Kaart/Lijst round-trip, Back restores list.
/// </summary>
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
    [InlineData(910, 720)]
    public async Task Kaart_lijst_toggle_scrolls_tags_visible_back_restores_list(int width, int height)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
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
                && !root.classList.contains('show-list')
                && root.getAttribute('data-kb-view') === 'map'
                && !window.location.search.includes('weergave=lijst');
            }
            """,
            null,
            new() { Timeout = 15_000 });

        await page.Locator("[data-testid=kb-view-lijst]").First.ClickAsync();
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root && root.classList.contains('show-list')
                && root.classList.contains('jobsy-discovery--list')
                && !root.classList.contains('show-map')
                && root.getAttribute('data-kb-view') === 'list'
                && window.location.search.includes('weergave=lijst');
            }
            """,
            null,
            new() { Timeout = 15_000 });

        var rowCount = await page.Locator("[data-testid=kb-list-row]").CountAsync();
        Assert.True(rowCount >= 4, "Expected multiple list rows for scroll test, got " + rowCount);

        Assert.True(await CardMetaFullyVisibleAsync(page), "List card meta row must not be clipped.");

        Assert.True(await WheelScrollReachesLastCardAsync(page), "Wheel scroll on list must reach the last card.");

        await page.Locator("[data-testid=kb-view-kaart]").First.ClickAsync();
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root && root.classList.contains('show-map')
                && !root.classList.contains('show-list')
                && !root.classList.contains('jobsy-discovery--list')
                && root.getAttribute('data-kb-view') === 'map'
                && !window.location.search.includes('weergave=lijst');
            }
            """,
            null,
            new() { Timeout = 15_000 });

        Assert.True(await CardMetaFullyVisibleAsync(page), "Kaart rail card meta row must not be clipped.");

        await page.Locator("[data-testid=kb-view-lijst]").First.ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.getAttribute('data-kb-view') === 'list'",
            null,
            new() { Timeout = 15_000 });

        var href = await page.Locator("[data-testid=kb-list-row]").First.GetAttributeAsync("href");
        Assert.False(string.IsNullOrWhiteSpace(href));
        await page.Locator("[data-testid=kb-list-row]").First.ClickAsync();
        await page.WaitForURLAsync("**/vacancies/**", new() { Timeout = 60_000 });
        await page.GoBackAsync(new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root && root.classList.contains('show-list')
                && root.classList.contains('jobsy-discovery--list')
                && !root.classList.contains('show-map')
                && root.getAttribute('data-kb-view') === 'list'
                && window.location.search.includes('weergave=lijst');
            }
            """,
            null,
            new() { Timeout = 30_000 });

        var brokenSplit = await page.EvaluateAsync<bool>(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (!root || !root.classList.contains('show-list')) return true;
              const map = document.querySelector('.map-pane');
              if (!map) return false;
              const ms = getComputedStyle(map);
              return ms.display !== 'none' && map.getClientRects().length > 0;
            }
            """);
        Assert.False(brokenSplit, "Back must not show map pane beside list layout.");
    }

    private static async Task<bool> CardMetaFullyVisibleAsync(IPage page)
    {
        return await page.EvaluateAsync<bool>(
            """
            () => {
              const card = document.querySelector('[data-testid=kb-list-row]');
              if (!card) return false;
              const meta = card.querySelector('.vb .meta');
              if (!meta || meta.getClientRects().length === 0) return true;
              const cr = card.getBoundingClientRect();
              const mr = meta.getBoundingClientRect();
              return mr.bottom <= cr.bottom + 1 && mr.top >= cr.top - 1;
            }
            """);
    }

    private static async Task<bool> WheelScrollReachesLastCardAsync(IPage page)
    {
        return await page.EvaluateAsync<bool>(
            """
            async () => {
              const list = document.querySelector('[data-banenkaart-ssr="1"] .vacancy-list');
              const rows = document.querySelectorAll('[data-testid=kb-list-row]');
              if (!list || rows.length < 2) return false;
              const last = rows[rows.length - 1];
              const before = list.scrollTop;
              for (let i = 0; i < 24; i++) {
                list.dispatchEvent(new WheelEvent('wheel', { deltaY: 400, bubbles: true, cancelable: true }));
                await new Promise(r => requestAnimationFrame(r));
              }
              list.scrollTop = list.scrollHeight;
              await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
              if (list.scrollTop <= before && list.scrollHeight > list.clientHeight + 8) return false;
              const lr = list.getBoundingClientRect();
              const br = last.getBoundingClientRect();
              return br.bottom <= lr.bottom + 4 && br.top >= lr.top - 4;
            }
            """);
    }
}
