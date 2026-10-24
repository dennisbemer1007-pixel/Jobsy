using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Desktop lagen map strip: mouse wheel and drag scroll (mockup #878 has no arrow buttons).
/// </summary>
[Collection("BanenkaartPlaywright")]
public sealed class BanenkaartLagenDesktopStripScrollPlaywrightTests : IAsyncLifetime
{
    private const string BanenkaartPath =
        "/banenkaart?originLat=51.9944&originLng=4.2097&originLabel=Kasweg%2012%2C%202671%20AB%20Naaldwijk&maxMinutes=20&transport=Fiets&minHours=32&maxHours=40";

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
    public async Task Desktop_1280_strip_wheel_and_drag_scroll()
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

        var strip = page.Locator("[data-testid=kb-map-strip]");
        await strip.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const track = document.querySelector('[data-testid=kb-map-strip] .strip__track');
              return !!(track && track.querySelectorAll('.sc').length >= 3
                && document.querySelector('[data-testid=kb-map-strip]')?.getAttribute('data-kb-strip-scroll') === '1');
            }
            """,
            null,
            new() { Timeout = 60_000 });

        await page.EvaluateAsync(
            """
            () => {
              const shell = document.querySelector('.app-shell');
              if (!shell?.classList.contains('app-shell--banenkaart-shell')) {
                throw new Error('Expected app-shell--banenkaart-shell on banenkaart (guest + candidate).');
              }
              const track = document.querySelector('[data-testid=kb-map-strip] .strip__track');
              if (!track) return;
              if (track.scrollWidth <= track.clientWidth + 8) {
                throw new Error('Expected horizontal overflow on map strip track.');
              }
              track.style.scrollSnapType = 'none';
              track.scrollLeft = 0;
            }
            """);

        var track = page.Locator("[data-testid=kb-map-strip] .strip__track");
        var box = await track.BoundingBoxAsync();
        Assert.NotNull(box);
        var wheelX = (float)(box!.X + box.Width * 0.5);
        var wheelY = (float)(box.Y + box.Height * 0.5);
        await page.Mouse.MoveAsync(wheelX, wheelY);
        await page.Mouse.WheelAsync(0, 240);
        var afterWheel = await track.EvaluateAsync<double>("el => el.scrollLeft");
        Assert.True(afterWheel > 0, $"Expected wheel to scroll strip horizontally; scrollLeft={afterWheel}.");

        await track.EvaluateAsync("el => { el.scrollLeft = 0; }");
        var dragY = (float)(box.Y + box.Height * 0.5);
        var dragStartX = (float)(box.X + box.Width * 0.55);
        await page.Mouse.MoveAsync(dragStartX, dragY);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(dragStartX - 180, dragY);
        await page.Mouse.UpAsync();
        var afterDrag = await track.EvaluateAsync<double>("el => el.scrollLeft");
        Assert.True(afterDrag > 0, $"Expected drag to scroll strip; scrollLeft={afterDrag}.");

        var bound = await page.EvaluateAsync<bool>(
            "() => document.querySelector('[data-testid=kb-map-strip]')?.getAttribute('data-kb-strip-scroll') === '1'");
        Assert.True(bound, "Expected map strip scroll bindings after render.");
    }

    [Fact]
    public async Task Desktop_1280_strip_wheel_on_card_delta_modes()
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

        await page.Locator("[data-testid=kb-map-strip]").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const track = document.querySelector('[data-testid=kb-map-strip] .strip__track');
              return !!(track && track.querySelectorAll('.sc').length >= 3
                && track.scrollWidth > track.clientWidth + 8);
            }
            """,
            null,
            new() { Timeout = 60_000 });

        await page.EvaluateAsync(
            """
            () => {
              const track = document.querySelector('[data-testid=kb-map-strip] .strip__track');
              if (track) {
                track.style.scrollSnapType = 'none';
                track.scrollLeft = 0;
              }
            }
            """);

        var afterPixel = await page.EvaluateAsync<double>(
            "() => window.jobsyBanenkaartCatalog.__testStripWheelScroll(120, 0)");
        Assert.True(afterPixel > 0, $"Expected pixel-mode wheel on .sc to scroll strip; scrollLeft={afterPixel}.");

        await page.EvaluateAsync(
            """
            () => {
              const track = document.querySelector('[data-testid=kb-map-strip] .strip__track');
              if (track) track.scrollLeft = 0;
            }
            """);

        var afterLine = await page.EvaluateAsync<double>(
            "() => window.jobsyBanenkaartCatalog.__testStripWheelScroll(3, 1)");
        Assert.True(afterLine > 0, $"Expected line-mode wheel on .sc to scroll strip; scrollLeft={afterLine}.");
    }

    [Fact]
    public async Task Desktop_1280_strip_click_after_drag_opens_vacancy()
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

        var track = page.Locator("[data-testid=kb-map-strip] .strip__track");
        await track.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const track = document.querySelector('[data-testid=kb-map-strip] .strip__track');
              return !!(track && track.querySelectorAll('.sc').length >= 2
                && track.scrollWidth > track.clientWidth + 8);
            }
            """,
            null,
            new() { Timeout = 60_000 });

        var box = await track.BoundingBoxAsync();
        Assert.NotNull(box);
        var dragY = (float)(box!.Y + box.Height * 0.5);
        var dragStartX = (float)(box.X + box.Width * 0.55);
        await page.Mouse.MoveAsync(dragStartX, dragY);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(dragStartX - 200, dragY);
        await page.Mouse.UpAsync();

        var card = page.Locator("[data-testid=kb-map-strip] .sc").First;
        await card.ClickAsync(new() { Timeout = 15_000 });

        await page.Locator(".maplibregl-popup.kb-popup-lagen-pop").First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 45_000
        });
    }
}
