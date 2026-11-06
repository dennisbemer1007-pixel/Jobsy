using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class ProfileEncouragementTipPlaywrightTests
{
    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_ENCourage_TIPS"), "1", StringComparison.Ordinal);

    [Theory]
    [InlineData("wist-tests-match", "desktop-1440", false)]
    [InlineData("wist-cv-vs-passport", "desktop-1440", false)]
    [InlineData("wist-day-in-life", "mobile-390", true)]
    public async Task Capture_encourage_tip_screenshot(string tipId, string tag, bool mobile)
    {
        if (!ShouldRun)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync()
                      ?? throw new InvalidOperationException("JOBSY_E2E_BASE_URL unreachable");
        var email = $"e2e-enc-{tipId}@jobsy.local";
        await CandidateJourneyE2e.EnsureDemoApplyProfileReadyAsync(email);
        if (tipId.StartsWith("wist-tests", StringComparison.Ordinal))
        {
            await CandidateJourneyE2e.ResetDemoCompetencyForE2eAsync(email);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var vp = mobile ? KbE2e.Mobile : KbE2e.Desktop;
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = vp.Width, Height = vp.Height },
            DeviceScaleFactor = mobile ? 2 : 1
        });
        var page = await ctx.NewPageAsync();
        await CandidateJourneyE2e.SignInExistingCandidateViaEmailCodeAsync(page, baseUrl, email);
        await page.EvaluateAsync(
            """
            () => {
              for (const k of Object.keys(localStorage)) {
                if (k.startsWith('lobsy-encourage:')) localStorage.removeItem(k);
              }
            }
            """);

        var path = tipId == "wist-day-in-life"
            ? $"{baseUrl}/vacancies/{await ResolveAnyVacancyIdAsync(page, baseUrl)}?encouragePreview={tipId}#apply"
            : $"{baseUrl}/vacancies/{await ResolveAnyVacancyIdAsync(page, baseUrl)}?encouragePreview={tipId}#apply";

        await page.GotoAsync(path);
        await KbE2e.WaitForStyledAppAsync(page);
        await page.Locator($"[data-encourage-tip='{tipId}']").WaitForAsync(new() { Timeout = 45_000 });
        var outDir = "/cursor/stores/self/fixes/encourage-tips";
        Directory.CreateDirectory(outDir);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"{tag}-{tipId}.png"),
            FullPage = false
        });
    }

    private static async Task<Guid> ResolveAnyVacancyIdAsync(IPage page, string baseUrl)
    {
        await page.GotoAsync(baseUrl + "/banenkaart?weergave=lijst");
        await KbE2e.WaitForStyledAppAsync(page);
        var link = page.Locator("a[href*='/vacancies/']").First;
        var href = await link.GetAttributeAsync("href") ?? "";
        var id = href.Split('/').LastOrDefault(s => Guid.TryParse(s, out _));
        return Guid.Parse(id!);
    }
}
