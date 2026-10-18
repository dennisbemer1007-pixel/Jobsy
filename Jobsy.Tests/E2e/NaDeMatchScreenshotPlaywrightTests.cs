using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Desktop 1440 and mobile 390 screenshots for Na de match dev previews vs mockups.
/// Run: JOBSY_NADMATCH_SCREENSHOTS=1 JOBSY_E2E_BASE_URL=http://127.0.0.1:5201
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class NaDeMatchScreenshotPlaywrightTests
{
    private const string EnvRun = "JOBSY_NADMATCH_SCREENSHOTS";

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    private static string OutDir =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_NADMATCH_ARTIFACTS"))
            ? "/opt/cursor/artifacts/nadematch-r1"
            : Environment.GetEnvironmentVariable("JOBSY_NADMATCH_ARTIFACTS")!;

    private static string MockupRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "na-de-match-package", "out", "na-de-match"));

    private static readonly string[] ScreenIds =
        ["01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13"];

    [Fact]
    public async Task Capture_na_de_match_previews_desktop_and_mobile()
    {
        if (!ShouldRun)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync()
                      ?? throw new InvalidOperationException("JOBSY_E2E_BASE_URL unreachable");

        Directory.CreateDirectory(OutDir);
        var compareDir = Path.Combine(OutDir, "compare");
        Directory.CreateDirectory(compareDir);

        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in new[] { (1440, 900, "desktop"), (390, 844, "mobiel") })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                Locale = "nl-NL",
            });
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(60_000);
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);

            foreach (var id in ScreenIds)
            {
                await page.GotoAsync($"{baseUrl.TrimEnd('/')}/dev/na-de-match/{id}", new()
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                });
                await page.Locator("[data-testid=ndm-preview]").WaitForAsync(new() { Timeout = 30_000 });
                await page.WaitForTimeoutAsync(400);
                var file = Path.Combine(OutDir, $"{id}-capture-{tag}.png");
                await page.ScreenshotAsync(new() { Path = file, FullPage = true });
                CopyMockupSideBySide(id, tag, file, compareDir);
            }
        }

        Assert.True(Directory.GetFiles(OutDir, "*-capture-*.png").Length >= ScreenIds.Length * 2);
    }

    private static void CopyMockupSideBySide(string screenId, string tag, string capturePath, string compareDir)
    {
        var mockName = tag == "desktop"
            ? $"{screenId}-*-desktop.png"
            : $"{screenId}-*-mobiel.png";
        var dir = MockupRoot;
        if (!Directory.Exists(dir))
        {
            return;
        }

        var mock = Directory.GetFiles(dir, mockName).OrderBy(f => f, StringComparer.Ordinal).FirstOrDefault();
        if (mock is null)
        {
            return;
        }

        var destMock = Path.Combine(compareDir, $"{screenId}-mockup-{tag}.png");
        File.Copy(mock, destMock, overwrite: true);
        TrySideBySide(capturePath, destMock, Path.Combine(compareDir, $"{screenId}-side-by-side-{tag}.png"));
    }

    private static void TrySideBySide(string leftPath, string rightPath, string outPath)
    {
        if (!File.Exists(leftPath) || !File.Exists(rightPath))
        {
            return;
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                ArgumentList =
                {
                    "-c",
                    """
import sys
from PIL import Image
left, right, out = sys.argv[1:4]
a = Image.open(left).convert('RGB')
b = Image.open(right).convert('RGB')
h = max(a.height, b.height)

def pad(im):
    if im.height == h:
        return im
    canvas = Image.new('RGB', (im.width, h), (245, 245, 245))
    canvas.paste(im, (0, 0))
    return canvas

a, b = pad(a), pad(b)
combo = Image.new('RGB', (a.width + b.width + 8, h), (32, 32, 32))
combo.paste(a, (0, 0))
combo.paste(b, (a.width + 8, 0))
combo.save(out)
""",
                    leftPath,
                    rightPath,
                    outPath
                },
                RedirectStandardError = true,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            p?.WaitForExit(30_000);
        }
        catch
        {
            // Pillow optional in CI — screenshots still land in OutDir.
        }
    }
}
