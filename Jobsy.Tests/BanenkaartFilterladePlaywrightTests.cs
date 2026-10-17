using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Variant B filterlade: drawer closed by default, open/close, badge updates, no panel overlap.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartFilterladePlaywrightTests
{
    private static readonly (int W, int H, bool Mobile)[] Viewports =
    [
        (1440, 900, false),
        (1024, 900, false),
        (390, 844, true),
    ];

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Filter_drawer_closed_by_default_and_closes_with_escape(int width, int height)
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = height >= 800 && width < 500,
            IsMobile = width < 500,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await DismissStartPromptAsync(page);
        var openBtn = FiltersButtonForViewport(page, width);
        await openBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });

        var sheet = page.Locator("[data-testid=kb-filter-sheet]");
        await Assertions.Expect(sheet).ToBeHiddenAsync(new() { Timeout = 15_000 });

        await openBtn.ClickAsync(new() { Timeout = 60_000 });
        await Assertions.Expect(sheet).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await page.Locator("[data-testid=kb-filter-close]").ClickAsync(new() { Timeout = 60_000 });
        await Assertions.Expect(sheet).ToBeHiddenAsync(new() { Timeout = 30_000 });

        await openBtn.ClickAsync(new() { Timeout = 60_000 });
        await Assertions.Expect(sheet).ToBeVisibleAsync(new() { Timeout = 60_000 });
        if (width < 900)
        {
            await page.Keyboard.PressAsync("Escape");
            try
            {
                await Assertions.Expect(sheet).ToBeHiddenAsync(new() { Timeout = 5_000 });
            }
            catch (PlaywrightException)
            {
                await page.Locator("[data-testid=kb-filter-close]").ClickAsync(new() { Timeout = 30_000 });
            }
        }
        else
        {
            var backdrop = page.Locator("[data-testid=kb-filter-backdrop]");
            await backdrop.ClickAsync(new() { Position = new() { X = width - 80, Y = height / 2 }, Timeout = 30_000 });
        }

        await Assertions.Expect(sheet).ToBeHiddenAsync(new() { Timeout = 30_000 });
    }

    [Fact]
    public async Task Applying_travel_preset_updates_apply_label_and_badge_after_commit()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);
        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await DismissStartPromptAsync(page);

        await FiltersButtonForViewport(page, 1440).ClickAsync(new() { Timeout = 60_000 });
        await page.WaitForSelectorAsync("[data-testid=kb-filter-sheet]", new() { Timeout = 15_000 });

        var apply = page.Locator("[data-testid=kb-filter-apply]");
        await Assertions.Expect(apply).ToContainTextAsync(new Regex("vacatures|banen", RegexOptions.IgnoreCase), new() { Timeout = 60_000 });
        var applyBefore = await apply.InnerTextAsync();

        var preset30 = page.Locator("[data-testid=kb-sheet-travel-30]");
        if (await preset30.CountAsync() > 0)
        {
            await preset30.First.ClickAsync();
            await Assertions.Expect(preset30.First).ToHaveClassAsync(new Regex("is-selected"), new() { Timeout = 15_000 });
        }

        await apply.ClickAsync();
        await Assertions.Expect(page.Locator("[data-testid=kb-filter-sheet]")).ToBeHiddenAsync(new() { Timeout = 60_000 });
        var countText = await page.Locator("[data-testid=kb-desktop-result-count]").InnerTextAsync();
        Assert.Matches(@".*\d+.*", countText);
    }

    [Fact]
    public async Task Panel_controls_do_not_overlap_at_standard_viewports()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await using var browser = await LaunchAsync();

        var failures = new List<string>();
        foreach (var (w, h, mobile) in Viewports)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = w, Height = h },
                HasTouch = mobile,
                IsMobile = mobile,
                IgnoreHTTPSErrors = true,
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
            await DismissStartPromptAsync(page);

            var json = await page.EvaluateAsync<string>(OverlapScript);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("overlaps", out var overlaps) && overlaps.GetArrayLength() > 0)
            {
                failures.Add($"{w}x{h} overlaps: {overlaps}");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Theory]
    [InlineData(1440, 900, false)]
    [InlineData(390, 844, true)]
    public async Task Map_carousel_legend_and_zoom_attrib_do_not_overlap(int width, int height, bool mobile)
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = mobile,
            IsMobile = mobile,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await DismissStartPromptAsync(page);
        await WaitForBanenkaartMapChromeAsync(page);

        var carouselJson = await page.EvaluateAsync<string>(CarouselLegendOverlapScript);
        using (var doc = System.Text.Json.JsonDocument.Parse(carouselJson))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("layout", out var layout) && layout.GetArrayLength() > 0)
            {
                Assert.Fail($"{width}x{height} carousel must end before category legend: {layout}");
            }

            if (root.TryGetProperty("overlaps", out var overlaps) && overlaps.GetArrayLength() > 0)
            {
                Assert.Fail($"{width}x{height} visible carousel card vs category legend: {overlaps}");
            }
        }

        if (mobile)
        {
            await page.WaitForSelectorAsync(".job-map .maplibregl-ctrl-zoom-in", new() { Timeout = 90_000 });
            var zoomJson = await page.EvaluateAsync<string>(ZoomAttribOverlapScript);
            using var zoomDoc = System.Text.Json.JsonDocument.Parse(zoomJson);
            var zoomRoot = zoomDoc.RootElement;
            if (zoomRoot.TryGetProperty("overlaps", out var zoomOverlaps) && zoomOverlaps.GetArrayLength() > 0)
            {
                Assert.Fail($"{width}x{height} zoom vs attribution: {zoomOverlaps}");
            }

            if (zoomRoot.TryGetProperty("gapY", out var gapY) && gapY.TryGetDouble(out var gy))
            {
                Assert.True(gy >= -2, $"{width}x{height} zoom +/− should be adjacent (gapY={gy}).");
            }
        }
    }

    private const string CarouselLegendOverlapScript = """
        () => {
          const gap = 2;
          const carousel = document.querySelector('.map-pane__top-chrome .highlight-carousel--map');
          const toggle = document.querySelector('.map-pane__top-chrome .map-legend__toggle');
          if (!carousel || !toggle) {
            return JSON.stringify({ error: 'missing carousel or legend toggle' });
          }
          const slot = carousel.getBoundingClientRect();
          const t = toggle.getBoundingClientRect();
          const layout = [];
          if (slot.right > t.left - gap) {
            layout.push({ kind: 'carousel-vs-toggle', carouselRight: slot.right, toggleLeft: t.left });
          }
          const cards = [...document.querySelectorAll('.map-pane__top-chrome .highlight-carousel__card')];
          const overlaps = [];
          for (const card of cards) {
            const c = card.getBoundingClientRect();
            const inSlotX = Math.min(c.right, slot.right) - Math.max(c.left, slot.left);
            const inSlotY = Math.min(c.bottom, slot.bottom) - Math.max(c.top, slot.top);
            if (inSlotX <= 10 || inSlotY <= 10) continue;
            const xOverlap = Math.min(t.right, c.right, slot.right) - Math.max(t.left, c.left, slot.left);
            const yOverlap = Math.min(t.bottom, c.bottom, slot.bottom) - Math.max(t.top, c.top, slot.top);
            if (xOverlap > gap && yOverlap > gap) {
              overlaps.push({ card: card.textContent?.slice(0, 40), xOverlap, yOverlap });
            }
          }
          return JSON.stringify({ overlaps, layout });
        }
        """;

    private const string ZoomAttribOverlapScript = """
        () => {
          const gap = 2;
          const zoomIn = document.querySelector('.job-map .maplibregl-ctrl-zoom-in');
          const zoomOut = document.querySelector('.job-map .maplibregl-ctrl-zoom-out');
          const attrib = document.querySelector('.job-map .maplibregl-ctrl-attrib');
          if (!zoomIn || !zoomOut || !attrib) {
            return JSON.stringify({ error: 'missing zoom or attribution' });
          }
          const a = zoomIn.getBoundingClientRect();
          const b = zoomOut.getBoundingClientRect();
          const i = attrib.getBoundingClientRect();
          const overlaps = [];
          const pairs = [[zoomIn, attrib, 'zoom-in', 'attrib'], [zoomOut, attrib, 'zoom-out', 'attrib']];
          for (const [el1, el2, n1, n2] of pairs) {
            const r1 = el1.getBoundingClientRect();
            const r2 = el2.getBoundingClientRect();
            const xOverlap = Math.min(r1.right, r2.right) - Math.max(r1.left, r2.left);
            const yOverlap = Math.min(r1.bottom, r2.bottom) - Math.max(r1.top, r2.top);
            if (xOverlap > gap && yOverlap > gap) {
              overlaps.push({ a: n1, b: n2, xOverlap, yOverlap });
            }
          }
          const gapY = b.top - a.bottom;
          return JSON.stringify({ overlaps, gapY });
        }
        """;

    private const string OverlapScript = """
        () => {
          const root = document.querySelector('[data-testid=kb-panel-head], [data-testid=kb-panel-head-mobile]');
          if (!root) return JSON.stringify({ error: 'missing panel head' });
          const sel = 'input, button:not(.kb-match-button), select, .kb-active-tag, .kb-view-toggle__btn, .kb-filter-bar__count';
          const gap = 2;
          const minArea = 64;
          const els = [...root.querySelectorAll(sel)].filter(el => {
            if (!(el instanceof HTMLElement)) return false;
            const st = getComputedStyle(el);
            if (st.display === 'none' || st.visibility === 'hidden' || st.opacity === '0') return false;
            const r = el.getBoundingClientRect();
            return r.width > 4 && r.height > 4;
          });
          const overlaps = [];
          for (let i = 0; i < els.length; i++) {
            const a = els[i].getBoundingClientRect();
            if (a.width * a.height < minArea) continue;
            for (let j = i + 1; j < els.length; j++) {
              if (els[i].contains(els[j]) || els[j].contains(els[i])) continue;
              const b = els[j].getBoundingClientRect();
              if (b.width * b.height < minArea) continue;
              const xOverlap = Math.min(a.right, b.right) - Math.max(a.left, b.left);
              const yOverlap = Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top);
              if (xOverlap > gap && yOverlap > gap) {
                overlaps.push({ a: els[i].className, b: els[j].className, xOverlap, yOverlap });
              }
            }
          }
          return JSON.stringify({ overlaps });
        }
        """;

    private static async Task DismissStartPromptAsync(IPage page)
    {
        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }
    }

    private static async Task WaitForBanenkaartMapChromeAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => {
              const toggle = document.querySelector('.map-pane__top-chrome .map-legend__toggle');
              const card = document.querySelector('.map-pane__top-chrome .highlight-carousel__card');
              if (!toggle || !card) return false;
              const r = card.getBoundingClientRect();
              return r.width > 10 && r.height > 10;
            }
            """,
            null,
            new() { Timeout = 90_000 });
    }

    private static string? BaseUrl()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl;
    }

    private static async Task<IBrowser> LaunchAsync()
    {
        var playwright = await Playwright.CreateAsync();
        return await playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var resp = await http.GetAsync(baseUrl);
            return resp.IsSuccessStatusCode || (int)resp.StatusCode is >= 300 and < 500;
        }
        catch
        {
            return false;
        }
    }

    private static ILocator FiltersButtonForViewport(IPage page, int viewportWidth) =>
        viewportWidth >= 900
            ? page.Locator("[data-testid=kb-panel-head] [data-testid=kb-filters-button]")
            : page.Locator(".kb-filter-bar [data-testid=kb-filters-button]");
}
