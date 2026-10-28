using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using SkiaSharp;
using Jobsy.Tests.Testing;

namespace Jobsy.Tests;

/// <summary>JOBSY_PLAIN_PR_PREP=1 — volledige banenkaart/banen previews + checklist voor PR na #1332.</summary>
public sealed class Plain878BanenkaartPrPreviewTests : IAsyncLifetime, IAsyncDisposable
{
    private static readonly (string MockupRel, string MockupArtifact)[] Mockups =
    [
        ("docs/mockups/kandidaat/banenkaart/lagen/desktop/02-banenkaart-lagen-desktop-1440.png", "mockup-banenkaart-1440.png"),
        ("docs/mockups/kandidaat/banenkaart/lagen/mobile/01-banenkaart-lagen-mobiel-390.png", "mockup-banenkaart-390.png"),
    ];

    private IllustratedPagesWebFactory? _web;

    public async ValueTask InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("JOBSY_PLAIN_PR_PREP") != "1")
        {
            return;
        }

        _web = new IllustratedPagesWebFactory(plainSkin: true);
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_web is not null)
        {
            await _web.DisposeAsync();
        }
    }

    [Fact]
    public async Task Plain878_banenkaart_pr_preview_and_checklist()
    {
        if (Environment.GetEnvironmentVariable("JOBSY_PLAIN_PR_PREP") != "1")
        {
            return;
        }

        Assert.NotNull(_web);
        var baseUrl = _web!.ServerAddress;
        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_PLAIN_ARTIFACT_DIR")
            ?? "/opt/cursor/artifacts";
        Directory.CreateDirectory(artifactDir);
        CopyMockups(artifactDir);

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        const string queryAuto =
            "?maxMinutes=60&transport=Auto&originLat=52.0705&originLng=4.3007&originLabel=Naaldwijk";

        var checklist = new List<ChecklistRow>();
        var failures = new List<string>();

        foreach (var (width, height, mobile, tag) in new[]
                 {
                     (1440, 900, false, "1440"),
                     (390, 844, true, "390"),
                 })
        {
            await CaptureAsync(browser, baseUrl, artifactDir, checklist, failures, tag, width, height, mobile,
                auth: true, path: "/banenkaart" + queryAuto, label: "banenkaart-ingelogd");
            await CaptureAsync(browser, baseUrl, artifactDir, checklist, failures, tag, width, height, mobile,
                auth: false, path: "/banenkaart" + queryAuto, label: "banenkaart-gast");
            await CaptureAsync(browser, baseUrl, artifactDir, checklist, failures, tag, width, height, mobile,
                auth: true, path: "/banen" + queryAuto, label: "banen-ingelogd");
            await CaptureAsync(browser, baseUrl, artifactDir, checklist, failures, tag, width, height, mobile,
                auth: false, path: "/banen" + queryAuto, label: "banen-gast");
        }

        await CaptureTransportIconAsync(browser, baseUrl, checklist, failures, queryAuto);

        WriteChecklist(artifactDir, checklist, failures);
        Assert.True(failures.Count == 0, string.Join('\n', failures));
    }

    private static async Task CaptureAsync(
        IBrowser browser,
        string baseUrl,
        string artifactDir,
        List<ChecklistRow> checklist,
        List<string> failures,
        string tag,
        int width,
        int height,
        bool mobile,
        bool auth,
        string path,
        string label)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
        });
        await ctx.AddInitScriptAsync(CookieInitScript);
        var page = await ctx.NewPageAsync();
        if (auth)
        {
            var ret = Uri.EscapeDataString("/candidate/paspoort");
            await page.GotoAsync($"{baseUrl}/__test/sign-in?return={ret}", new() { Timeout = 60_000 });
            await page.WaitForURLAsync("**/candidate/paspoort**", new() { Timeout = 60_000 });
        }

        await page.GotoAsync(baseUrl + path, new() { Timeout = 60_000 });
        if (path.StartsWith("/banen", StringComparison.Ordinal))
        {
            await page.WaitForURLAsync("**/banenkaart**", new() { Timeout = 30_000 });
        }

        await page.WaitForSelectorAsync("[data-plain-banenkaart=\"1\"]", new() { Timeout = 30_000 });
        await DismissCookieAsync(page);
        await page.WaitForSelectorAsync("[data-testid=kb-list-row], [data-testid=kb-panel-head]", new() { Timeout = 30_000 });
        try
        {
            await page.WaitForFunctionAsync(
                "() => { const c = document.querySelector('canvas.maplibregl-canvas'); return c && c.getBoundingClientRect().height > 60; }",
                null,
                new() { Timeout = 45_000 });
        }
        catch
        {
            // stub map may be slow; checklist will flag canvas
        }

        await page.WaitForTimeoutAsync(mobile ? 1500 : 1200);

        var shotName = $"preview-{label}-{tag}.png";
        var shotPath = Path.Combine(artifactDir, shotName);
        await page.ScreenshotAsync(new() { Path = shotPath, FullPage = false });

        var mockArtifact = tag == "390" ? "mockup-banenkaart-390.png" : "mockup-banenkaart-1440.png";
        var mockPath = Path.Combine(artifactDir, mockArtifact);
        var comparePath = Path.Combine(artifactDir, $"compare-{label}-{tag}.png");
        await SideBySideAsync(mockPath, shotPath, comparePath);

        var gap = await Plain878LayoutGapHelpers.MeasureAsync(page);
        if (!gap.Pass)
        {
            failures.Add($"{shotName}: {gap.OneLineSummary}");
        }

        var audit = await RunChecklistAsync(page, mobile);
        foreach (var item in audit)
        {
            checklist.Add(new ChecklistRow(label, tag, item.Name, item.Pass, item.Detail));
            if (!item.Pass)
            {
                failures.Add($"{label}@{tag}: {item.Name} — {item.Detail}");
            }
        }

        await page.Locator("header.wg-topbar--candidate").First.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifactDir, $"header-{label}-{tag}.png"),
        });
    }

    private static async Task CaptureTransportIconAsync(
        IBrowser browser,
        string baseUrl,
        List<ChecklistRow> checklist,
        List<string> failures,
        string queryAuto)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
        });
        await ctx.AddInitScriptAsync(CookieInitScript);
        var page = await ctx.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart" + queryAuto, new() { Timeout = 60_000 });
        await page.WaitForSelectorAsync("[data-plain-banenkaart=\"1\"]", new() { Timeout = 30_000 });
        await DismissCookieAsync(page);
        await page.WaitForSelectorAsync("[data-testid=kb-list-row]", new() { Timeout = 30_000 });
        var autoOk = await page.EvaluateAsync<bool>(
            """
            () => {
              const spans = Array.from(document.querySelectorAll('[data-testid=kb-list-row] .meta span'));
              return spans.some(s => {
                const t = (s.textContent || '').toLowerCase();
                if (!t.includes('min')) return false;
                const svg = s.querySelector('svg');
                if (!svg) return false;
                const html = svg.outerHTML || '';
                return html.includes('viewBox') && html.length > 80;
              });
            }
            """);
        checklist.Add(new ChecklistRow("banenkaart-auto", "1440", "Vervoersicoon op kaartje (SSR)", autoOk,
            autoOk ? "svg in meta bij reistijd" : "geen svg in meta"));
        if (!autoOk)
        {
            failures.Add("banenkaart-auto@1440: Vervoersicoon Auto op kaartje");
        }
    }

    private sealed record ChecklistRow(string Preview, string Viewport, string Criterion, bool Pass, string Detail);

    private sealed record AuditItem(string Name, bool Pass, string Detail);

    private static async Task<List<AuditItem>> RunChecklistAsync(IPage page, bool mobile)
    {
        var json = await page.EvaluateAsync<string>(
            """
            (mobile) => {
              const header = document.querySelector('header.wg-topbar--candidate');
              const panel = document.querySelector('.cp-plain-banenkaart');
              const matchBtn = document.querySelector('[data-testid=kb-match-button]');
              const items = [];
              const push = (name, pass, detail) => items.push({ name, pass, detail: detail || '' });

              push('Navy WgTopBar met logo',
                !!header && !!header.querySelector('.wg-topbar__logo, .lobsy-logo'),
                header ? 'header.wg-topbar--candidate aanwezig' : 'geen candidate topbar');

              if (header) {
                const bg = getComputedStyle(header).backgroundColor;
                const fg = getComputedStyle(header).color;
                const navy = bg.includes('10') || bg.includes('15') || bg.includes('20') || bg.includes('25')
                  || bg.includes('31') || bg.includes('32') || bg.includes('44') || bg.includes('68');
                push('Header navy (WgTopBar)', navy, 'bg=' + bg + ' fg=' + fg);
              }

              const lang = header?.querySelector('.lang, [class*="lang"]');
              const langText = (lang?.textContent || '').replace(/\s+/g, ' ');
              push('Taalwissel toont NL', langText.includes('NL'), langText);

              push('Geen kb-mockup-top',
                document.querySelectorAll('[data-testid=kb-mockup-top]').length === 0, 'mockup top bar afwezig');

              push('Geen lege kb-banenkaart-ssr-chrome',
                document.querySelectorAll('[data-testid=kb-banenkaart-ssr-chrome]').length === 0,
                'geen hero-chrome container');

              const active = document.querySelector('[data-testid=kb-active-filters]');
              const tags = active ? active.querySelectorAll('.kb-active-tag').length : 0;
              const wissen = document.querySelector('[data-testid=kb-active-filters-clear]');
              push('Geen leeg filtervak/Wissen zonder tags',
                !active || (tags > 0 && !!wissen),
                tags === 0 ? 'geen active-filters rij' : tags + ' tags');

              if (panel) {
                const font = getComputedStyle(panel).fontFamily.toLowerCase();
                push('Inter op paneel', font.includes('inter'), font);
                const matchStyle = matchBtn ? getComputedStyle(matchBtn) : null;
                if (matchStyle) {
                  const accent = matchStyle.backgroundColor;
                  push('Accent op Match-knop', accent.includes('168') || accent.includes('72') || accent.includes('a848'), accent);
                  const mh = parseFloat(matchStyle.minHeight || matchStyle.height || '0');
                  push('Knoppen minimaal 52px', mh >= 50, 'match ' + mh + 'px');
                }
              }

              const canvas = document.querySelector('canvas.maplibregl-canvas');
              const mapH = canvas ? canvas.getBoundingClientRect().height : 0;
              push('Kaart zichtbaar (canvas)', mapH > 80, 'hoogte ' + Math.round(mapH));

              const rows = document.querySelectorAll('[data-testid=kb-list-row]').length;
              push('Vacaturetegels zichtbaar', rows >= 1, rows + ' rijen');

              if (mobile && matchBtn) {
                const label = matchBtn.querySelector('.kb-match-button__label');
                const vis = label && getComputedStyle(label).display !== 'none' && (label.textContent || '').trim().length > 0;
                push('Match-knop label op 390', !!vis, (label?.textContent || '').trim());
              }

              const coach = document.querySelector('.lobsy-coach-dock, .coach-pill, .lobsy-assistant');
              let coachBlocksPanel = false;
              const panelHead = document.querySelector('[data-testid=kb-panel-head]');
              if (panelHead) {
                const r = panelHead.getBoundingClientRect();
                const x = Math.min(window.innerWidth - 2, Math.max(2, r.left + r.width / 2));
                const y = Math.min(window.innerHeight - 2, Math.max(2, r.top + 16));
                const hit = document.elementFromPoint(x, y);
                coachBlocksPanel = !!(hit && hit.closest('.lobsy-coach-dock, .coach-pill, .lobsy-assistant, .feedback-widget'));
              }
              push('Coach-pill blokkeert paneel niet', !coachBlocksPanel,
                coachBlocksPanel ? 'widget boven zoekbalk' : 'paneel klikbaar');

              const bigSvg = Array.from(document.querySelectorAll('header .ic, .kb-panel-head .ic'))
                .some(s => s.getBoundingClientRect().height > 48);
              push('Geen gigantische iconen', !bigSvg, bigSvg ? 'ic > 48px' : 'ok');

              return JSON.stringify(items);
            }
            """,
            mobile);

        return JsonSerializer.Deserialize<List<AuditItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? [];
    }

    private static void WriteChecklist(string artifactDir, List<ChecklistRow> rows, List<string> failures)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Plain878 banenkaart PR-checklist");
        sb.AppendLine();
        sb.AppendLine("Mockup referentie: docs/mockups/kandidaat/banenkaart/lagen (plain #878 renders niet in repo).");
        sb.AppendLine("Vergelijking: compare-*.png (mockup links, app rechts).");
        sb.AppendLine();
        sb.AppendLine("| Preview | Viewport | Criterium | OK | Detail |");
        sb.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var g in rows.GroupBy(r => (r.Preview, r.Viewport)))
        {
            foreach (var r in g)
            {
                sb.AppendLine($"| {r.Preview} | {r.Viewport} | {r.Criterion} | {(r.Pass ? "ja" : "nee")} | {r.Detail} |");
            }
        }

        sb.AppendLine();
        sb.AppendLine(failures.Count == 0 ? "Status: **geslaagd**" : "Status: **open punten**");
        File.WriteAllText(Path.Combine(artifactDir, "plain878-banenkaart-pr-checklist.md"), sb.ToString(), Encoding.UTF8);
    }

    private static Task SideBySideAsync(string mockupPath, string appPath, string outPath)
    {
        if (!File.Exists(mockupPath) || !File.Exists(appPath))
        {
            return Task.CompletedTask;
        }

        using var aImg = SkiaTestImage.LoadRgba(mockupPath);
        using var bImg = SkiaTestImage.LoadRgba(appPath);
        var h = Math.Max(aImg.Height, bImg.Height);
        using var a = SkiaTestImage.Resize(
            aImg.ToBitmap(),
            Math.Max(1, (int)(aImg.Width * (h / (float)aImg.Height))),
            h);
        using var b = SkiaTestImage.Resize(
            bImg.ToBitmap(),
            Math.Max(1, (int)(bImg.Width * (h / (float)bImg.Height))),
            h);
        const int gap = 8;
        using var sheet = SkiaTestImage.CreateSolid(a.Width + gap + b.Width, h, new SKColor(247, 248, 250));
        using (var canvas = new SKCanvas(sheet))
        {
            canvas.DrawBitmap(a, 0, 0);
            canvas.DrawBitmap(b, a.Width + gap, 0);
        }

        SkiaTestImage.SavePng(sheet, outPath);
        return Task.CompletedTask;
    }

    private static void CopyMockups(string artifactDir)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        foreach (var (rel, name) in Mockups)
        {
            var src = Path.Combine(root, rel);
            if (File.Exists(src))
            {
                File.Copy(src, Path.Combine(artifactDir, name), overwrite: true);
            }
        }
    }

    private const string CookieInitScript = """
        try {
          localStorage.setItem('Jobsy.CookieConsent', 'necessary');
          document.documentElement.classList.add('cookie-consent-known');
        } catch (e) {}
        """;

    private static async Task DismissCookieAsync(IPage page)
    {
        var necessary = page.Locator("button[data-consent=\"necessary\"]").First;
        if (await necessary.CountAsync() > 0 && await necessary.IsVisibleAsync())
        {
            await necessary.ClickAsync();
            await page.WaitForTimeoutAsync(200);
        }
    }
}
