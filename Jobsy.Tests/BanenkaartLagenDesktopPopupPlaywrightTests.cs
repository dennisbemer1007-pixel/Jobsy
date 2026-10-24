using System.Threading;
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

    [Fact]
    public async Task Desktop_popup_stays_in_map_focus_rect_and_heart_does_not_navigate()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1113 },
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

        var inFocus = await page.EvaluateAsync<bool>(
            """
            () => {
              const pop = document.querySelector('.maplibregl-popup.kb-popup-lagen-pop');
              const carousel = document.querySelector('.highlight-carousel--map');
              if (!pop) return false;
              const pr = pop.getBoundingClientRect();
              const map = document.querySelector('#job-map');
              if (!map) return false;
              const mr = map.getBoundingClientRect();
              let top = mr.top + 12;
              if (carousel) {
                const cr = carousel.getBoundingClientRect();
                if (cr.bottom > top) top = cr.bottom + 8;
              }
              const left = mr.left + 12;
              const right = mr.right - 12;
              const bottom = mr.bottom - 12;
              return pr.top >= top - 2 && pr.left >= left - 2 && pr.right <= right + 2 && pr.bottom <= bottom + 2;
            }
            """);
        Assert.True(inFocus, "Desktop popup should sit fully inside the map area below the carousel.");

        await page.Locator("[data-save-job]").First.ClickAsync(new() { Force = true });
        await page.WaitForTimeoutAsync(400);
        Assert.DoesNotContain("/vacancies/", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Desktop_popup_skips_map_popup_photo_when_pin_has_no_image()
    {
        const string noPhotoId = "b2000002-0000-0000-0000-000000000001";
        var photoRequests = 0;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1113 },
            IgnoreHTTPSErrors = true
        });
        await context.RouteAsync("**/map-popup-photo**", route =>
        {
            Interlocked.Increment(ref photoRequests);
            return route.FulfillAsync(new() { Status = 404, Body = "" });
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + BanenkaartPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.dataset?.banenkaartSsrBound === '1'",
            null,
            new() { Timeout = 90_000 });
        await page.EvaluateAsync("() => { window.jobMap?.invalidate?.(); window.jobMap?.__testGetMap?.()?.resize?.(); }");
        Assert.True(await page.EvaluateAsync<bool>(
            "(vid) => window.jobMap?.__testOpenVacancyById?.(vid) ?? false",
            noPhotoId));
        await page.WaitForTimeoutAsync(2500);
        Assert.Equal(0, photoRequests);
        Assert.True(await page.EvaluateAsync<bool>(
            "() => !!document.querySelector('.fph--category, .fph--empty')"));
    }
}
