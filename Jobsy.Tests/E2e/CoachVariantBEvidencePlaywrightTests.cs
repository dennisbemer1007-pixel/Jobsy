using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Dennis evidence pack — screenshots under JOBSY_COACH_EVIDENCE_DIR.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CoachVariantBEvidencePlaywrightTests
{
    [Fact(Timeout = 600_000)]
    public async Task Capture_coach_variant_b_evidence_pack()
    {
        _ = TestContext.Current.CancellationToken;
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await CoachVariantBOverlapAuditSupport.EnsureSchoolsEnabledAsync();

        var outDir = Environment.GetEnvironmentVariable("JOBSY_COACH_EVIDENCE_DIR")
                     ?? "/opt/cursor/artifacts/coach-variant-b-evidence";
        Directory.CreateDirectory(outDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (w, h, tag) in new[] { (390, 844, "390"), (1440, 900, "1440") })
        {
            await using var ctx = await browser.NewContextAsync(KbE2e.ContextFor(w, h));
            await PlaywrightCookieConsent.AcceptAsync(ctx);
            var page = await ctx.NewPageAsync();
            page.SetDefaultTimeout(120_000);
            Assert.True(await CoachVariantBOverlapAuditSupport.LoginAsync(page, baseUrl, "onboarding.e2e@jobsy.local"));

            foreach (var step in new[] { 1, 2 })
            {
                await page.GotoAsync($"{baseUrl}/candidate/start?step={step}", new() { WaitUntil = WaitUntilState.NetworkIdle });
                await page.WaitForTimeoutAsync(500);
                await page.ScreenshotAsync(new() { Path = Path.Combine(outDir, $"onboarding-step-{step:D2}-{tag}.png") });
            }

            await page.GotoAsync($"{baseUrl}/candidate/start?step=1", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForFunctionAsync(
                "() => document.documentElement.dataset.ssrCoachBound === '1'",
                null,
                new() { Timeout = 30_000 });
            var coach = page.Locator("#lobsy-coach-btn, [data-ssr-coach-open]").First;
            await coach.ClickAsync(new() { Force = true });
            await page.Locator("#lobsy-assistant-panel").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20_000 });
            await page.ScreenshotAsync(new() { Path = Path.Combine(outDir, $"coach-open-step01-{tag}.png") });
            await page.Locator("[data-ssr-assistant-close]").First.ClickAsync();
        }

        await using (var mboCtx = await browser.NewContextAsync(KbE2e.MobileContext()))
        {
            var mboPage = await mboCtx.NewPageAsync();
            var seed = await CoachVariantBOverlapAuditSupport.SeedFreshMboPupilAsync(browser, baseUrl);
            Assert.NotNull(seed);
            Assert.True(await CoachVariantBOverlapAuditSupport.PupilLoginAsync(
                mboPage, baseUrl, seed.Value.SchoolId, seed.Value.ClassId, seed.Value.Code));
            await mboPage.GotoAsync($"{baseUrl}/leerling/mbo-hbo/overzicht", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await mboPage.WaitForTimeoutAsync(800);
            await mboPage.ScreenshotAsync(new() { Path = Path.Combine(outDir, "mbo-overzicht-390.png") });
        }

        var vacancyId = await CandidateJourneyE2e.SeedFreshVacancyFixtureAsync(baseUrl, KbE2e.DefaultCompleteEmail, "coach-b-evidence");
        Assert.NotNull(vacancyId);
        foreach (var (w, h, tag) in new[] { (390, 844, "390"), (1440, 900, "1440") })
        {
            await using var ctx = await browser.NewContextAsync(KbE2e.ContextFor(w, h));
            var page = await ctx.NewPageAsync();
            Assert.True(await CoachVariantBOverlapAuditSupport.LoginAsync(page, baseUrl, KbE2e.DefaultCompleteEmail));
            await page.GotoAsync($"{baseUrl}/vacancies/{vacancyId:D}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
            await page.WaitForSelectorAsync("[data-testid=vacancy-mock-interview-cta]", new() { Timeout = 60_000 });
            await page.ScreenshotAsync(new() { Path = Path.Combine(outDir, $"vacancy-mock-interview-{tag}.png") });
            var cta = page.GetByTestId("vacancy-mock-interview-cta").Locator("button").First;
            await cta.ScrollIntoViewIfNeededAsync();
            await cta.ClickAsync(new() { Force = true });
            var modal = page.Locator(".mock-interview-modal, [data-testid=mock-interview-modal]").First;
            try
            {
                await modal.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            }
            catch
            {
                await page.EvaluateAsync(
                    "() => document.querySelector('[data-testid=vacancy-mock-interview-cta] button')?.click()");
                await modal.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            }

            await page.ScreenshotAsync(new() { Path = Path.Combine(outDir, $"mock-interview-open-{tag}.png") });
        }

        var g78 = await SeedGroep78PupilForEvidenceAsync(browser, baseUrl);
        Assert.NotNull(g78);
        await using var g78Ctx = await browser.NewContextAsync(KbE2e.DesktopContext());
        var g78Page = await g78Ctx.NewPageAsync();
        Assert.True(await CoachVariantBOverlapAuditSupport.PupilLoginAsync(
            g78Page, baseUrl, g78.Value.SchoolId, g78.Value.ClassId, g78.Value.Code));
        await g78Page.GotoAsync(
            $"{baseUrl}/leerling/reis",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await g78Page.Locator(
                "[data-testid=ll-g78-coach], [data-testid=ll-g78-coach-mobile], [data-testid=ll-g78-answers] .ans")
            .First.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
        await g78Page.WaitForFunctionAsync(
            """
            () => document.querySelector('[data-testid=ll-g78-coach]')
              || document.querySelector('[data-testid=ll-g78-coach-mobile]')
            """,
            null,
            new() { Timeout = 90_000 });
        await g78Page.ScreenshotAsync(new() { Path = Path.Combine(outDir, "g78-coach-unchanged-1440.png") });
    }

    private static async Task<(Guid SchoolId, Guid ClassId, string Code)?> SeedGroep78PupilForEvidenceAsync(
        IBrowser browser,
        string baseUrl)
    {
        _ = browser;
        _ = baseUrl;
        await CoachVariantBOverlapAuditSupport.EnsureSchoolsEnabledAsync();
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            return null;
        }

        var repoRoot = KbE2e.FindRepoRoot();
        var contentRoot = Path.Combine(repoRoot, "Jobsy.Web");
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var schoolClass = await db.SchoolClasses
            .FirstOrDefaultAsync(c => c.Id == DemoSchoolIds.ClassGroep78Id);
        if (schoolClass is null)
        {
            return null;
        }

        if (schoolClass.TestWindow != TestWindowState.Open)
        {
            schoolClass.TestWindow = TestWindowState.Open;
            await db.SaveChangesAsync();
        }

        var config = new ConfigurationBuilder().Build();
        var env = new CoachVariantBOverlapAuditSupport.E2eWebContentHostEnvironment(contentRoot);
        var dp = DataProtectionProvider.Create(Path.Combine(contentRoot, "dp-keys-e2e-audit"));
        var codes = new PupilCodeService(db, dp, config, env);
        var created = await codes.GenerateAsync(1, schoolClass);
        var codeRow = created[0];
        var now = DateTime.UtcNow;
        db.PupilProgresses.Add(new PupilProgress
        {
            PupilCodeId = codeRow.Id,
            JourneyThemeKey = PupilJourneyTheme.ZeeExpeditie,
            JourneyThemeChosenAtUtc = now,
            CurrentIndex = 0,
            AnswersJson = "{}",
            LikesJson = "[]",
            DislikesJson = "[]",
            StartedAtUtc = now,
            UpdatedAtUtc = now,
            QuestionScoringVersion = "g78-1",
        });
        await db.SaveChangesAsync();
        var plain = codes.Unprotect(codeRow.CodeProtected);
        return string.IsNullOrWhiteSpace(plain)
            ? null
            : (DemoSchoolIds.SchoolId, DemoSchoolIds.ClassGroep78Id, PupilCodeFormat.Display(plain!));
    }
}
