using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Werkgever sollicitaties list SSR: desktop table vs mobile cards at 1440 / 390.
/// </summary>
[Collection("BanenkaartPlaywright")]
public sealed class WerkgeverApplicationsListLayoutPlaywrightTests : IAsyncLifetime
{
    private const string ListPath = "/werkgever/sollicitaties?view=lijst";

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
    [InlineData(1440, 900, true)]
    [InlineData(390, 844, false)]
    public async Task List_view_shows_table_on_desktop_and_cards_on_mobile(int width, int height, bool expectDesktop)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            _baseUrl + "/__test/sign-in?role=employer&return=" + Uri.EscapeDataString(ListPath),
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForURLAsync("**" + ListPath, new() { Timeout = 60_000 });
        await page.WaitForSelectorAsync("[data-wg-list-ssr=\"1\"]", new() { Timeout = 60_000 });

        var desktopVisible = await page.EvaluateAsync<bool>(
            """
            () => {
              const desktop = document.querySelector('.wg-app__list-desktop');
              const mobile = document.querySelector('.wg-app__list-mobile');
              if (!desktop || !mobile) return false;
              const d = getComputedStyle(desktop).display;
              const m = getComputedStyle(mobile).display;
              return d !== 'none' && m === 'none';
            }
            """);

        Assert.Equal(expectDesktop, desktopVisible);
        if (expectDesktop)
        {
            Assert.True(await page.Locator(".wg-app__list-desktop .ent-data-table").IsVisibleAsync());
            Assert.True(await page.Locator(".wg-app__list-desktop a[href*=\"/werkgever/sollicitaties/\"]").CountAsync() > 0);
        }
        else
        {
            Assert.True(await page.Locator(".wg-app__list-mobile").IsVisibleAsync());
        }
    }
}
