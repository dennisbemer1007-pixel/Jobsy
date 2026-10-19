using Jobsy.Infrastructure.Data;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Desktop 1440 and mobile 390 screenshots for Na de match live routes vs mockups.
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
            ? "/opt/cursor/artifacts/nadematch-r11"
            : Environment.GetEnvironmentVariable("JOBSY_NADMATCH_ARTIFACTS")!;

    private static string MockupRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "na-de-match-package", "out", "na-de-match"));

    private const string CandidateEmail = "kandidaat.denhaag@jobsy.local";
    private const string EmployerEmail = "ondernemer@jobsy.local";
    private const string Password = "Jobsy123!";

    private static readonly (string Id, string Path, string Role)[] Routes =
    [
        ("01", $"/candidate/applications/{AfterMatchDemoIds.WaitApp:D}/after-match", "kandidaat"),
        ("02", $"/werkgever/sollicitaties/{AfterMatchDemoIds.WaitApp:D}/after-match", "werkgever"),
        ("03", $"/werkgever/sollicitaties/{AfterMatchDemoIds.WaitApp:D}/after-match?panel=propose", "werkgever"),
        ("04", $"/candidate/applications/{AfterMatchDemoIds.PickSlotsApp:D}/after-match", "kandidaat"),
        ("05", $"/candidate/applications/{AfterMatchDemoIds.ScheduledApp:D}/after-match", "kandidaat"),
        ("06", $"/candidate/applications/{AfterMatchDemoIds.ScheduledApp:D}/after-match/bevestiging", "kandidaat"),
        ("07", $"/candidate/applications/{AfterMatchDemoIds.ScheduledApp:D}/after-match/herinneringen", "kandidaat"),
        ("08", $"/candidate/applications/{AfterMatchDemoIds.ScheduledApp:D}/after-match?panel=reschedule", "kandidaat"),
        ("09", $"/werkgever/sollicitaties/{AfterMatchDemoIds.RescheduleApp:D}/after-match", "werkgever"),
        ("10", $"/candidate/applications/{AfterMatchDemoIds.WaitApp:D}/after-match?panel=messages", "kandidaat"),
        ("11", $"/werkgever/sollicitaties/{AfterMatchDemoIds.WaitApp:D}/after-match?panel=messages", "werkgever"),
        ("12", $"/werkgever/sollicitaties/{AfterMatchDemoIds.ScheduledApp:D}/after-match?panel=outcome", "werkgever"),
        ("13", $"/candidate/applications/{AfterMatchDemoIds.NotNowApp:D}/after-match", "kandidaat"),
    ];

    [Fact]
    public async Task Capture_na_de_match_live_routes_per_role_viewport()
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

        var mobileWidths = new[] { (390, "mobiel"), (360, "mobiel-360") };
        // kandidaat: 390/360 full + viewport-top each; werkgever: desktop full
        var expectedCaptures = Routes.Count(r => r.Role == "werkgever")
                               + Routes.Count(r => r.Role == "kandidaat") * 4;

        await using (var candidateCtx = await browser.NewContextAsync(KbE2e.ContextFor(390, 844)))
        {
            await PlaywrightCookieConsent.AcceptAsync(candidateCtx);
            var candidatePage = await candidateCtx.NewPageAsync();
            candidatePage.SetDefaultTimeout(60_000);
            await PlaywrightCookieConsent.AcceptOnPageAsync(candidatePage);
            Assert.True(await KbE2e.TryLoginAsync(candidatePage, baseUrl, CandidateEmail, Password));

            foreach (var route in Routes.Where(r => r.Role == "kandidaat"))
            {
                foreach (var (width, tag) in mobileWidths)
                {
                    await CaptureOnPageAsync(candidatePage, baseUrl, route, width, 844, tag, compareDir);
                }
            }
        }

        await using (var employerCtx = await browser.NewContextAsync(KbE2e.ContextFor(1440, 900)))
        {
            await PlaywrightCookieConsent.AcceptAsync(employerCtx);
            var employerPage = await employerCtx.NewPageAsync();
            employerPage.SetDefaultTimeout(60_000);
            await PlaywrightCookieConsent.AcceptOnPageAsync(employerPage);
            Assert.True(await KbE2e.TryLoginAsync(employerPage, baseUrl, EmployerEmail, Password));

            foreach (var route in Routes.Where(r => r.Role == "werkgever"))
            {
                await CaptureOnPageAsync(employerPage, baseUrl, route, 1440, 900, "desktop", compareDir);
            }
        }

        Assert.Equal(expectedCaptures, Directory.GetFiles(OutDir, "*-capture-*.png").Length);
    }

    private static async Task CaptureOnPageAsync(
        IPage page,
        string baseUrl,
        (string Id, string Path, string Role) route,
        int width,
        int height,
        string tag,
        string compareDir)
    {
        await page.SetViewportSizeAsync(width, height);

        await page.GotoAsync($"{baseUrl.TrimEnd('/')}{route.Path}", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator("[data-testid=ndm-candidate-live], [data-testid=ndm-werkgever-live]").WaitForAsync(new() { Timeout = 90_000 });
        await AssertCapturePageHealthyAsync(page, route.Id, route.Role);
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        await PrepareCaptureChromeAsync(page, route.Role);
        await page.WaitForTimeoutAsync(400);
        var file = Path.Combine(OutDir, $"{route.Id}-capture-{tag}.png");
        await page.ScreenshotAsync(new() { Path = file, FullPage = true });
        if (tag is "mobiel" or "mobiel-360")
        {
            var topFile = Path.Combine(OutDir, $"{route.Id}-capture-{tag}-top.png");
            await page.ScreenshotAsync(new() { Path = topFile, FullPage = false });
        }

        if (tag is "mobiel" or "desktop")
        {
            CopyMockupSideBySide(route.Id, tag, file, compareDir);
        }
    }

    private static async Task PrepareCaptureChromeAsync(IPage page, string role)
    {
        if (role == "kandidaat")
        {
            await page.EvaluateAsync(
                "document.documentElement.setAttribute('data-ndm-screenshot','1');");
        }
    }

    private static async Task AssertCapturePageHealthyAsync(IPage page, string screenId, string role)
    {
        var body = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain("404", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Pagina niet gevonden", body, StringComparison.OrdinalIgnoreCase);
        var loginVisible = await page.GetByText("Inloggen", new() { Exact = true }).IsVisibleAsync();
        Assert.False(loginVisible, $"Screen {screenId} still shows login for {role}");
        var cookieBanner = page.Locator("[data-testid=cookie-consent-banner]");
        if (await cookieBanner.CountAsync() > 0)
        {
            Assert.False(await cookieBanner.IsVisibleAsync(), $"Screen {screenId} still shows cookie banner");
        }
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
