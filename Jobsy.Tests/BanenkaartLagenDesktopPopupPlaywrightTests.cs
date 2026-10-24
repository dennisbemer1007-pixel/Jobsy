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

        await page.Locator("[data-lagen-heart]").First.ClickAsync(new() { Force = true });
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

    [Fact]
    public async Task Desktop_popup_shell_appears_before_vacancy_card_api()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1113 },
            IgnoreHTTPSErrors = true
        });
        await context.RouteAsync("**/api/vacancies/*/card**", async route =>
        {
            await Task.Delay(8000);
            await route.ContinueAsync();
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + BanenkaartPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.dataset?.banenkaartSsrBound === '1'",
            null,
            new() { Timeout = 90_000 });
        await page.EvaluateAsync("() => { window.jobMap?.invalidate?.(); window.jobMap?.__testGetMap?.()?.resize?.(); }");

        var elapsedMs = await page.EvaluateAsync<int>(
            """
            async (vid) => {
              const t0 = performance.now();
              if (!window.jobMap?.__testOpenVacancyById?.(vid)) return -1;
              for (let i = 0; i < 120; i++) {
                if (document.querySelector('.maplibregl-popup.kb-popup-lagen-pop .fcw')) {
                  return Math.round(performance.now() - t0);
                }
                await new Promise(r => requestAnimationFrame(r));
              }
              return 9999;
            }
            """,
            VacancyWithPhoto);

        Assert.InRange(elapsedMs, 0, 350);
    }

    [Fact]
    public async Task Desktop_map_view_and_popup_restore_after_browser_back()
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

        const double targetZoom = 13.25;
        await page.EvaluateAsync(
            """
            async ({ vid, z }) => {
              const map = window.jobMap?.__testGetMap?.();
              if (map) map.jumpTo({ zoom: z, duration: 0 });
              window.jobMap?.__testOpenVacancyById?.(vid);
            }
            """,
            new { vid = VacancyWithPhoto, z = targetZoom });

        await page.Locator(".maplibregl-popup.kb-popup-lagen-pop").First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15_000
        });

        await page.Locator(".fgo").First.ClickAsync();
        await page.WaitForURLAsync("**/vacancies/**", new() { Timeout = 30_000 });
        await page.GoBackAsync(new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.dataset?.banenkaartSsrBound === '1'",
            null,
            new() { Timeout = 90_000 });

        await page.WaitForFunctionAsync(
            """
            (z) => {
              const map = window.jobMap?.__testGetMap?.();
              const pop = document.querySelector('.maplibregl-popup.kb-popup-lagen-pop');
              return map && Math.abs(map.getZoom() - z) < 0.08 && !!pop;
            }
            """,
            targetZoom,
            new() { Timeout = 25_000 });
    }

    [Fact]
    public async Task Desktop_cluster_pager_advances_counter()
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
        await page.WaitForTimeoutAsync(600);

        var opened = await page.EvaluateAsync<bool>("() => window.jobMap?.__testTapLargestCluster?.() ?? false");
        Assert.True(opened);

        await page.Locator(".maplibregl-popup.kb-popup-lagen-pop [data-lagen-counter]").First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15_000
        });

        var before = await page.Locator("[data-lagen-counter]").First.InnerTextAsync();
        Assert.Contains("1 van", before, StringComparison.OrdinalIgnoreCase);

        await page.Locator("[data-lagen-next]").First.ClickAsync();
        await page.WaitForFunctionAsync(
            """
            () => {
              const t = document.querySelector('[data-lagen-counter]')?.textContent || '';
              return /2\\s*van/i.test(t);
            }
            """,
            null,
            new() { Timeout = 8_000 });
    }

    [Fact]
    public async Task Desktop_three_sequential_pin_opens_stay_in_focus_rect()
    {
        var ids = new[]
        {
            "b2000001-0000-0000-0000-000000000001",
            "b2000003-0000-0000-0000-000000000001",
            "b2000005-0000-0000-0000-000000000001"
        };

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

        foreach (var id in ids)
        {
            Assert.True(await page.EvaluateAsync<bool>(
                "(vid) => window.jobMap?.__testOpenVacancyById?.(vid) ?? false",
                id));
            await page.Locator(".maplibregl-popup.kb-popup-lagen-pop").First.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 15_000
            });
            await page.WaitForTimeoutAsync(450);
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
            Assert.True(inFocus, "Popup should stay fully visible below the carousel on every open (id=" + id + ").");
            await page.Locator("[data-lagen-close]").First.ClickAsync();
            await page.WaitForFunctionAsync(
                "() => !document.querySelector('.maplibregl-popup.kb-popup-lagen-pop')",
                null,
                new() { Timeout = 10_000 });
        }
    }

    [Fact]
    public async Task Desktop_popup_photo_loads_webp_from_api()
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
        Assert.True(await page.EvaluateAsync<bool>(
            "(vid) => window.jobMap?.__testOpenVacancyById?.(vid) ?? false",
            VacancyWithPhoto));
        await page.WaitForFunctionAsync(
            """
            () => {
              const img = document.querySelector('.kb-popup-lagen-pop .fph img');
              return img && img.complete && img.naturalWidth > 0;
            }
            """,
            null,
            new() { Timeout = 15_000 });
    }
}
