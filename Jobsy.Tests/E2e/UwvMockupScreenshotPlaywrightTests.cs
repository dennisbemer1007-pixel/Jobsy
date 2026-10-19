using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Captures UWV werkmap mockup PNGs (r3) into <c>uwv-mockups-r3/</c> at repo root or JOBSY_UWV_MOCKUP_ARTIFACTS.
/// Run: JOBSY_UWV_MOCKUP_SCREENSHOTS=1 dotnet test --filter FullyQualifiedName~UwvMockupScreenshotPlaywrightTests
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class UwvMockupScreenshotPlaywrightTests
{
    private const string EnvRun = "JOBSY_UWV_MOCKUP_SCREENSHOTS";

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    private static string OutDir =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_UWV_MOCKUP_ARTIFACTS"))
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "uwv-mockups-r3"))
            : Environment.GetEnvironmentVariable("JOBSY_UWV_MOCKUP_ARTIFACTS")!;

    private static readonly (string FileSlug, string Path, int Width, int Height, bool FullPage)[] Captures =
    [
        ("01-carriere-kansberoepen-nl-1440", "/preview/uwv-mockups/carriere?lang=nl", 1440, 900, true),
        ("01-carriere-kansberoepen-nl-1920", "/preview/uwv-mockups/carriere?lang=nl&viewport=1920", 1920, 1080, true),
        ("01-carriere-kansberoepen-nl-390", "/preview/uwv-mockups/carriere?lang=nl", 390, 844, true),
        ("01-carriere-kansberoepen-en-1440", "/preview/uwv-mockups/carriere?lang=en", 1440, 900, true),
        ("01-carriere-kansberoepen-en-390", "/preview/uwv-mockups/carriere?lang=en", 390, 844, true),
        ("02-banenkaart-kanslaag-nl-1440", "/preview/uwv-mockups/banenkaart?lang=nl", 1440, 900, false),
        ("02-banenkaart-kanslaag-nl-390", "/preview/uwv-mockups/banenkaart?lang=nl", 390, 844, false),
        ("02-banenkaart-kanslaag-en-1440", "/preview/uwv-mockups/banenkaart?lang=en", 1440, 900, false),
        ("02-banenkaart-kanslaag-en-390", "/preview/uwv-mockups/banenkaart?lang=en", 390, 844, false),
        ("03-workshops-omscholen-nl-1440", "/preview/uwv-mockups/workshops?lang=nl", 1440, 900, true),
        ("03-workshops-omscholen-nl-390", "/preview/uwv-mockups/workshops?lang=nl", 390, 844, true),
        ("03-workshops-omscholen-en-1440", "/preview/uwv-mockups/workshops?lang=en", 1440, 900, true),
        ("03-workshops-omscholen-en-390", "/preview/uwv-mockups/workshops?lang=en", 390, 844, true),
        ("04-ondernemer-intro-nl-1440", "/preview/uwv-mockups/ondernemer-intro?lang=nl", 1440, 900, true),
        ("04-ondernemer-intro-nl-390", "/preview/uwv-mockups/ondernemer-intro?lang=nl", 390, 844, true),
        ("04-ondernemer-intro-en-1440", "/preview/uwv-mockups/ondernemer-intro?lang=en", 1440, 900, true),
        ("04-ondernemer-intro-en-390", "/preview/uwv-mockups/ondernemer-intro?lang=en", 390, 844, true),
        ("04-ondernemer-vraag-nl-1440", "/preview/uwv-mockups/ondernemer-question?lang=nl", 1440, 900, true),
        ("04-ondernemer-vraag-nl-390", "/preview/uwv-mockups/ondernemer-question?lang=nl", 390, 844, true),
        ("04-ondernemer-vraag-en-1440", "/preview/uwv-mockups/ondernemer-question?lang=en", 1440, 900, true),
        ("04-ondernemer-vraag-en-390", "/preview/uwv-mockups/ondernemer-question?lang=en", 390, 844, true),
        ("04-ondernemer-result-nl-1440", "/preview/uwv-mockups/ondernemer-result?lang=nl", 1440, 900, true),
        ("04-ondernemer-result-nl-390", "/preview/uwv-mockups/ondernemer-result?lang=nl", 390, 844, true),
        ("04-ondernemer-result-en-1440", "/preview/uwv-mockups/ondernemer-result?lang=en", 1440, 900, true),
        ("04-ondernemer-result-en-390", "/preview/uwv-mockups/ondernemer-result?lang=en", 390, 844, true),
        ("05-drijfveren-wat-drijft-jou-nl-1440", "/preview/uwv-mockups/drijfveren?lang=nl", 1440, 900, true),
        ("05-drijfveren-wat-drijft-jou-nl-390", "/preview/uwv-mockups/drijfveren?lang=nl", 390, 844, true),
        ("05-drijfveren-wat-drijft-jou-en-1440", "/preview/uwv-mockups/drijfveren?lang=en", 1440, 900, true),
        ("05-drijfveren-wat-drijft-jou-en-390", "/preview/uwv-mockups/drijfveren?lang=en", 390, 844, true),
        ("06-adviseur-share-nl-1440", "/preview/uwv-mockups/adviseur?lang=nl", 1440, 900, true),
        ("06-adviseur-share-nl-390", "/preview/uwv-mockups/adviseur?lang=nl", 390, 844, true),
        ("06-adviseur-share-en-1440", "/preview/uwv-mockups/adviseur?lang=en", 1440, 900, true),
        ("06-adviseur-share-en-390", "/preview/uwv-mockups/adviseur?lang=en", 390, 844, true),
    ];

    [Fact]
    public async Task Capture_uwv_mockup_screens()
    {
        if (!ShouldRun)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync()
                      ?? throw new InvalidOperationException("Set JOBSY_E2E_BASE_URL to a running Jobsy.Web instance.");

        Directory.CreateDirectory(OutDir);
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var cap in Captures)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = cap.Width, Height = cap.Height },
                DeviceScaleFactor = 1,
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(60_000);
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await page.GotoAsync(baseUrl.TrimEnd('/') + cap.Path, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-uwv-screen]", new() { Timeout = 30_000 });
            await page.EvaluateAsync("""
                () => {
                  document.querySelectorAll('.cookie-consent, .cookie-banner, [data-testid="cookie-consent"]').forEach(el => el.remove());
                }
                """);
            var path = Path.Combine(OutDir, cap.FileSlug + ".png");
            await page.ScreenshotAsync(new() { Path = path, FullPage = cap.FullPage });
        }

        await BuildPerScreenContactSheetsAsync(OutDir);
    }

    private static async Task BuildPerScreenContactSheetsAsync(string dir)
    {
        var pngs = Directory.GetFiles(dir, "*.png")
            .Where(p => !p.Contains("contact-sheet", StringComparison.OrdinalIgnoreCase))
            .Select(p => Path.GetFileName(p)!)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
        if (pngs.Count == 0)
        {
            return;
        }

        var groups = pngs
            .GroupBy(f =>
            {
                var parts = f.Split('-');
                return parts.Length >= 2 ? $"{parts[0]}-{parts[1]}" : parts[0];
            })
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        var scriptPath = Path.Combine(dir, "_build_contact_sheets.py");
        await File.WriteAllTextAsync(scriptPath, """
            import sys
            from pathlib import Path
            from PIL import Image

            out_dir = Path(sys.argv[1])
            files = sorted(out_dir.glob("*.png"))
            files = [f for f in files if "contact-sheet" not in f.name]

            import re

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

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "python3",
            Arguments = $"\"{scriptPath}\" \"{dir}\"",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc is not null)
        {
            await proc.WaitForExitAsync();
        }
    }
}
