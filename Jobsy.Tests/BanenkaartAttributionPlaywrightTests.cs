using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Lagen banenkaart: OSM attribution (i) must stay visible and hit-testable above legend / bottom nav.
/// </summary>
[Collection("BanenkaartPlaywright")]
public sealed class BanenkaartAttributionPlaywrightTests : IAsyncLifetime
{
    private const string MapPath =
        "/banenkaart?originLat=51.9944&originLng=4.2097&originLabel=Kasweg%2012%2C%202671%20AB%20Naaldwijk&maxMinutes=20&transport=Fiets";

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
    [InlineData(1440, 900, false)]
    [InlineData(390, 844, true)]
    public async Task Attribution_icon_is_hit_testable_at_viewport_center(int width, int height, bool touch)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + MapPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await WaitMapReadyAsync(page);
        await page.WaitForFunctionAsync(
            """
            () => {
              const host = document.querySelector('.jobsy-discovery--lagen .maplibregl-ctrl-bottom-left');
              return !!host && getComputedStyle(host).zIndex !== 'auto';
            }
            """,
            null,
            new() { Timeout = 30_000 });

        var probe = await page.EvaluateAsync<AttribProbe>(
            """
            () => {
              const host = document.querySelector('.jobsy-discovery--lagen .map-stage .maplibregl-ctrl-bottom-left');
              const root = host?.querySelector('.maplibregl-ctrl-attrib');
              if (!root) return { ok: false, reason: 'missing-attrib' };
              const r = root.getBoundingClientRect();
              if (r.width < 20 || r.height < 20) {
                return { ok: false, reason: 'too-small', width: r.width, height: r.height };
              }
              const x = r.left + r.width / 2;
              const y = r.top + r.height / 2;
              const hit = document.elementFromPoint(x, y);
              const ok = !!(hit && (root === hit || root.contains(hit)));
              return {
                ok,
                reason: ok ? 'hit' : 'obscured',
                x,
                y,
                width: r.width,
                height: r.height,
                hitTag: hit ? hit.tagName : null,
                hitClass: hit && hit.className ? String(hit.className) : null
              };
            }
            """);

        Assert.True(probe.Ok, System.Text.Json.JsonSerializer.Serialize(probe));
        Assert.True(probe.Width >= 24 && probe.Height >= 24, System.Text.Json.JsonSerializer.Serialize(probe));
    }

    [Fact]
    public async Task Mobile_first_load_shows_20_min_ring_and_label()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + MapPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await WaitMapReadyAsync(page);

        await page.WaitForFunctionAsync(
            """
            () => {
              const rings = window.jobMap?.__testGetTravelRingFeatureCount?.() ?? 0;
              const labels = document.querySelectorAll('.jobsy-discovery--lagen .map-iso-label.kb-rl').length;
              return rings >= 1 && labels >= 1;
            }
            """,
            null,
            new() { Timeout = 90_000 });
    }

    private static async Task WaitMapReadyAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => window.jobsyBanenkaartCatalog?.isReady?.() === true && document.querySelector('.jobsy-discovery--lagen .map-stage .maplibregl-ctrl-bottom-left .maplibregl-ctrl-attrib')",
            null,
            new() { Timeout = 90_000 });
        await page.WaitForTimeoutAsync(400);
    }

    private sealed class AttribProbe
    {
        public bool Ok { get; set; }
        public string Reason { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string? HitTag { get; set; }
        public string? HitClass { get; set; }
    }
}
