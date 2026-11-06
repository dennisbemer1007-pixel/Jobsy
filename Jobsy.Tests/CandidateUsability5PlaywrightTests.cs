using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Proves vacancy apply submit via UI only (10× desktop + 10× mobile), fresh candidate + vacancy per run.
/// Artifacts: usability5/. Run:
/// <c>JOBSY_E2E_USABILITY5=1 JOBSY_E2E_BASE_URL=http://127.0.0.1:5201 ConnectionStrings__JobsyDb=… dotnet test --filter Usability5</c>
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateUsability5PlaywrightTests
{
    private static int RunsPerViewport =>
        int.TryParse(Environment.GetEnvironmentVariable("JOBSY_E2E_USABILITY5_RUNS"), out var n) && n > 0
            ? n
            : 10;

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_USABILITY5"), "1", StringComparison.Ordinal);

    [Fact]
    public async Task Usability5_apply_ui_submit_10x_desktop_and_mobile()
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

        Environment.SetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT", "/workspace/artifacts/usability5");
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
            for (var run = 0; run < RunsPerViewport; run++)
            {
                var candidate = await CandidateJourneyE2e.SeedFreshApplyCandidateAsync(tag, run);
                Assert.NotNull(candidate);
                var (email, password) = candidate.Value;

                await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
                await PlaywrightCookieConsent.AcceptAsync(context);
                var page = await context.NewPageAsync();
                page.SetDefaultTimeout(120_000);

                Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, email, password));
                await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
                await RunSingleUiApplyAsync(page, baseUrl, tag, run, email);
            }

            await using var doneContext = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
            await PlaywrightCookieConsent.AcceptAsync(doneContext);
            var doneCandidate = await CandidateJourneyE2e.SeedFreshApplyCandidateAsync(tag, RunsPerViewport);
            Assert.NotNull(doneCandidate);
            var donePage = await doneContext.NewPageAsync();
            donePage.SetDefaultTimeout(120_000);
            Assert.True(await KbE2e.TryLoginAsync(donePage, baseUrl, doneCandidate.Value.Email, doneCandidate.Value.Password));
            var lastVacancy = await CandidateJourneyE2e.SeedFreshVacancyFixtureAsync(
                baseUrl,
                doneCandidate.Value.Email,
                $"Usability5-{tag}-done");
            Assert.NotNull(lastVacancy);
            await donePage.GotoAsync($"{baseUrl}/vacancies/{lastVacancy:D}");
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(donePage);
            await Shot(donePage, tag, "apply-ui-10x-done", after: true);
            await KbE2e.AssertNoCircuitErrorAsync(donePage);
        }

        CopyArtifactsToRepoAndStore();
    }

    private static async Task RunSingleUiApplyAsync(IPage page, string baseUrl, string tag, int run, string candidateEmail)
    {
        var vacancyId = await CandidateJourneyE2e.SeedFreshVacancyFixtureAsync(
            baseUrl,
            candidateEmail,
            $"Usability5-{tag}-{run}");
        Assert.NotNull(vacancyId);

        await page.GotoAsync($"{baseUrl}/vacancies/{vacancyId:D}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await page.EvaluateAsync("() => document.getElementById('apply')?.scrollIntoView({ block: 'center' })");

        if (run == 0)
        {
            await Shot(page, tag, "apply-before-first-run");
        }

        var stickyApply = page.Locator("[data-testid=kb-detail-apply]");
        if (await stickyApply.CountAsync() > 0)
        {
            await stickyApply.First.ClickAsync(new() { Force = true });
        }

        await page.WaitForTimeoutAsync(600);
        await CandidateJourneyE2e.SubmitVacancyApplyViaUiAsync(page, baseUrl, candidateEmail);

        if (run == RunsPerViewport - 1)
        {
            await Shot(page, tag, "apply-after-last-run", after: true);
        }
    }

    private static async Task Shot(IPage page, string tag, string slug, bool after = false)
    {
        var prefix = after ? "after" : "before";
        var path = Path.Combine(CandidateJourneyE2e.ShotDir(tag), $"{prefix}-{slug}.png");
        await CandidateJourneyE2e.ShotViewportAsync(page, path);
    }

    private static void CopyArtifactsToRepoAndStore()
    {
        var srcRoot = "/workspace/artifacts/usability5";
        foreach (var destRoot in new[] { "/workspace/usability5", "/cursor/stores/self/usability5" })
        {
            if (!Directory.Exists(srcRoot))
            {
                continue;
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
                    // store mount may lock open PNGs
                }
            }
        }
    }
}
