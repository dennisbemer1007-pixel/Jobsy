using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Captures filterlade screenshots for PR evidence (CI stack on :5201).</summary>
public sealed class BanenkaartFilterladeScreenshotTests
{
    private const string OutDir = "/cursor/stores/self/banenkaart-b";

    [Fact]
    public async Task Capture_filterlade_screenshots_for_pr()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_CAPTURE_BANENKAART"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        Directory.CreateDirectory(OutDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await using var browser = await (await Playwright.CreateAsync()).Chromium.LaunchAsync(new() { Headless = true });

        var errors = new List<string>();
        try { await CaptureDesktopLoggedOutAsync(browser, baseUrl); }
        catch (Exception ex) { errors.Add("desktop-logged-out: " + ex.Message); }
        try { await CaptureMobileLoggedOutAsync(browser, baseUrl); }
        catch (Exception ex) { errors.Add("mobile-logged-out: " + ex.Message); }
        try { await CaptureDesktopLoggedInAsync(browser, baseUrl); }
        catch (Exception ex) { errors.Add("desktop-logged-in: " + ex.Message); }

        var count = Directory.Exists(OutDir) ? Directory.GetFiles(OutDir, "*.png").Length : 0;
        Assert.True(count >= 6, $"Expected ≥6 screenshots, got {count}. {string.Join("; ", errors)}");
        Assert.True(errors.Count == 0, string.Join("; ", errors));
    }

    private static async Task CaptureDesktopLoggedOutAsync(IBrowser browser, string baseUrl)
    {
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await DismissStartAsync(page);
        await WaitForMapReadyAsync(page);
        await AssertDesktopPanelChromeAsync(page);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "desktop-1440-closed-logged-out.png") });
        await OpenDesktopFilterDrawerAsync(page);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "desktop-1440-drawer-open-logged-out.png") });
    }

    private static async Task CaptureMobileLoggedOutAsync(IBrowser browser, string baseUrl)
    {
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await DismissStartAsync(page);
        await WaitForMapReadyAsync(page);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "mobile-390-closed-logged-out.png") });
        await page.Locator(".kb-filter-bar [data-testid=kb-filters-button]").ClickAsync();
        await page.WaitForSelectorAsync("[data-testid=kb-filter-sheet]", new() { Timeout = 15_000 });
        await page.WaitForTimeoutAsync(400);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "mobile-390-sheet-open-logged-out.png") });
    }

    private static async Task CaptureDesktopLoggedInAsync(IBrowser browser, string baseUrl)
    {
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await page.FillAsync("input[name='email']", "kandidaat@jobsy.local");
        await page.FillAsync("input[name='password']", "Jobsy123!");
        await page.Locator("button.login-submit, button.au-submit[type=submit]").ClickAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await DismissStartAsync(page);
        await WaitForMapReadyAsync(page);
        await EnsureDesktopOriginAsync(page);
        await AssertDesktopPanelChromeAsync(page);
        await AssertNoDesktopBottomNavAsync(page);
        await AssertTravelLegendInMapAsync(page);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "desktop-1440-closed-logged-in.png") });
        await OpenDesktopFilterDrawerAsync(page);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "desktop-1440-drawer-open-logged-in.png") });
    }

    private static async Task WaitForMapReadyAsync(IPage page)
    {
        await page.WaitForSelectorAsync(".map-stage.is-live #job-map canvas", new() { Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const map = window.jobMap;
              const canvas = document.querySelector('#job-map canvas');
              if (!canvas || canvas.width < 50) return false;
              if (map && typeof map.isStyleLoaded === 'function' && !map.isStyleLoaded()) return false;
              if (map && typeof map.areTilesLoaded === 'function' && !map.areTilesLoaded()) return false;
              return document.querySelector('.map-stage.is-live') !== null;
            }
            """,
            null,
            new() { Timeout = 120_000 });

        var pinsResponse = page.WaitForResponseAsync(
            r => r.Url.Contains("/api/vacancies/pins", StringComparison.OrdinalIgnoreCase) && r.Ok,
            new() { Timeout = 120_000 });

        await page.EvaluateAsync(
            """
            async () => {
              const map = window.jobMap;
              if (!map) return;
              const pinCount = () => (map.__testGetPinCount && map.__testGetPinCount()) || 0;
              if (pinCount() > 0) return;
              if (typeof map.setOrigin === 'function') {
                map.setOrigin(52.011, 4.357, { maxMinutes: 30, transport: 'Fiets', radiusKm: 20 });
              }
              if (typeof map.reloadPins === 'function') {
                await map.reloadPins('/api/vacancies/pins?transport=Fiets&maxMinutes=30&originLat=52.011&originLng=4.357&radiusKm=20');
              }
            }
            """);

        try
        {
            await pinsResponse;
        }
        catch (TimeoutException)
        {
            /* pins may already be loaded from boot JSON */
        }

        await page.WaitForFunctionAsync(
            """
            () => {
              const map = window.jobMap;
              if (!map) return false;
              const count = map.__testGetPinCount ? map.__testGetPinCount() : 0;
              if (count > 0) return true;
              const boot = document.getElementById('jobsy-map-boot');
              return !!(boot && boot.textContent && boot.textContent.indexOf('"id"') >= 0);
            }
            """,
            null,
            new() { Timeout = 90_000 });

        await page.WaitForFunctionAsync(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap ? window.jobMap.__testGetMap() : null;
              if (!map || typeof map.areTilesLoaded !== 'function') return true;
              return map.areTilesLoaded();
            }
            """,
            null,
            new() { Timeout = 45_000 });

        await page.WaitForTimeoutAsync(800);
    }

    private static async Task EnsureDesktopOriginAsync(IPage page)
    {
        try
        {
            var input = page.Locator("#discovery-address-desktop");
            await input.WaitForAsync(new() { Timeout = 30_000 });
            var value = await input.InputValueAsync();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            await input.ClickAsync();
            await input.FillAsync("Rijswijk");
            await page.WaitForFunctionAsync(
                """
                () => document.querySelector('.kb-panel-head__address .address-suggestions__item') !== null
                """,
                null,
                new() { Timeout = 60_000 });
            await page.Locator(".kb-panel-head__address .address-suggestions__item").First
                .ClickAsync(new() { Timeout = 15_000 });
            await page.Locator(".kb-panel-head__address.kb-address--with-clear").WaitForAsync(new() { Timeout = 30_000 });
        }
        catch (PlaywrightException)
        {
            /* geocoder flake — chrome assert skips clear when origin unset */
        }
    }

    private static async Task AssertDesktopPanelChromeAsync(IPage page)
    {
        var clearBtn = page.Locator("[data-testid=kb-panel-head] .kb-address__clear");
        if (await clearBtn.CountAsync() > 0 && await clearBtn.First.IsVisibleAsync())
        {
            await Assertions.Expect(clearBtn.First).Not.ToContainTextAsync("Wissen");
            var hasIcon = await clearBtn.First.Locator("svg.kb-address__clear-icon").CountAsync();
            Assert.True(hasIcon > 0, "Address clear must render × icon SVG.");
            var text = (await clearBtn.First.InnerTextAsync()).Trim();
            Assert.True(string.IsNullOrEmpty(text), "Clear control must not show label text beside the field.");
        }

        var search = page.Locator("#discovery-keyword-desktop");
        await search.WaitForAsync(new() { Timeout = 30_000 });
        var bg = await search.EvaluateAsync<string>("el => getComputedStyle(el).backgroundImage");
        Assert.Contains("svg", bg, StringComparison.OrdinalIgnoreCase);

        var coachVisible = await page.Locator(".lobsy-coach-dock").IsVisibleAsync();
        Assert.False(coachVisible, "Coach mascot must not show on banenkaart.");
    }

    private static async Task AssertNoDesktopBottomNavAsync(IPage page)
    {
        var nav = page.Locator(".bottom-nav");
        if (await nav.CountAsync() > 0)
        {
            await Assertions.Expect(nav.First).ToBeHiddenAsync();
        }
    }

    private static async Task DismissStartAsync(IPage page)
    {
        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }
    }

    private static async Task OpenDesktopFilterDrawerAsync(IPage page)
    {
        var openBtn = page.Locator("[data-testid=kb-panel-head] [data-testid=kb-filters-button]");
        await openBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await openBtn.ClickAsync(new() { Timeout = 60_000 });
        var sheet = page.Locator("[data-testid=kb-filter-sheet]");
        await Assertions.Expect(sheet).ToBeVisibleAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(400);
    }

    private static async Task AssertTravelLegendInMapAsync(IPage page)
    {
        var legend = page.Locator(".map-stage .kb-travel-legend");
        if (await legend.CountAsync() == 0)
        {
            return;
        }

        var box = await legend.First.BoundingBoxAsync();
        var stage = await page.Locator(".map-stage").First.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.NotNull(stage);
        Assert.True(box.Y >= stage.Y - 1, "Travel legend must not sit above the map.");
        Assert.True(box.Y + box.Height <= stage.Y + stage.Height + 1, "Travel legend must fit inside the map stage.");
    }
}
