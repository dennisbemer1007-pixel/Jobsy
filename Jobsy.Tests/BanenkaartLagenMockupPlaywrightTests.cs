using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Pixel-structure gate for PR #878 lagen mockup: element presence + screenshots vs docs/mockups PNGs.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartLagenMockupPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _baseUrl = "";
    private static readonly string ArtifactDir = "/opt/cursor/artifacts/banenkaart-lagen-mockup";
    private static readonly string ElementTablePath = "/opt/cursor/artifacts/banenkaart-lagen-element-table.json";
    private static readonly string MockupRoot = BanenkaartLagenComparePlaywrightTests.MockupRefRoot + "/lagen";

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        Directory.CreateDirectory(ArtifactDir);
        _web = new IllustratedPagesWebFactory();
        _baseUrl = _web.ServerAddress.TrimEnd('/');
        await BanenkaartLagenComparePlaywrightTests_EnsurePublished.EnsurePublishedAsync();
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
    [InlineData("desktop", 1280, 720, false, "desktop/02-banenkaart-lagen-desktop-1440.png")]
    [InlineData("mobile", 390, 844, true, "mobile/01-banenkaart-lagen-mobiel-390.png")]
    public async Task Lagen_mockup_element_table_and_screenshot(
        string tag,
        int width,
        int height,
        bool touch,
        string mockupRelative)
    {
        await RunProfileAsync(tag + "-fresh", width, height, touch, mockupRelative, seedLegacyUi: false);
        await RunProfileAsync(tag + "-legacy-ui", width, height, touch, mockupRelative, seedLegacyUi: true);
    }

    [Fact]
    public async Task Lagen_mobile_legend_sheet_and_button_interactions()
    {
        await using var context = await NewContextAsync(390, 844, true);
        var page = await OpenBanenkaartLoggedInAsync(context);
        await WaitForCatalogAsync(page);

        var legbtn = page.Locator("[data-testid=kb-map-legbtn]");
        await legbtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        var legText = await legbtn.InnerTextAsync();
        Assert.Contains("Uitleg kaart", legText, StringComparison.Ordinal);
        await legbtn.ClickAsync();
        await page.WaitForFunctionAsync(
            "() => document.body.classList.contains('s-legend')",
            null,
            new() { Timeout = 15_000 });
        var sheetText = await page.Locator("[data-testid=kb-map-legend-sheet]").InnerTextAsync();
        Assert.Contains("10 min fietsen", sheetText, StringComparison.Ordinal);
        await page.Locator("[data-testid=kb-map-legend-done]").ClickAsync();
        await page.WaitForFunctionAsync(
            "() => !document.body.classList.contains('s-legend')",
            null,
            new() { Timeout = 15_000 });

        await page.Locator("[data-testid=kb-view-lijst]").ClickAsync();
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              return root && root.classList.contains('show-list');
            }
            """,
            null,
            new() { Timeout = 15_000 });
        Assert.True(await page.Locator(".vc").First.IsVisibleAsync());
        await page.Locator("[data-testid=kb-view-kaart]").ClickAsync();
    }

    private async Task RunProfileAsync(
        string profile,
        int width,
        int height,
        bool touch,
        string mockupRelative,
        bool seedLegacyUi)
    {
        await using var context = await NewContextAsync(width, height, touch);
        if (seedLegacyUi)
        {
            await context.AddInitScriptAsync(
                """
                () => {
                  try {
                    sessionStorage.setItem('jobsy.bk.ui.v1', JSON.stringify({ showMap: false, desktopList: true, filtersOpen: false }));
                  } catch (e) {}
                }
                """);
        }

        var page = await OpenBanenkaartLoggedInAsync(context);
        await WaitForCatalogAsync(page);

        if (width >= 900)
        {
            await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load });
            await page.WaitForFunctionAsync(
                "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.dataset?.banenkaartSsrBound === '1'",
                null,
                new() { Timeout = 90_000 });
            await DismissStartPromptAsync(page);
            await WaitForCatalogAsync(page);
            var mapH = await page.EvaluateAsync<double>(
                """
                () => {
                  const c = document.querySelector('#job-map .maplibregl-canvas');
                  return c ? c.getBoundingClientRect().height : 0;
                }
                """);
            Assert.True(mapH > 320, profile + ": map canvas height after hard reload should exceed 320px, got " + mapH);
        }

        var rows = await BuildElementTableAsync(page, width);
        var failures = rows.Where(r => !r.Pass).ToList();
        await AppendElementTableAsync(profile, width, height, rows);

        var shotPath = Path.Combine(ArtifactDir, $"banenkaart-lagen-{profile}.png");
        await page.ScreenshotAsync(new() { Path = shotPath, FullPage = false });
        var mockupPath = Path.Combine(MockupRoot, mockupRelative);
        Assert.True(File.Exists(mockupPath), "Missing mockup reference PNG: " + mockupPath);
        Assert.True(File.Exists(shotPath));

        Assert.True(failures.Count == 0, profile + " element failures: " + string.Join("; ", failures.Select(f => f.Id + ": " + f.Note)));
    }

    private static async Task<List<ElementRow>> BuildElementTableAsync(IPage page, int width)
    {
        var checks = new (string Id, string Selector, string? TextContains, bool DesktopOnly, bool MobileOnly)[]
        {
            ("root-lagen", "[data-banenkaart-ssr=\"1\"].jobsy-discovery--lagen", null, false, false),
            ("header-strip", "[data-testid=kb-nearby-banner].kb-banenkaart-header-strip", null, false, false),
            ("map-canvas", "#job-map .maplibregl-canvas", null, false, false),
            ("strip-cards", ".strip .sc", null, false, false),
            ("list-vc", ".vc", null, false, false),
            ("vc-meta", ".vc .meta span", null, false, false),
            ("result-vacatures", "[data-testid=kb-desktop-result-count]", "vacatures", width >= 900, false),
            ("result-vacatures-m", "[data-testid=kb-mobile-result-count]", "vacatures", false, width < 900),
            ("result-sub-binnen", ".results-meta__sub", "binnen", width >= 900, false),
            ("view-seg-kaart", "[data-testid=kb-view-kaart]", "Kaart", false, false),
            ("view-seg-lijst", "[data-testid=kb-view-lijst]", "Lijst", false, false),
            ("legend-desktop", "[data-testid=kb-map-legend]", "Wat zie je op de kaart?", true, false),
            ("legbtn-mobile", "[data-testid=kb-map-legbtn]", "Uitleg kaart", false, true),
            ("legend-rings", ".legend .ln", null, width >= 900, false),
            ("bottom-nav-full", "[data-testid=candidate-bottom-nav-ssr], nav.bottom-nav", null, false, false),
            ("no-header-nav", ".candidate-header-nav", null, false, false),
        };

        var rows = new List<ElementRow>();
        foreach (var c in checks)
        {
            if (c.DesktopOnly && width < 900)
            {
                continue;
            }

            if (c.MobileOnly && width >= 900)
            {
                continue;
            }

            var loc = page.Locator(c.Selector);
            var count = await loc.CountAsync();
            bool pass;
            string? note = null;
            if (c.Id == "no-header-nav")
            {
                pass = count == 0 || !await loc.First.IsVisibleAsync();
                if (!pass)
                {
                    note = "header nav links must not show on banenkaart";
                }
            }
            else if (c.Id == "bottom-nav-full")
            {
                pass = count > 0 && await loc.First.IsVisibleAsync();
                if (pass)
                {
                    var box = await loc.First.BoundingBoxAsync();
                    var vp = page.ViewportSize;
                    pass = box is not null && vp is not null && box.Width >= vp.Width * 0.95;
                    if (!pass)
                    {
                        note = "bottom nav should span full viewport width";
                    }
                }
                else
                {
                    note = "bottom nav missing";
                }
            }
            else
            {
                var visible = count > 0 && await loc.First.IsVisibleAsync();
                pass = visible;
                if (!visible)
                {
                    note = "not visible";
                }
            }

            if (pass && !string.IsNullOrEmpty(c.TextContains) && c.Id is not "no-header-nav" and not "bottom-nav-full")
            {
                var text = await loc.First.InnerTextAsync();
                pass = text.Contains(c.TextContains, StringComparison.OrdinalIgnoreCase);
                if (!pass)
                {
                    note = "text mismatch: " + text;
                }
            }

            if (pass && width >= 900 && c.Id == "map-canvas")
            {
                var clear = await page.EvaluateAsync<bool>(
                    """
                    () => {
                      const nav = document.querySelector('[data-testid=candidate-bottom-nav-ssr], nav.bottom-nav');
                      const canvas = document.querySelector('#job-map .maplibregl-canvas');
                      if (!nav || !canvas) return false;
                      const nb = nav.getBoundingClientRect();
                      const cb = canvas.getBoundingClientRect();
                      return cb.bottom <= nb.top + 2;
                    }
                    """);
                if (!clear)
                {
                    pass = false;
                    note = "map extends under bottom nav";
                }
            }

            rows.Add(new ElementRow(c.Id, c.Selector, pass, note));
        }

        return rows;
    }

    private static async Task AppendElementTableAsync(string profile, int width, int height, List<ElementRow> rows)
    {
        List<object> all = [];
        if (File.Exists(ElementTablePath))
        {
            try
            {
                all = JsonSerializer.Deserialize<List<object>>(await File.ReadAllTextAsync(ElementTablePath)) ?? [];
            }
            catch (JsonException)
            {
                all = [];
            }
        }

        all.Add(new
        {
            profile,
            width,
            height,
            at = DateTime.UtcNow.ToString("O"),
            rows = rows.Select(r => new { r.Id, r.Selector, r.Pass, r.Note }),
            passCount = rows.Count(r => r.Pass),
            total = rows.Count,
        });
        await File.WriteAllTextAsync(ElementTablePath, JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<IBrowserContext> NewContextAsync(int width, int height, bool touch)
    {
        return await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });
    }

    private async Task<IPage> OpenBanenkaartLoggedInAsync(IBrowserContext context)
    {
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await page.GotoAsync(_baseUrl + "/__test/sign-in?role=candidate&return=" + Uri.EscapeDataString("/banenkaart"));
        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.dataset?.banenkaartSsrBound === '1'",
            null,
            new() { Timeout = 90_000 });
        await DismissStartPromptAsync(page);
        return page;
    }

    private static async Task WaitForCatalogAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => {
              const strip = document.querySelector('.strip .sc');
              const list = document.querySelector('.vc');
              const map = document.querySelector('#job-map .maplibregl-canvas');
              return !!(strip && list && map && map.getBoundingClientRect().height > 80);
            }
            """,
            null,
            new() { Timeout = 120_000 });
    }

    private static async Task DismissStartPromptAsync(IPage page)
    {
        var later = page.Locator("[data-testid=kb-start-later]");
        if (await later.CountAsync() > 0)
        {
            await later.First.ClickAsync(new() { Timeout = 5_000 });
        }
    }

    private sealed record ElementRow(string Id, string Selector, bool Pass, string? Note);
}

internal static class BanenkaartLagenComparePlaywrightTests_EnsurePublished
{
    internal static Task EnsurePublishedAsync() =>
        BanenkaartLagenComparePlaywrightTests_EnsurePublishedCore.EnsureAsync();

    private static class BanenkaartLagenComparePlaywrightTests_EnsurePublishedCore
    {
        internal static async Task EnsureAsync()
        {
            var dll = Path.Combine(BanenkaartLagenComparePlaywrightTests.PublishDir, "Jobsy.Web.dll");
            if (File.Exists(dll) && File.GetLastWriteTimeUtc(dll) > DateTime.UtcNow.AddHours(-3))
            {
                return;
            }

            if (Directory.Exists(BanenkaartLagenComparePlaywrightTests.PublishDir))
            {
                Directory.Delete(BanenkaartLagenComparePlaywrightTests.PublishDir, recursive: true);
            }

            Directory.CreateDirectory("/workspace/tmp");
            var root = Directory.GetCurrentDirectory();
            while (!File.Exists(Path.Combine(root, "Jobsy.sln")) && Directory.GetParent(root) is not null)
            {
                root = Directory.GetParent(root)!.FullName;
            }

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = root,
                ArgumentList =
                {
                    "publish",
                    "Jobsy.Web/Jobsy.Web.csproj",
                    "-c",
                    "Release",
                    "-o",
                    BanenkaartLagenComparePlaywrightTests.PublishDir,
                    "/p:UseAppHost=false",
                },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var pub = System.Diagnostics.Process.Start(psi)!;
            var stderr = await pub.StandardError.ReadToEndAsync();
            await pub.WaitForExitAsync();
            Assert.True(pub.ExitCode == 0, "publish failed: " + stderr);
        }
    }
}
