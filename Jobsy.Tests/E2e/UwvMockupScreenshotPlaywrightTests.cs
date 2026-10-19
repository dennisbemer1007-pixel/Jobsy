using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Captures UWV werkmap blocks on real candidate routes (flag <c>Features:UwvFeatures</c>).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class UwvMockupScreenshotPlaywrightTests : IAsyncLifetime
{
    private const string EnvRun = "JOBSY_UWV_MOCKUP_SCREENSHOTS";

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    private const string EnvPartial = "JOBSY_UWV_MOCKUP_PARTIAL";

    private static bool PartialCapturesOnly =>
        string.Equals(Environment.GetEnvironmentVariable(EnvPartial), "1", StringComparison.Ordinal);

    private static string OutDir =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_UWV_MOCKUP_ARTIFACTS"))
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "uwv-build"))
            : Environment.GetEnvironmentVariable("JOBSY_UWV_MOCKUP_ARTIFACTS")!;

    private static readonly string[] PartialCapturePrefixes =
    [
        "04-ondernemer-result-nl-1440",
        "04-ondernemer-result-nl-1920",
        "04-ondernemer-result-en-1440",
        "04-ondernemer-result-en-1920",
    ];

    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _baseUrl = "";

    private static readonly (string FileSlug, string Path, int Width, int Height, bool FullPage)[] Captures =
    [
        ("01-carriere-kansberoepen-nl-1440", "/carriere", 1440, 900, true),
        ("01-carriere-kansberoepen-nl-1920", "/carriere", 1920, 1080, true),
        ("01-carriere-kansberoepen-nl-390", "/carriere", 390, 844, true),
        ("01-carriere-kansberoepen-en-1440", "/carriere", 1440, 900, true),
        ("01-carriere-kansberoepen-en-1920", "/carriere", 1920, 1080, true),
        ("01-carriere-kansberoepen-en-390", "/carriere", 390, 844, true),
        ("02-banenkaart-kanslaag-nl-1440", "/banenkaart", 1440, 900, false),
        ("02-banenkaart-kanslaag-nl-390", "/banenkaart", 390, 844, false),
        ("02-banenkaart-kanslaag-en-1440", "/banenkaart", 1440, 900, false),
        ("02-banenkaart-kanslaag-en-390", "/banenkaart", 390, 844, false),
        ("03-workshops-omscholen-nl-1440", "/carriere/leren", 1440, 900, true),
        ("03-workshops-omscholen-nl-390", "/carriere/leren", 390, 844, true),
        ("03-workshops-omscholen-en-1440", "/carriere/leren", 1440, 900, true),
        ("03-workshops-omscholen-en-390", "/carriere/leren", 390, 844, true),
        ("04-ondernemer-intro-nl-1440", "/candidate/ondernemerscheck?stap=intro", 1440, 900, true),
        ("04-ondernemer-intro-nl-390", "/candidate/ondernemerscheck?stap=intro", 390, 844, true),
        ("04-ondernemer-intro-en-1440", "/candidate/ondernemerscheck?stap=intro", 1440, 900, true),
        ("04-ondernemer-intro-en-390", "/candidate/ondernemerscheck?stap=intro", 390, 844, true),
        ("04-ondernemer-vraag-nl-1440", "/candidate/ondernemerscheck?stap=question", 1440, 900, true),
        ("04-ondernemer-vraag-nl-390", "/candidate/ondernemerscheck?stap=question", 390, 844, true),
        ("04-ondernemer-vraag-en-1440", "/candidate/ondernemerscheck?stap=question", 1440, 900, true),
        ("04-ondernemer-vraag-en-390", "/candidate/ondernemerscheck?stap=question", 390, 844, true),
        ("04-ondernemer-result-nl-1440", "/candidate/ondernemerscheck?stap=result", 1440, 900, true),
        ("04-ondernemer-result-nl-1920", "/candidate/ondernemerscheck?stap=result", 1920, 1080, true),
        ("04-ondernemer-result-nl-390", "/candidate/ondernemerscheck?stap=result", 390, 844, true),
        ("04-ondernemer-result-en-1440", "/candidate/ondernemerscheck?stap=result", 1440, 900, true),
        ("04-ondernemer-result-en-1920", "/candidate/ondernemerscheck?stap=result", 1920, 1080, true),
        ("04-ondernemer-result-en-390", "/candidate/ondernemerscheck?stap=result", 390, 844, true),
        ("05-drijfveren-wat-drijft-jou-nl-1440", "/candidate/wat-drijft-jou", 1440, 900, true),
        ("05-drijfveren-wat-drijft-jou-nl-390", "/candidate/wat-drijft-jou", 390, 844, true),
        ("05-drijfveren-wat-drijft-jou-en-1440", "/candidate/wat-drijft-jou", 1440, 900, true),
        ("05-drijfveren-wat-drijft-jou-en-390", "/candidate/wat-drijft-jou", 390, 844, true),
        ("06-adviseur-share-nl-1440", "/candidate/adviseur-werk", 1440, 900, true),
        ("06-adviseur-share-nl-390", "/candidate/adviseur-werk", 390, 844, true),
        ("06-adviseur-share-en-1440", "/candidate/adviseur-werk", 1440, 900, true),
        ("06-adviseur-share-en-390", "/candidate/adviseur-werk", 390, 844, true),
    ];

    public async ValueTask InitializeAsync()
    {
        if (!ShouldRun)
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        Environment.SetEnvironmentVariable("JOBSY_UWV_FEATURES", "1");
        _web = new IllustratedPagesWebFactory();
        _ = _web.ServerAddress;
        _baseUrl = _web.ServerAddress;
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
    public async Task Capture_uwv_mockup_screens()
    {
        if (!ShouldRun)
        {
            return;
        }

        Directory.CreateDirectory(OutDir);

        var captures = PartialCapturesOnly
            ? Captures.Where(c => PartialCapturePrefixes.Any(p => c.FileSlug.StartsWith(p, StringComparison.Ordinal))).ToArray()
            : Captures;

        foreach (var cap in captures)
        {
            var lang = cap.FileSlug.Contains("-en-", StringComparison.Ordinal) ? "en" : "nl";
            await using var context = await _browser!.NewContextAsync(KbE2e.ContextFor(cap.Width, cap.Height));
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);

            if (lang == "en")
            {
                await page.GotoAsync(_baseUrl.TrimEnd('/') + "/taal/en?returnUrl=" + Uri.EscapeDataString(cap.Path));
            }

            await page.GotoAsync(_baseUrl.TrimEnd('/') + "/__test/sign-in?role=candidate&return=" + Uri.EscapeDataString(cap.Path));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await page.EvaluateAsync("""
                () => {
                  document.querySelectorAll('.cookie-consent, .cookie-banner, [data-testid="cookie-consent"]').forEach(el => el.remove());
                }
                """);

            if (cap.Path.Contains("banenkaart", StringComparison.Ordinal))
            {
                await page.WaitForSelectorAsync("[data-testid='uwv-kansberoepen-toggle'], .jobsy-discovery", new() { Timeout = 60_000 });
            }
            else if (cap.Path.StartsWith("/carriere?", StringComparison.Ordinal)
                     || cap.Path.Equals("/carriere", StringComparison.OrdinalIgnoreCase))
            {
                await page.WaitForSelectorAsync("[data-testid='uwv-carriere-sections'], .career-page--illustrated", new() { Timeout = 60_000 });
            }
            else if (cap.Path.StartsWith("/carriere/leren", StringComparison.OrdinalIgnoreCase))
            {
                await page.WaitForSelectorAsync("[data-testid='uwv-carriere-leren-sections'], .uwv-partners, .opl-page", new() { Timeout = 60_000 });
            }
            else
            {
                await page.WaitForSelectorAsync("main.app-main", new() { Timeout = 60_000 });
            }

            await Task.Delay(1500);

            if (cap.Path.Contains("ondernemerscheck", StringComparison.OrdinalIgnoreCase)
                && cap.Path.Contains("result", StringComparison.OrdinalIgnoreCase))
            {
                await page.Locator(".uwv-ondernemer-result__scene img").WaitForAsync(new() { Timeout = 30_000 });
                await page.Locator(".uwv-ondernemer-result__scene img").EvaluateAsync(
                    "img => img.complete ? Promise.resolve() : img.decode()");
                await AssertOndernemerResultLayoutAsync(page, cap.Width);
            }

            var path = Path.Combine(OutDir, cap.FileSlug + ".png");
            await page.ScreenshotAsync(new() { Path = path, FullPage = cap.FullPage });
        }

        await BuildPerScreenContactSheetsAsync(OutDir);
    }

    private static async Task AssertOndernemerResultLayoutAsync(IPage page, int width)
    {
        var layout = await page.EvaluateAsync<CoachOverlapDto>("""
            () => {
              const done = document.querySelector('.uwv-ondernemer-result__done');
              const coach = document.querySelector('#lobsy-coach-btn');
              const scene = document.querySelector('.uwv-ondernemer-result__scene');
              const sceneImg = scene?.querySelector('img');
              if (!done || !coach) {
                return { ok: false, reason: 'missing nodes' };
              }
              const dr = done.getBoundingClientRect();
              const cr = coach.getBoundingClientRect();
              const overlap = !(dr.right <= cr.left + 2 || dr.left >= cr.right - 2 || dr.bottom <= cr.top + 2 || dr.top >= cr.bottom - 2);
              let sceneHiddenMobile = true;
              if (scene && sceneImg) {
                const sr = scene.getBoundingClientRect();
                const style = getComputedStyle(scene);
                sceneHiddenMobile = style.display === 'none' || sr.width < 8 || sr.height < 8;
              }
              const panel = document.querySelector('.uwv-ondernemer-result__panel');
              const img = document.querySelector('.uwv-ondernemer-result__scene img');
              let insetOk = true;
              let inset = { left: 0, right: 0, top: 0, bottom: 0 };
              let edgeOk = true;
              let edgeDiffs = [];
              if (panel && img && scene && window.innerWidth >= 640 && img.complete) {
                const pr = panel.getBoundingClientRect();
                const ir = img.getBoundingClientRect();
                inset = {
                  left: ir.left - pr.left,
                  right: pr.right - ir.right,
                  top: ir.top - pr.top,
                  bottom: pr.bottom - ir.bottom
                };
                insetOk = inset.left >= 16 && inset.right >= 16 && inset.top >= 16 && inset.bottom >= 16;
                const w = Math.max(1, Math.round(ir.width));
                const h = Math.max(1, Math.round(ir.height));
                const pad = 8;
                const canvas = document.createElement('canvas');
                canvas.width = w + pad * 2;
                canvas.height = h + pad * 2;
                const ctx = canvas.getContext('2d');
                const bgProbe = document.createElement('span');
                bgProbe.style.cssText = 'position:absolute;visibility:hidden;background:var(--bg)';
                document.body.appendChild(bgProbe);
                const bgColor = getComputedStyle(bgProbe).backgroundColor;
                bgProbe.remove();
                ctx.fillStyle = bgColor;
                ctx.fillRect(0, 0, canvas.width, canvas.height);
                ctx.drawImage(img, pad, pad, w, h);
                const sample = (x, y) => ctx.getImageData(Math.floor(x), Math.floor(y), 1, 1).data;
                const diff = (a, b) => Math.max(
                  Math.abs(a[0] - b[0]), Math.abs(a[1] - b[1]), Math.abs(a[2] - b[2]), Math.abs(a[3] - b[3]));
                edgeDiffs = [
                  diff(sample(pad + w / 2, pad - 3), sample(pad + w / 2, pad + 3)),
                  diff(sample(pad + w / 2, pad + h + 3), sample(pad + w / 2, pad + h - 3)),
                  diff(sample(pad - 3, pad + h / 2), sample(pad + 3, pad + h / 2)),
                  diff(sample(pad + w + 3, pad + h / 2), sample(pad + w - 3, pad + h / 2))
                ];
                edgeOk = edgeDiffs.every((d) => d < 6);
              }
              let contrastOk = true;
              let contrast = {};
              if (window.innerWidth >= 640) {
                const sampleCssColor = (cssColor) => {
                  const canvas = document.createElement('canvas');
                  canvas.width = 1;
                  canvas.height = 1;
                  const ctx = canvas.getContext('2d');
                  ctx.fillStyle = cssColor;
                  ctx.fillRect(0, 0, 1, 1);
                  return ctx.getImageData(0, 0, 1, 1).data;
                };
                const lumFromData = (d) => {
                  const ch = [d[0], d[1], d[2]].map((v) => {
                    v /= 255;
                    return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
                  });
                  return 0.2126 * ch[0] + 0.7152 * ch[1] + 0.0722 * ch[2];
                };
                const ratio = (fgCss, bgCss) => {
                  const l1 = lumFromData(sampleCssColor(fgCss));
                  const l2 = lumFromData(sampleCssColor(bgCss));
                  const lighter = Math.max(l1, l2);
                  const darker = Math.min(l1, l2);
                  return (lighter + 0.05) / (darker + 0.05);
                };
                const probe = document.createElement('span');
                probe.style.cssText = 'position:absolute;visibility:hidden;background:var(--surface)';
                document.body.appendChild(probe);
                const surface = getComputedStyle(probe).backgroundColor;
                probe.remove();
                const title = document.querySelector('.uwv-ondernemer-result__title');
                const lead = document.querySelector('.uwv-ondernemer-result__lead');
                const eyebrow = document.querySelector('.uwv-ondernemer-result__eyebrow');
                const link = document.querySelector('.uwv-ondernemer-result__step-link');
                contrast = {
                  title: title ? ratio(getComputedStyle(title).color, surface) : 99,
                  lead: lead ? ratio(getComputedStyle(lead).color, surface) : 99,
                  eyebrow: eyebrow ? ratio(getComputedStyle(eyebrow).color, surface) : 99,
                  link: link ? ratio(getComputedStyle(link).color, surface) : 99,
                  done: done ? ratio(getComputedStyle(done).color, surface) : 99
                };
                contrastOk =
                  contrast.title >= 3 &&
                  contrast.lead >= 4.5 &&
                  contrast.eyebrow >= 4.5 &&
                  contrast.link >= 4.5 &&
                  contrast.done >= 4.5;
              }
              return {
                ok: !overlap && insetOk && edgeOk && contrastOk,
                overlap,
                insetOk,
                edgeOk,
                contrastOk,
                contrast: JSON.stringify(contrast),
                edgeDiffs,
                inset,
                doneBottom: dr.bottom,
                coachTop: cr.top,
                sceneHidden: sceneHiddenMobile,
                viewportWidth: window.innerWidth
              };
            }
            """);

        Assert.True(layout.Ok, layout.Reason ?? DescribeLayoutFailure(layout));

        if (width >= 640)
        {
            Assert.False(layout.SceneHidden, "result scene should be visible from 640px up");
            Assert.True(layout.InsetOk, $"vignette img must sit >=16px inside card (inset L{layout.Inset?.Left} R{layout.Inset?.Right} T{layout.Inset?.Top} B{layout.Inset?.Bottom})");
            Assert.True(layout.EdgeOk, $"vignette edge feather failed (diffs={string.Join(',', layout.EdgeDiffs ?? [])})");
            Assert.True(layout.ContrastOk, $"text contrast below AA on cream/surface (ratios={layout.Contrast})");
        }
        else
        {
            Assert.True(layout.SceneHidden, "result scene should be hidden below 640px");
        }
    }

    private static string DescribeLayoutFailure(CoachOverlapDto layout)
    {
        if (layout.Overlap)
        {
            return $"coach overlaps done line (doneBottom={layout.DoneBottom}, coachTop={layout.CoachTop})";
        }

        if (!layout.InsetOk)
        {
            return $"vignette inset too tight (L{layout.Inset?.Left} R{layout.Inset?.Right} T{layout.Inset?.Top} B{layout.Inset?.Bottom})";
        }

        if (!layout.EdgeOk)
        {
            return $"vignette edge feather failed (diffs={string.Join(',', layout.EdgeDiffs ?? [])})";
        }

        return $"text contrast below AA (ratios={layout.Contrast})";
    }

    private sealed class CoachOverlapDto
    {
        public bool Ok { get; set; }
        public string? Reason { get; set; }
        public bool Overlap { get; set; }
        public bool InsetOk { get; set; }
        public bool EdgeOk { get; set; }
        public bool ContrastOk { get; set; }
        public string? Contrast { get; set; }
        public double[]? EdgeDiffs { get; set; }
        public InsetDto? Inset { get; set; }
        public double DoneBottom { get; set; }
        public double CoachTop { get; set; }
        public bool SceneHidden { get; set; }
    }

    private sealed class InsetDto
    {
        public double Left { get; set; }
        public double Right { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }
    }

    private static async Task BuildPerScreenContactSheetsAsync(string dir)
    {
        var pngs = Directory.GetFiles(dir, "*.png")
            .Where(p => !p.Contains("contact-", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName!)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
        if (pngs.Count == 0)
        {
            return;
        }

        var scriptPath = Path.Combine(dir, "_build_contact_sheets.py");
        await File.WriteAllTextAsync(scriptPath, """
            import re
            import sys
            from pathlib import Path
            from PIL import Image

            out_dir = Path(sys.argv[1])
            files = sorted(out_dir.glob("*.png"))
            files = [f for f in files if "contact-" not in f.name]

            def screen_key(name: str) -> str:
                stem = name.replace(".png", "")
                m = re.match(r"^(.*)-(nl|en)-(1440|390|1920)$", stem)
                return m.group(1) if m else stem

            groups = {}
            for f in files:
                groups.setdefault(screen_key(f.name), []).append(f)

            pad = 24
            bg = (245, 242, 238)

            for key, members in sorted(groups.items()):
                for lang in ("nl", "en"):
                    desktop = next((m for m in members if f"-{lang}-1920" in m.name), None)
                    if desktop is None:
                        desktop = next((m for m in members if f"-{lang}-1440" in m.name), None)
                    mobile = next((m for m in members if f"-{lang}-390" in m.name), None)
                    if desktop is None or mobile is None:
                        continue
                    d = Image.open(desktop).convert("RGB")
                    m = Image.open(mobile).convert("RGB")
                    target_h = max(d.height, m.height)
                    if d.height != target_h:
                        ratio = target_h / d.height
                        d = d.resize((int(d.width * ratio), target_h), Image.Resampling.LANCZOS)
                    if m.height != target_h:
                        ratio = target_h / m.height
                        m = m.resize((int(m.width * ratio), target_h), Image.Resampling.LANCZOS)
                    sheet_w = d.width + m.width + pad * 3
                    sheet_h = target_h + pad * 2
                    sheet = Image.new("RGB", (sheet_w, sheet_h), bg)
                    sheet.paste(d, (pad, pad))
                    sheet.paste(m, (pad * 2 + d.width, pad))
                    out = out_dir / f"{key}-contact-{lang}.png"
                    sheet.save(out, optimize=True)
            """);

        using var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "python3",
            Arguments = $"\"{scriptPath}\" \"{dir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        if (proc is not null)
        {
            await proc.WaitForExitAsync();
        }
    }
}
