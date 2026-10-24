using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Desktop lagen: Funda popup on the map (mockup c8ee9298) — not the docked cluster sheet.
/// </summary>
public sealed class BanenkaartLagenDesktopPopupPlaywrightTests : IAsyncLifetime
{
    private const string BanenkaartPath =
        "/banenkaart?originLat=51.9944&originLng=4.2097&originLabel=Kasweg%2012%2C%202671%20AB%20Naaldwijk&maxMinutes=20&transport=Fiets&minHours=32&maxHours=40";

    private const string VacancyWithPhoto = "b2000001-0000-0000-0000-000000000001";

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
    public async Task Desktop_1280_map_attached_funda_popup_wage_and_close()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + BanenkaartPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.dataset?.banenkaartSsrBound === '1'",
            null,
            new() { Timeout = 90_000 });
        await page.EvaluateAsync("() => { window.jobMap?.invalidate?.(); window.jobMap?.__testGetMap?.()?.resize?.(); }");
        await page.WaitForTimeoutAsync(400);

        Assert.True(await page.EvaluateAsync<bool>(
            "(vid) => window.jobMap?.__testOpenVacancyById?.(vid) ?? false",
            VacancyWithPhoto));

        await page.Locator(".maplibregl-popup.kb-popup-lagen-pop").First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15_000
        });

        await page.EvaluateAsync(
            "() => document.querySelector('[data-lagen-wage]')?.dispatchEvent(new MouseEvent('mouseenter', { bubbles: true }))");
        await page.WaitForFunctionAsync(
            "() => { const t = document.querySelector('.kb-popup-lagen-pop .tip'); return t && !t.hasAttribute('hidden'); }",
            null,
            new() { Timeout = 8_000 });

        await page.EvaluateAsync("() => document.querySelector('[data-lagen-close]')?.click()");
        await page.WaitForFunctionAsync(
            "() => !document.querySelector('.maplibregl-popup.kb-popup-lagen-pop')",
            null,
            new() { Timeout = 10_000 });
    }
}
