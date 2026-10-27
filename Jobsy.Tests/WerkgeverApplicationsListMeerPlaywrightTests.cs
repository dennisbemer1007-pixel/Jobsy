using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("BanenkaartPlaywright")]
public sealed class WerkgeverApplicationsListMeerPlaywrightTests : IAsyncLifetime
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
    public async Task Mobile_meer_sheet_opens_on_list_ssr()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        var listPath = "/werkgever/sollicitaties?view=lijst";
        await page.GotoAsync(
            _baseUrl + "/__test/sign-in?role=employer&return=" + Uri.EscapeDataString(listPath),
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForURLAsync("**" + listPath, new() { Timeout = 60_000 });

        var meer = page.Locator("[data-wg-meer-toggle]");
        await meer.WaitForAsync(new() { Timeout = 60_000 });
        await meer.ClickAsync();

        var panel = page.Locator("[data-wg-meer-panel]");
        await page.WaitForSelectorAsync("[data-wg-meer-panel]:not([hidden])", new() { Timeout = 15_000 });

        Assert.False(await panel.IsHiddenAsync());
        Assert.True(await panel.Locator(".wg-sidebar__link").CountAsync() > 0);
    }
}
