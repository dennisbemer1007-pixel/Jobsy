using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>PR #1488 paspoort plain-layout evidence (local illustrated host + stub profile).</summary>
[Collection("PlaywrightSmoke")]
public sealed class Pr1488PaspoortEvidencePlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        if (!ShouldRunAfter() && !ShouldRunBefore() && !ShouldRun1488b())
        {
            return;
        }

        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    private static bool ShouldRunAfter()
        => string.Equals(Environment.GetEnvironmentVariable("JOBSY_PR1488_CAPTURE"), "1", StringComparison.Ordinal);

    private static bool ShouldRunBefore()
        => string.Equals(Environment.GetEnvironmentVariable("JOBSY_PR1488_BEFORE"), "1", StringComparison.Ordinal);

    private static bool ShouldRun1488b()
        => string.Equals(Environment.GetEnvironmentVariable("JOBSY_PR1488B_CAPTURE"), "1", StringComparison.Ordinal);

    [Fact(Timeout = 300_000)]
    public async Task Capture_pr1488b_viewport_paspoort_coach()
    {
        _ = TestContext.Current.CancellationToken;
        if (!ShouldRun1488b())
        {
            return;
        }

        Environment.SetEnvironmentVariable("JOBSY_PR1488_CAPTURE", "1");
        var outDir = Environment.GetEnvironmentVariable("JOBSY_PR1488_EVIDENCE_DIR")
                     ?? "/opt/cursor/artifacts/pr1488b";
        Directory.CreateDirectory(outDir);

        await using var factory = new IllustratedPagesWebFactory();
        var root = factory.EnsureServerAddress();

        foreach (var (w, h, tag) in new[] { (1280, 800, "1280"), (390, 844, "390") })
        {
            await using var context = await NewContextAsync(w, h);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            await SignInAsync(page, root, "/candidate/paspoort?tab=dna");
            await ViewportShotAsync(page, root, "/candidate/paspoort?tab=dna", outDir,
                $"paspoort-wie-ik-ben-{tag}.png");

            await ViewportShotAsync(page, root, "/candidate/paspoort?tab=proof", outDir,
                $"paspoort-bewijzen-{tag}.png");
        }
    }

    [Fact(Timeout = 600_000)]
    public async Task Capture_pr1488_after_plain_paspoort()
    {
        _ = TestContext.Current.CancellationToken;
        if (!ShouldRunAfter())
        {
            return;
        }

        var outDir = Environment.GetEnvironmentVariable("JOBSY_PR1488_EVIDENCE_DIR")
                     ?? "/opt/cursor/artifacts/pr1488/after";
        Directory.CreateDirectory(outDir);

        await using var factory = new IllustratedPagesWebFactory();
        var root = factory.EnsureServerAddress();

        foreach (var (w, h, tag) in new[] { (1280, 800, "1280"), (390, 844, "390") })
        {
            await using var context = await NewContextAsync(w, h);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            await SignInAsync(page, root, "/candidate/paspoort?tab=dna");
            await ShotAsync(page, root, "/candidate/paspoort?tab=dna", outDir, $"paspoort-wie-ik-ben-{tag}.png",
                ".passport-page--cv-plain, .cand-hero--passport-plain, .kompas-tip--plain");

            await ShotAsync(page, root, "/candidate/paspoort?tab=proof", outDir, $"paspoort-bewijzen-{tag}.png",
                "[data-testid=passport-cv-proposal], .passport-page--cv-plain");

            await ShotAsync(page, root, "/candidate/paspoort?tab=data", outDir, $"paspoort-gegevens-{tag}.png",
                ".passport-page--cv-plain, .passport-data");
        }

        await CaptureGeboortedatumAsync(outDir);
        await CaptureMockInterviewAsync(outDir, root);
    }

    [Fact(Timeout = 300_000)]
    public async Task Capture_pr1488_before_acceptatie_baseline()
    {
        _ = TestContext.Current.CancellationToken;
        if (!ShouldRunBefore())
        {
            return;
        }

        var outDir = Environment.GetEnvironmentVariable("JOBSY_PR1488_EVIDENCE_DIR")
                     ?? "/opt/cursor/artifacts/pr1488/before";
        Directory.CreateDirectory(outDir);

        await using var factory = new IllustratedPagesWebFactory();
        var root = factory.EnsureServerAddress();

        await using var context = await NewContextAsync(1280, 800);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await SignInAsync(page, root, "/candidate/paspoort?tab=dna");
        await ShotAsync(page, root, "/candidate/paspoort?tab=dna", outDir, "paspoort-wie-ik-ben-1280.png",
            ".kompas-tip, .cand-hero");
        await ShotAsync(page, root, "/candidate/paspoort?tab=proof", outDir, "paspoort-bewijzen-1280.png",
            ".kompas-card-spot, [data-testid=passport-tabs]");
    }

    private async Task CaptureGeboortedatumAsync(string outDir)
    {
        Environment.SetEnvironmentVariable("JOBSY_PR1488_CAPTURE", null);
        Environment.SetEnvironmentVariable("JOBSY_PR1488_CAPTURE_DOB", "1");

        await using var factory = new IllustratedPagesWebFactory();
        var root = factory.EnsureServerAddress();

        foreach (var (w, h, tag) in new[] { (1280, 800, "1280"), (390, 844, "390") })
        {
            await using var context = await NewContextAsync(w, h);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);
            await SignInAsync(page, root, "/candidate/geboortedatum");
            await ShotAsync(page, root, "/candidate/geboortedatum", outDir, $"geboortedatum-{tag}.png",
                ".candidate-dob-gate--plain, .candidate-dob-gate");
        }

        Environment.SetEnvironmentVariable("JOBSY_PR1488_CAPTURE_DOB", null);
        Environment.SetEnvironmentVariable("JOBSY_PR1488_CAPTURE", "1");
    }

    private async Task CaptureMockInterviewAsync(string outDir, string _)
    {
        var vacancyId = IllustratedPagesApiStub.SampleVacancyId;
        await using var factory = new IllustratedPagesWebFactory();
        var root = factory.EnsureServerAddress();

        foreach (var (w, h, tag) in new[] { (1280, 800, "1280"), (390, 844, "390") })
        {
            await using var context = await NewContextAsync(w, h);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);
            await SignInAsync(page, root, $"/vacancies/{vacancyId:D}");
            await page.GotoAsync($"{root}/vacancies/{vacancyId:D}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForSelectorAsync("[data-testid=vacancy-mock-interview-cta]", new() { Timeout = 90_000 });
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(outDir, $"vacancy-mock-interview-entry-{tag}.png"),
                FullPage = true
            });

            var cta = page.GetByTestId("vacancy-mock-interview-cta").Locator("button").First;
            await cta.ScrollIntoViewIfNeededAsync();
            await cta.ClickAsync(new() { Force = true });
            var modal = page.Locator(".mock-interview-modal, [data-testid=mock-interview-modal]").First;
            await modal.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(outDir, $"mock-interview-modal-{tag}.png"),
                FullPage = true
            });
        }
    }

    private async Task<IBrowserContext> NewContextAsync(int w, int h)
    {
        var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = w, Height = h },
            IgnoreHTTPSErrors = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        return context;
    }

    private static async Task SignInAsync(IPage page, string root, string returnPath)
    {
        var url =
            $"{root.TrimEnd('/')}/__test/sign-in?role=candidate&return={Uri.EscapeDataString(returnPath)}";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
    }

    private static async Task ShotAsync(
        IPage page,
        string root,
        string path,
        string outDir,
        string fileName,
        string waitSelector)
    {
        await page.GotoAsync($"{root}{path}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(waitSelector.Split(',')[0].Trim()).First
            .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(400);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, fileName),
            FullPage = true
        });
    }

    private static async Task ViewportShotAsync(
        IPage page,
        string root,
        string path,
        string outDir,
        string fileName)
    {
        await page.GotoAsync($"{root}{path}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid=passport-page-ssr], .passport-page--cv-plain").First
            .WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
        await page.Locator(".lobsy-coach-dock--variant-b .lobsy-coach-dock__lobster").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(350);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, fileName),
            FullPage = false
        });
    }
}
