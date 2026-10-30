using System.Text;
using System.Text.Json;
using Jobsy.Web.Banenkaart;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Acc #940 regressions: travel chip/subtitle with client-set origin; map origin URL SSOT.
/// </summary>
public sealed class BanenkaartChipOriginSsotPlaywrightTests : IAsyncLifetime
{
    private const double DelftLat = 51.99389784;
    private const double DelftLng = 4.37897242;
    private const string DelftLabel = "Delft, Delft";

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
    public async Task Client_origin_and_30_minutes_shows_travel_chip_and_subtitle()
    {
        await using var context = await NewDesktopContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await WaitCatalogReadyAsync(page);

        await page.EvaluateAsync(
            """
            ({ lat, lng, label }) => {
              const form = document.getElementById('kb-filter-form');
              if (!form) throw new Error('missing kb-filter-form');
              document.querySelectorAll('[data-kb-address]').forEach(el => { el.value = label; });
              const latEl = document.getElementById('kb-filter-origin-lat');
              const lngEl = document.getElementById('kb-filter-origin-lng');
              const labelEl = document.getElementById('kb-filter-origin-label');
              if (latEl) latEl.value = String(lat);
              if (lngEl) lngEl.value = String(lng);
              if (labelEl) labelEl.value = label;
              const slider = form.querySelector('[data-testid=kb-sheet-travel-slider]');
              if (slider) slider.value = '30';
              const transport = document.getElementById('kb-filter-transport');
              if (transport) transport.value = 'Fiets';
              const filters = window.jobsyBanenkaartCatalog.filtersFromForm(form);
              filters.maxMinutes = 30;
              filters.transport = 'Fiets';
              filters.originLat = lat;
              filters.originLng = lng;
              filters.originLabel = label;
              window.jobsyBanenkaartCatalog.applyClientFilters(filters);
            }
            """,
            new { lat = DelftLat, lng = DelftLng, label = DelftLabel });

        await page.WaitForFunctionAsync(
            "() => window.location.search.includes('maxMinutes=30') && window.location.search.includes('originLat=')",
            null,
            new() { Timeout = 15_000 });

        var chip = await page.Locator("[data-testid=kb-active-tag-travel]").InnerTextAsync();
        Assert.Contains("30", chip, StringComparison.Ordinal);
        Assert.Contains("fiets", chip, StringComparison.OrdinalIgnoreCase);

        var sub = await page.Locator(".results-meta__sub").First.InnerTextAsync();
        Assert.Contains("30", sub, StringComparison.Ordinal);
        Assert.Contains("fiets", sub, StringComparison.OrdinalIgnoreCase);

        var address = await page.Locator("[data-kb-address]").First.InputValueAsync();
        Assert.Contains("Delft", address, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MaxMinutes_without_origin_has_no_map_marker_or_travel_rings()
    {
        var cookiePayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            maxTravelMinutes = 60,
            transport = "Auto",
            originLat = DelftLat,
            originLng = DelftLng,
            originLabel = DelftLabel,
        })));

        await using var context = await NewDesktopContextAsync();
        var host = new Uri(_baseUrl).Host;
        await context.AddCookiesAsync([
            new Cookie
            {
                Name = BanenkaartFilterCookie.CookieName,
                Value = cookiePayload,
                Domain = host,
                Path = "/",
                HttpOnly = true,
                Secure = _baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase),
            }
        ]);

        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + "/banenkaart?maxMinutes=30", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await WaitCatalogReadyAsync(page);
        await page.WaitForFunctionAsync(
            "() => window.jobMap && typeof window.jobMap.__testGetMap === 'function' && window.jobMap.__testGetMap()",
            null,
            new() { Timeout = 90_000 });

        var address = await page.Locator("[data-kb-address]").First.InputValueAsync();
        Assert.True(string.IsNullOrWhiteSpace(address));

        await page.WaitForFunctionAsync(
            """
            () => {
              const marker = document.querySelector('.job-map-origin');
              if (marker) return false;
              const map = window.jobMap.__testGetMap();
              if (!map || typeof map.getSource !== 'function') return false;
              return !map.getSource('jobsy-travel-rings');
            }
            """,
            null,
            new() { Timeout = 30_000 });
    }

    [Fact]
    public async Task Travel_chip_remove_at_30_min_clears_time_filter_and_keeps_address()
    {
        var path = $"/banenkaart?originLat={DelftLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            + $"&originLng={DelftLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            + $"&originLabel={Uri.EscapeDataString(DelftLabel)}&maxMinutes=30&transport=Fiets";

        await using var context = await NewDesktopContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + path, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await WaitCatalogReadyAsync(page);

        await page.Locator("[data-testid=kb-active-tag-travel]").ClickAsync();
        await page.WaitForFunctionAsync(
            """
            () => {
              const btn = document.querySelector('[data-testid=kb-active-tag-travel]');
              return btn && (btn.hidden || btn.style.display === 'none')
                && !window.location.search.includes('maxMinutes=')
                && window.jobsyBanenkaartCatalog?.resolveFilters?.()?.maxMinutes == null;
            }
            """,
            null,
            new() { Timeout = 15_000 });

        var address = await page.Locator("[data-kb-address]").First.InputValueAsync();
        Assert.Contains("Delft", address, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("originLat=", page.Url, StringComparison.Ordinal);
    }

    private Task<IBrowserContext> NewDesktopContextAsync()
        => _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1113 },
            IgnoreHTTPSErrors = true
        });

    private static async Task WaitCatalogReadyAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => window.jobsyBanenkaartCatalog?.isReady?.() === true",
            null,
            new() { Timeout = 90_000 });
    }
}
