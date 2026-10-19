using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Captures UWV werkmap mockup PNGs into <c>uwv-mockups/</c> at repo root.
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
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "uwv-mockups"))
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

        await BuildContactSheetAsync(OutDir);
    }

    private static async Task BuildContactSheetAsync(string dir)
    {
        var pngs = Directory.GetFiles(dir, "*.png")
            .Where(p => !p.Contains("contact-sheet", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
        if (pngs.Count == 0)
        {
            return;
        }

        var sheetPath = Path.Combine(dir, "00-contact-sheet.png");
        try
        {
            var args = string.Join(' ', pngs.Select(p => $"\"{p}\""));
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "montage",
                Arguments = $"-tile 4x -geometry 320x200+4+4 -background '#f5f2ee' {args} \"{sheetPath}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is not null)
            {
                await proc.WaitForExitAsync();
                if (proc.ExitCode == 0 && File.Exists(sheetPath))
                {
                    return;
                }
            }
        }
        catch
        {
            // fallback below
        }

    }
}
