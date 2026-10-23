using System.Diagnostics;
using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Generates side-by-side mockup comparisons and button checklist under
/// <c>/opt/cursor/artifacts/banenkaart-lagen-compare/</c> (not committed).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartLagenComparePlaywrightTests : IAsyncLifetime
{
    public const string PublishDir = "/workspace/tmp/app-publish";
    public const string CompareDir = "/opt/cursor/artifacts/banenkaart-lagen-compare";
    public const string MockupRefRoot = CompareDir + "/mockup-ref/docs/mockups/kandidaat/banenkaart";
    private const string MockupCommit = "dd7cb7ce";

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IllustratedPagesWebFactory? _web;
    private string _baseUrl = "";

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        Directory.CreateDirectory(CompareDir);
        await EnsurePublishedToWorkspaceTmpAsync();
        await ExtractMockupRefsAsync();
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
    public async Task Generate_lagen_compare_artifacts_and_button_checklist()
    {
        var buttons = new List<object>();
        var elementTables = new List<object>();
        try
        {
        await CaptureViewportAsync(1280, 720, false, "desktop-1280-map-list", async (page, dir) =>
        {
            await WaitReadyAsync(page);
            var twoCol = await page.EvaluateAsync<bool>(
                """
                () => {
                  const cards = document.querySelectorAll('.vacancy-list .vc');
                  if (cards.length < 2) return false;
                  const list = document.querySelector('.vacancy-list');
                  if (!list) return false;
                  const s = getComputedStyle(list);
                  return s.display === 'grid' && s.gridTemplateColumns.split(' ').length >= 2;
                }
                """);
            buttons.Add(new { action = "desktop-list-2-columns", pass = twoCol });
            await ShotAsync(page, dir, "01-map-list.png");
            await SideBySideAsync(
                Path.Combine(MockupRefRoot, "lagen/desktop/02-banenkaart-lagen-desktop-1440.png"),
                Path.Combine(dir, "01-map-list.png"),
                Path.Combine(CompareDir, "side-by-side-desktop-1280-map-list.png"));
            await SideBySideAsync(
                Path.Combine(MockupRefRoot, "header/desktop.png"),
                Path.Combine(dir, "01-map-list.png"),
                Path.Combine(CompareDir, "side-by-side-header-c-desktop-1280.png"));
            elementTables.Add(await BuildElementTableAsync(page, 1280, "desktop-1280-map-list"));
        });

        await CaptureViewportAsync(1280, 720, false, "desktop-1280-popup", async (page, dir) =>
        {
            await WaitReadyAsync(page);
            var opened = await OpenFirstPinPopupAsync(page);
            buttons.Add(new { action = "employer-pin-popup", pass = opened });
            if (opened)
            {
                await page.WaitForTimeoutAsync(500);
                await ShotAsync(page, dir, "02-popup.png");
                await SideBySideAsync(
                    Path.Combine(MockupRefRoot, "lagen/desktop/01-banenkaart-lagen-desktop-1440-popup.png"),
                    Path.Combine(dir, "02-popup.png"),
                    Path.Combine(CompareDir, "side-by-side-desktop-1280-popup.png"));
                var cta = page.Locator(".maplibregl-popup, .kb-popup-lagen").Filter(new() { HasText = "Bekijk vacature" });
                buttons.Add(new { action = "popup-bekijk-vacature-visible", pass = await cta.CountAsync() > 0 });
            }
            else
            {
                await ShotAsync(page, dir, "02-map-no-popup.png");
            }
        });

        await CaptureViewportAsync(390, 844, true, "mobile-390-map", async (page, dir) =>
        {
            await WaitReadyAsync(page);
            await ShotAsync(page, dir, "01-map.png");
            await SideBySideAsync(
                Path.Combine(MockupRefRoot, "lagen/mobile/01-banenkaart-lagen-mobiel-390.png"),
                Path.Combine(dir, "01-map.png"),
                Path.Combine(CompareDir, "side-by-side-mobile-390-map.png"));
            await SideBySideAsync(
                Path.Combine(MockupRefRoot, "header/mobile.png"),
                Path.Combine(dir, "01-map.png"),
                Path.Combine(CompareDir, "side-by-side-header-c-mobile-390.png"));
            elementTables.Add(await BuildElementTableAsync(page, 390, "mobile-390-map"));
        });

        await CaptureViewportAsync(390, 844, true, "mobile-390-uitleg", async (page, dir) =>
        {
            await WaitReadyAsync(page);
            await page.Locator("[data-testid=kb-map-legbtn]").ClickAsync();
            await page.WaitForFunctionAsync("() => document.body.classList.contains('s-legend')", null, new() { Timeout = 30_000 });
            buttons.Add(new { action = "uitleg-kaart-sheet", pass = true });
            await ShotAsync(page, dir, "02-uitleg.png");
            await SideBySideAsync(
                Path.Combine(MockupRefRoot, "lagen/mobile/02-banenkaart-lagen-mobiel-390-uitleg.png"),
                Path.Combine(dir, "02-uitleg.png"),
                Path.Combine(CompareDir, "side-by-side-mobile-390-uitleg.png"));
        });

        await CaptureViewportAsync(390, 844, true, "mobile-390-vacature-sheet", async (page, dir) =>
        {
            await WaitReadyAsync(page);
            var opened = await OpenFirstPinPopupAsync(page);
            buttons.Add(new { action = "mobile-pin-popup-sheet", pass = opened });
            if (opened)
            {
                await page.WaitForTimeoutAsync(400);
                await ShotAsync(page, dir, "03-vacature.png");
                await SideBySideAsync(
                    Path.Combine(MockupRefRoot, "lagen/mobile/03-banenkaart-lagen-mobiel-390-vacature.png"),
                    Path.Combine(dir, "03-vacature.png"),
                    Path.Combine(CompareDir, "side-by-side-mobile-390-vacature.png"));
            }
        });

        await CaptureViewportAsync(1280, 720, false, "desktop-1280-interactions", async (page, _) =>
        {
            await WaitReadyAsync(page);
            buttons.Add(new { action = "map-pan", pass = await page.EvaluateAsync<bool>("() => { const m = window.jobMap?.__testGetMap?.(); if(!m) return false; const c=m.getCenter(); m.panBy([40,0],{duration:0}); const c2=m.getCenter(); return Math.abs(c2.lng-c.lng)>0; }") });
            buttons.Add(new { action = "cluster-zoom-tap", pass = await page.EvaluateAsync<bool>("() => !!(window.jobMap && window.jobMap.__testTapLargestCluster && window.jobMap.__testTapLargestCluster())") });
            var zoomIn = await page.EvaluateAsync<bool>("() => { const m=window.jobMap?.__testGetMap?.(); if(!m)return false; const z0=m.getZoom(); m.zoomIn({duration:0}); return m.getZoom()>z0; }");
            buttons.Add(new { action = "map-zoom-plus", pass = zoomIn });
            var viewLijst = page.Locator("[data-testid=kb-view-toggle-desktop] [data-testid=kb-view-lijst]");
            var viewKaart = page.Locator("[data-testid=kb-view-toggle-desktop] [data-testid=kb-view-kaart]");
            try
            {
                await viewLijst.ClickAsync(new() { Timeout = 10_000, Force = true });
                buttons.Add(new { action = "kaart-lijst-toggle-lijst", pass = await page.EvaluateAsync<bool>("() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.classList.contains('show-list')") });
                await viewKaart.ClickAsync(new() { Timeout = 10_000, Force = true });
                buttons.Add(new { action = "kaart-lijst-toggle-kaart", pass = await page.EvaluateAsync<bool>("() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.classList.contains('show-map')") });
            }
            catch (TimeoutException)
            {
                buttons.Add(new { action = "kaart-lijst-toggle", pass = false, note = "timeout" });
            }
            var filtersBtn = page.Locator("[data-testid=kb-filters-button]:visible").First;
            if (await filtersBtn.CountAsync() > 0)
            {
                await filtersBtn.ClickAsync();
                buttons.Add(new { action = "filters-open", pass = await page.EvaluateAsync<bool>("() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.classList.contains('filters-open')") });
                var close = page.Locator("[data-testid=kb-filter-sheet-close], .filter-sheet__close, [data-testid=kb-filter-cancel]");
                if (await close.CountAsync() > 0)
                {
                    await close.First.ClickAsync(new() { Timeout = 10_000 });
                }
                else
                {
                    await page.Keyboard.PressAsync("Escape");
                }
            }

            var loadMore = page.Locator("[data-testid=kb-load-more]");
            if (await loadMore.CountAsync() > 0 && await loadMore.IsVisibleAsync())
            {
                await loadMore.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
                await loadMore.ClickAsync(new() { Force = true });
                buttons.Add(new { action = "meer-laden", pass = true });
            }
            else
            {
                buttons.Add(new { action = "meer-laden", pass = false, note = "button not visible" });
            }

            buttons.Add(new { action = "bottom-nav-not-covering-map", pass = await page.EvaluateAsync<bool>(
                """
                () => {
                  const nav = document.querySelector('[data-testid=candidate-bottom-nav-ssr], nav.bottom-nav');
                  const canvas = document.querySelector('#job-map .maplibregl-canvas');
                  if (!nav || !canvas) return false;
                  return canvas.getBoundingClientRect().bottom <= nav.getBoundingClientRect().top + 2;
                }
                """) });
        });
        }
        finally
        {
            await File.WriteAllTextAsync(
                Path.Combine(CompareDir, "element-table.json"),
                JsonSerializer.Serialize(elementTables, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(
                Path.Combine(CompareDir, "buttons-tested.json"),
                JsonSerializer.Serialize(buttons, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(
                Path.Combine(CompareDir, "REPORT.md"),
                BuildReportMarkdown(elementTables, buttons));
        }

        var sideCars = Directory.Exists(CompareDir)
            ? Directory.GetFiles(CompareDir, "side-by-side-*.png")
            : [];
        Assert.True(sideCars.Length >= 7, "Expected 7 side-by-side PNGs, got " + sideCars.Length);

        var buttonFails = buttons
            .Select(b => JsonSerializer.SerializeToElement(b))
            .Where(e => e.TryGetProperty("pass", out var p) && p.ValueKind == JsonValueKind.False)
            .Select(e => e.TryGetProperty("action", out var a) ? a.GetString() : "?")
            .ToList();
        Assert.True(buttonFails.Count == 0, "Button checklist failures: " + string.Join(", ", buttonFails));

        var elementFails = new List<string>();
        foreach (var table in elementTables)
        {
            var el = JsonSerializer.SerializeToElement(table);
            if (!el.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var row in rows.EnumerateArray())
            {
                if (row.TryGetProperty("pass", out var pass) && pass.ValueKind == JsonValueKind.False)
                {
                    var id = row.TryGetProperty("id", out var idEl) ? idEl.GetString() : "?";
                    elementFails.Add(id ?? "?");
                }
            }
        }

        Assert.True(elementFails.Count == 0, "Element table failures: " + string.Join(", ", elementFails));
    }

    private static string BuildReportMarkdown(List<object> elementTables, List<object> buttons)
    {
        var elementJson = JsonSerializer.Serialize(elementTables, new JsonSerializerOptions { WriteIndented = true });
        var buttonsJson = JsonSerializer.Serialize(buttons, new JsonSerializerOptions { WriteIndented = true });
        return $"""
            # Banenkaart lagen compare (PR #923)

            Publish output: `{PublishDir}` (outside repo, Release build).

            ## Root cause acc breakage (ec45303 / logged-in hard reload)

            **Cause:** `banenkaart-ssr.js` restored `sessionStorage` key `jobsy.bk.ui.v1` on reload with `showMap: false` (and desktop `desktopList: true`), hiding the map/FAB and leaving list-only layout — half-unstyled list, narrow chrome.

            **Fix:** UI state key bumped to `jobsy.bk.ui.v2`, legacy v1 cleared; on navigation reload desktop/mobile default back to **map view**; SW/cache `CACHE_VERSION` → `acc9`.

            ## Side-by-side paths

            - `{CompareDir}/side-by-side-desktop-1280-map-list.png`
            - `{CompareDir}/side-by-side-desktop-1280-popup.png`
            - `{CompareDir}/side-by-side-header-c-desktop-1280.png`
            - `{CompareDir}/side-by-side-mobile-390-map.png`
            - `{CompareDir}/side-by-side-mobile-390-uitleg.png`
            - `{CompareDir}/side-by-side-mobile-390-vacature.png`
            - `{CompareDir}/side-by-side-header-c-mobile-390.png`

            Raw app captures under `{CompareDir}/captures/`.

            ## Element table

            See `{CompareDir}/element-table.json`.

            ```json
            {elementJson}
            ```

            ## Buttons tested

            See `{CompareDir}/buttons-tested.json`.

            ```json
            {buttonsJson}
            ```

            Generated: {DateTime.UtcNow:O}
            """;
    }

    private async Task CaptureViewportAsync(int width, int height, bool touch, string slug, Func<IPage, string, Task> work)
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(30_000);
        await page.GotoAsync(_baseUrl + "/__test/sign-in?role=candidate&return=" + Uri.EscapeDataString("/banenkaart"));
        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.dataset?.banenkaartSsrBound === '1'",
            null,
            new() { Timeout = 90_000 });
        var later = page.Locator("[data-testid=kb-start-later]");
        if (await later.CountAsync() > 0)
        {
            await later.First.ClickAsync();
        }

        var capDir = Path.Combine(CompareDir, "captures", slug);
        Directory.CreateDirectory(capDir);
        await work(page, capDir);
    }

    private static async Task WaitReadyAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => {
              const canvas = document.querySelector('#job-map .maplibregl-canvas');
              if (!canvas) return false;
              const rect = canvas.getBoundingClientRect();
              if (rect.width < 32 || rect.height < 32) return false;
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              return !!(map && typeof map.getZoom === 'function');
            }
            """,
            null,
            new() { Timeout = 90_000 });
        try
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const strip = document.querySelector('.strip .sc');
                  const list = document.querySelector('.vc');
                  const w = window.innerWidth || 1280;
                  if (w < 900) return !!strip;
                  return !!(strip && list);
                }
                """,
                null,
                new() { Timeout = 60_000 });
        }
        catch (TimeoutException)
        {
            // Catalog/strip may lag; still capture map chrome for compare artifacts.
        }

        try
        {
            await page.WaitForFunctionAsync(
                "() => window.jobMap && window.jobMap.__testGetPinCount && window.jobMap.__testGetPinCount() > 0",
                null,
                new() { Timeout = 60_000 });
        }
        catch (TimeoutException)
        {
            // Pins may still open via rendered features fallback.
        }

        await page.WaitForTimeoutAsync(800);
    }

    private static async Task ShotAsync(IPage page, string dir, string name) =>
        await page.ScreenshotAsync(new() { Path = Path.Combine(dir, name), FullPage = false });

    private static async Task<bool> OpenFirstPinPopupAsync(IPage page)
    {
        var rowLoc = page.Locator(".vc[data-vacancy-id], [data-testid=kb-list-row][data-vacancy-id]");
        string? rowId = null;
        if (await rowLoc.CountAsync() > 0)
        {
            rowId = await rowLoc.First.GetAttributeAsync("data-vacancy-id");
        }

        if (!string.IsNullOrWhiteSpace(rowId))
        {
            await page.EvaluateAsync(
                "(id) => { if (window.jobMap && window.jobMap.__testOpenVacancyById) window.jobMap.__testOpenVacancyById(id); }",
                rowId);
            return await WaitForVacancyPopupAsync(page);
        }

        var mapPinId = await page.EvaluateAsync<string?>(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              if (!map) return null;
              const layers = ['jobsy-pins-unclustered', 'jobsy-pins-point', 'jobsy-pins-clusters', 'jobsy-pins'];
              for (const layer of layers) {
                let feats = [];
                try {
                  feats = map.queryRenderedFeatures({ layers: [layer] }) || [];
                } catch (e) { }
                for (const f of feats) {
                  const id = f.properties && (f.properties.id || f.properties.vacancyId);
                  if (id) return String(id);
                }
              }
              return null;
            }
            """);
        if (string.IsNullOrWhiteSpace(mapPinId))
        {
            return false;
        }

        await page.EvaluateAsync(
            "(id) => { if (window.jobMap && window.jobMap.__testOpenVacancyById) window.jobMap.__testOpenVacancyById(id); }",
            mapPinId);
        return await WaitForVacancyPopupAsync(page);
    }

    private static async Task<bool> WaitForVacancyPopupAsync(IPage page)
    {
        var popup = page.Locator(".maplibregl-popup:visible, .kb-popup-lagen:visible, .map-cluster-sheet:visible");
        try
        {
            await popup.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static async Task<object> BuildElementTableAsync(IPage page, int width, string profile)
    {
        var checks = new (string id, string sel, string? contains)[]
        {
            ("lagen-root", "[data-banenkaart-ssr=\"1\"].jobsy-discovery--lagen", null),
            ("header-strip", "[data-testid=kb-nearby-banner]", null),
            ("strip", ".strip .sc", null),
            ("list-vc", width >= 900 ? ".vc" : ".strip .sc", null),
            ("legend-or-legbtn", width >= 900 ? "[data-testid=kb-map-legend]" : "[data-testid=kb-map-legbtn]", null),
            ("bottom-nav", "[data-testid=candidate-bottom-nav-ssr], nav.bottom-nav", null),
        };
        var rows = new List<object>();
        foreach (var c in checks)
        {
            var loc = page.Locator(c.sel);
            var ok = await loc.CountAsync() > 0 && await loc.First.IsVisibleAsync();
            rows.Add(new { c.id, c.sel, pass = ok });
        }

        return new { profile, width, rows };
    }

    private static async Task SideBySideAsync(string mockupPath, string appPath, string outPath)
    {
        if (!File.Exists(mockupPath) || !File.Exists(appPath))
        {
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "python3",
            ArgumentList =
            {
                "-c",
                """
import sys
from PIL import Image
mockup, app, out = sys.argv[1:4]
a = Image.open(mockup).convert('RGBA')
b = Image.open(app).convert('RGBA')
h = max(a.height, b.height)
def fit(im):
    r = h / im.height
    w = int(im.width * r)
    return im.resize((w, h), Image.Resampling.LANCZOS)
a, b = fit(a), fit(b)
gap = 8
label_h = 28
canvas = Image.new('RGBA', (a.width + gap + b.width, h + label_h), (250, 246, 239, 255))
canvas.paste(a, (0, label_h))
canvas.paste(b, (a.width + gap, label_h))
canvas.save(out)
""",
                mockupPath,
                appPath,
                outPath,
            },
            RedirectStandardError = true,
        };
        using var p = Process.Start(psi)!;
        var err = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        Assert.True(p.ExitCode == 0, "side-by-side failed: " + err);
    }

    private static async Task EnsurePublishedToWorkspaceTmpAsync()
    {
        var root = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(root, "Jobsy.sln")) && Directory.GetParent(root) is not null)
        {
            root = Directory.GetParent(root)!.FullName;
        }

        var dll = Path.Combine(PublishDir, "Jobsy.Web.dll");
        var webCs = Path.Combine(root, "Jobsy.Web", "Jobsy.Web.csproj");
        var testsCs = Path.Combine(root, "Jobsy.Tests", "Jobsy.Tests.csproj");
        var srcNewerThanPublish = File.Exists(dll)
            && (File.GetLastWriteTimeUtc(webCs) > File.GetLastWriteTimeUtc(dll)
                || File.GetLastWriteTimeUtc(testsCs) > File.GetLastWriteTimeUtc(dll));
        if (File.Exists(dll) && !srcNewerThanPublish && File.GetLastWriteTimeUtc(dll) > DateTime.UtcNow.AddHours(-3))
        {
            return;
        }

        if (Directory.Exists(PublishDir))
        {
            Directory.Delete(PublishDir, recursive: true);
        }

        Directory.CreateDirectory("/workspace/tmp");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            ArgumentList = { "publish", "Jobsy.Web/Jobsy.Web.csproj", "-c", "Release", "-o", PublishDir, "/p:UseAppHost=false" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var pub = Process.Start(psi)!;
        var stderr = await pub.StandardError.ReadToEndAsync();
        await pub.WaitForExitAsync();
        Assert.True(pub.ExitCode == 0, "publish to /workspace/tmp failed: " + stderr);
    }

    private static async Task ExtractMockupRefsAsync()
    {
        var marker = Path.Combine(MockupRefRoot, "lagen/desktop/02-banenkaart-lagen-desktop-1440.png");
        if (File.Exists(marker))
        {
            return;
        }

        Directory.CreateDirectory(Path.Combine(CompareDir, "mockup-ref"));
        var root = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(root, "Jobsy.sln")) && Directory.GetParent(root) is not null)
        {
            root = Directory.GetParent(root)!.FullName;
        }

        var tarPath = Path.Combine(CompareDir, "mockup-ref.tar");
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = root,
            ArgumentList = { "archive", MockupCommit, "docs/mockups/kandidaat/banenkaart", "-o", tarPath },
            RedirectStandardError = true,
        };
        using (var arch = Process.Start(psi)!)
        {
            var err = await arch.StandardError.ReadToEndAsync();
            await arch.WaitForExitAsync();
            Assert.True(arch.ExitCode == 0, "git archive mockups: " + err);
        }

        Directory.CreateDirectory(Path.Combine(CompareDir, "mockup-ref"));
        var tar = Process.Start(new ProcessStartInfo
        {
            FileName = "tar",
            ArgumentList = { "--no-same-permissions", "-xf", tarPath, "-C", Path.Combine(CompareDir, "mockup-ref") },
            RedirectStandardError = true,
        })!;
        var tarErr = await tar.StandardError.ReadToEndAsync();
        await tar.WaitForExitAsync();
        Assert.True(tar.ExitCode == 0, "tar extract mockups: " + tarErr);
    }
}
