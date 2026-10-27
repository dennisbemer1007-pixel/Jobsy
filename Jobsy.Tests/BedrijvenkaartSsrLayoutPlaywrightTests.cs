using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("BanenkaartPlaywright")]
public sealed class BedrijvenkaartSsrLayoutPlaywrightTests : IAsyncLifetime
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
    public async Task Desktop_shows_list_and_map_side_by_side()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + "/bedrijvenkaart/ssr", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForSelectorAsync("[data-bedrijvenkaart-ssr=\"1\"]", new() { Timeout = 60_000 });

        var split = await page.EvaluateAsync<bool>(
            """
            () => {
              const root = document.querySelector('[data-bedrijvenkaart-ssr="1"]');
              const list = root && root.querySelector('.bk-list');
              const map = root && root.querySelector('.bk-map-pane');
              if (!list || !map) return false;
              const ld = getComputedStyle(list).display;
              const md = getComputedStyle(map).display;
              if (ld === 'none' || md === 'none') return false;
              const lr = list.getBoundingClientRect();
              const mr = map.getBoundingClientRect();
              return lr.width > 200 && mr.width > 200 && lr.left < mr.left;
            }
            """);
        Assert.True(split);
        Assert.False(await page.Locator("[data-bk-view-toggle]").IsVisibleAsync());
    }

    [Fact]
    public async Task Mobile_toggles_between_map_and_list()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + "/bedrijvenkaart/ssr", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForSelectorAsync("[data-bk-view-toggle]", new() { Timeout = 60_000 });

        Assert.True(await page.Locator("#bedrijvenkaart-map").IsVisibleAsync());
        Assert.False(await page.Locator(".bk-list .bk-card").First.IsVisibleAsync());

        await page.Locator("[data-bk-view-list]").ClickAsync();
        await page.WaitForSelectorAsync("[data-bk-mobile-view=\"list\"]", new() { Timeout = 15_000 });
        Assert.True(await page.Locator(".bk-list .bk-card").First.IsVisibleAsync());
        Assert.False(await page.Locator("#bedrijvenkaart-map").IsVisibleAsync());

        await page.Locator("[data-bk-view-map]").ClickAsync();
        await page.WaitForSelectorAsync("[data-bk-mobile-view=\"map\"]", new() { Timeout = 15_000 });
        Assert.True(await page.Locator("#bedrijvenkaart-map").IsVisibleAsync());
    }
}
