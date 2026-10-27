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

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Bedrijvenkaart_ssr_shows_list_and_map_pane(int width, int height)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + "/bedrijvenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForSelectorAsync("[data-bedrijvenkaart-ssr=\"1\"]", new() { Timeout = 60_000 });
        Assert.True(await page.Locator(".bk-list .bk-card").CountAsync() >= 3);
        Assert.True(await page.Locator("#bedrijvenkaart-map").IsVisibleAsync());
    }
}
