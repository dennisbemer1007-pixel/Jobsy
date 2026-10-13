using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Usability follow-up 3: mobile PDF, full 25-Q competency DNA, fresh apply confirmation.
/// Artifacts: <c>/cursor/stores/self/usability3</c>.
/// Run: <c>JOBSY_E2E_USABILITY3=1 JOBSY_E2E_BASE_URL=http://127.0.0.1:5201 ConnectionStrings__JobsyDb=… dotnet test --filter Usability3</c>
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateUsability3PlaywrightTests
{
    private const string DemoEmail = "kandidaat@jobsy.local";

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_USABILITY3"), "1", StringComparison.Ordinal);

    [Fact]
    public async Task Usability3_pdf_dna_apply_desktop_and_mobile()
    {
        if (!ShouldRun)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable");
        }

        Environment.SetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT", "/workspace/artifacts/usability3");
        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);
        Assert.True(await CandidateJourneyE2e.WaitForFlagsAsync(baseUrl));

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in new[]
                 {
                     (KbE2e.Desktop.Width, KbE2e.Desktop.Height, "desktop"),
                     (KbE2e.Mobile.Width, KbE2e.Mobile.Height, "mobile")
                 })
        {
            await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl));
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);

            await VisitMobilePdfDownloadAsync(page, baseUrl, tag);
            await VisitFullCompetencyDnaAsync(page, baseUrl, tag);
            await VisitFreshApplySentAsync(page, baseUrl, tag);

            await KbE2e.AssertNoCircuitErrorAsync(page);
        }

        CopyArtifactsToAgentStore();
    }

    private static void CopyArtifactsToAgentStore()
    {
        var srcRoot = "/workspace/artifacts/usability3";
        var destRoot = "/cursor/stores/self/usability3";
        if (!Directory.Exists(srcRoot))
        {
            return;
        }

        Directory.CreateDirectory(destRoot);
        foreach (var srcPath in Directory.EnumerateFiles(srcRoot, "*.png", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(srcRoot, srcPath);
            var destPath = Path.Combine(destRoot, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            try
            {
                File.Copy(srcPath, destPath, overwrite: true);
            }
            catch (IOException)
            {
                // Agent store may lock open PNGs; workspace artifacts remain the source of truth.
            }
        }
    }

    private static async Task Shot(IPage page, string tag, string slug, bool after = false)
    {
        var prefix = after ? "after" : "before";
        var path = Path.Combine(CandidateJourneyE2e.ShotDir(tag), $"{prefix}-{slug}.png");
        await CandidateJourneyE2e.ShotViewportAsync(page, path);
    }

    private static async Task VisitMobilePdfDownloadAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=overview", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");

        var downloadBtn = tag == "mobile"
            ? page.Locator("[data-testid=passport-download-pdf-mobile], .passport-layout__card--mobile button:has-text('DNA-paspoort')")
            : page.Locator(".passport-layout__card--desktop [data-testid=passport-download-pdf]");

        await downloadBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await Shot(page, tag, "paspoort-pdf-button");

        await downloadBtn.ScrollIntoViewIfNeededAsync();
        await Assertions.Expect(downloadBtn).ToBeEnabledAsync(new() { Timeout = 60_000 });
        await downloadBtn.ClickAsync(new() { Timeout = 30_000 });
        await page.WaitForTimeoutAsync(1_500);
        await Shot(page, tag, "paspoort-pdf-after-click", after: true);
    }

    private static async Task VisitFullCompetencyDnaAsync(IPage page, string baseUrl, string tag)
    {
        await CandidateJourneyE2e.ResetDemoCompetencyForE2eAsync(DemoEmail);
        await page.GotoAsync($"{baseUrl}/candidate/competencies", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await Shot(page, tag, "competency-intro");

        await AcceptTestConsentIfNeededAsync(page);

        var fullDepth = page.Locator("input[type=radio][name=depth][value='2']");
        if (await fullDepth.CountAsync() > 0)
        {
            await fullDepth.Last.CheckAsync(new() { Force = true });
        }

        var again = page.GetByRole(AriaRole.Button, new() { Name = "Opnieuw" });
        if (await again.CountAsync() > 0 && await again.First.IsVisibleAsync())
        {
            await again.First.ClickAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(800);
        }

        foreach (var label in new[] { "Begin", "Ga verder", "Doorgaan" })
        {
            var begin = page.GetByRole(AriaRole.Button, new() { Name = label, Exact = true });
            if (await begin.CountAsync() > 0 && await begin.IsVisibleAsync())
            {
                await begin.ClickAsync(new() { Force = true });
                await page.WaitForTimeoutAsync(800);
                break;
            }
        }

        await Shot(page, tag, "competency-question-start");

        static bool CompetencyDone(IPage p) =>
            p.Url.Contains("stap=klaar", StringComparison.OrdinalIgnoreCase);

        var doneHeading = page.GetByRole(AriaRole.Heading, new() { Name = "Weer een laag eraf" });
        for (var round = 0; round < 10; round++)
        {
            if (CompetencyDone(page)
                || (await doneHeading.CountAsync() > 0 && await doneHeading.First.IsVisibleAsync()))
            {
                break;
            }

            await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
            await CandidateJourneyE2e.AnswerLikertUntilDoneAsync(page, 15, 3);

            foreach (var label in new[] { "Doorgaan", "Ga verder", "Volgende test", "Bekijk resultaat", "Afronden" })
            {
                var btn = page.GetByRole(AriaRole.Button, new() { Name = label });
                if (await btn.CountAsync() > 0 && await btn.First.IsVisibleAsync())
                {
                    await btn.First.ClickAsync(new() { Force = true });
                    await page.WaitForTimeoutAsync(600);
                }
            }
        }

        await page.WaitForURLAsync(
            url => url.Contains("/candidate/competencies", StringComparison.OrdinalIgnoreCase)
                   && url.Contains("stap=klaar", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 300_000 });
        await doneHeading.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await Shot(page, tag, "competency-25-done", after: true);
    }

    private static async Task AcceptTestConsentIfNeededAsync(IPage page)
    {
        foreach (var label in new[] { "Akkoord", "Ik geef toestemming", "Accept" })
        {
            var accept = page.GetByRole(AriaRole.Button, new() { Name = label });
            if (await accept.CountAsync() > 0 && await accept.First.IsVisibleAsync())
            {
                await accept.First.ClickAsync(new() { Force = true });
                await page.WaitForTimeoutAsync(600);
                break;
            }
        }
    }

    private static async Task VisitFreshApplySentAsync(IPage page, string baseUrl, string tag)
    {
        await CandidateJourneyE2e.EnsureDemoApplyProfileReadyAsync(DemoEmail);
        var vacancyId = await CandidateJourneyE2e.SeedFreshVacancyFixtureAsync(baseUrl, DemoEmail);
        Assert.NotNull(vacancyId);

        await page.GotoAsync($"{baseUrl}/vacancies/{vacancyId:D}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await page.EvaluateAsync(
            """
            () => {
              const apply = document.getElementById('apply') ?? document.querySelector('.apply-flow, .apply-panel');
              apply?.scrollIntoView({ block: 'center', behavior: 'instant' });
            }
            """);
        await Shot(page, tag, "fresh-vacancy-detail");

        var applyBtn = page.GetByRole(AriaRole.Button, new() { Name = "Solliciteer" });
        if (await applyBtn.CountAsync() == 0)
        {
            applyBtn = page.Locator("a:has-text('Solliciteer'), button:has-text('Solliciteer')");
        }

        await applyBtn.First.ClickAsync(new() { Force = true });
        await page.WaitForTimeoutAsync(1_000);
        await Shot(page, tag, "fresh-apply-flow-open");

        await CandidateJourneyE2e.SubmitVacancyApplyViaUiAsync(page, baseUrl, DemoEmail);
        await Shot(page, tag, "fresh-apply-sent", after: true);
    }
}
