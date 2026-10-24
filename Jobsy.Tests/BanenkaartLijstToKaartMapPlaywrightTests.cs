using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// SSR Lijst load then Kaart toggle: map must fill the pane, show tiles/pins, and open a popup on pin tap.
/// </summary>
public sealed class BanenkaartLijstToKaartMapPlaywrightTests : IAsyncLifetime
{
    private const string DelftListPath =
        "/banenkaart?maxMinutes=60&originLat=51.99389784&originLng=4.37897242&originLabel=Delft%2C%20Delft&weergave=lijst";

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
    public async Task Ssr_lijst_then_kaart_shows_full_map_pins_and_popup()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + DelftListPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            "() => window.jobsyBanenkaartCatalog?.isReady?.() === true",
            null,
            new() { Timeout = 90_000 });

        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root && root.getAttribute('data-kb-view') === 'list';
            }
            """,
            null,
            new() { Timeout = 15_000 });

        var desktopToggle = page.Locator("[data-testid=kb-view-toggle][data-kb-view-toggle-desktop=\"1\"]");
        await desktopToggle.Locator("[data-testid=kb-view-kaart]").ClickAsync(new() { Force = true });

        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root && root.getAttribute('data-kb-view') === 'map';
            }
            """,
            null,
            new() { Timeout = 15_000 });

        await page.WaitForFunctionAsync(
            """
            () => {
              const pane = document.querySelector('.jobsy-discovery[data-banenkaart-ssr="1"] .map-pane');
              const canvas = document.querySelector('#job-map .maplibregl-canvas');
              if (!pane || !canvas) return false;
              const pr = pane.getBoundingClientRect();
              const cr = canvas.getBoundingClientRect();
              if (pr.width < 200 || cr.width < 200) return false;
              return cr.width >= pr.width * 0.75 && cr.height >= 180;
            }
            """,
            null,
            new() { Timeout = 30_000 });

        await page.WaitForFunctionAsync(
            """
            () => {
              const data = window.jobMap?.__testGetPinCount?.() ?? 0;
              const rendered = window.jobMap?.__testGetRenderedPinCount?.() ?? 0;
              return data >= 20 && rendered >= 1;
            }
            """,
            null,
            new() { Timeout = 45_000 });

        await page.EvaluateAsync("() => window.jobMap?.__testTapLargestCluster?.()");
        await page.Locator(".maplibregl-popup.kb-popup-lagen-pop, .map-cluster-card, .job-map-popup").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20_000 });
    }
}
