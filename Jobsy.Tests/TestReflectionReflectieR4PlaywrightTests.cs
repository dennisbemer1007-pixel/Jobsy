using System.Globalization;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Tests.E2e;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class TestReflectionReflectieR4PlaywrightTests
{
    private const string Email = KbE2e.DefaultCompleteEmail;
    private const string Password = KbE2e.DefaultPassword;
    private const string OutputDir = "/opt/cursor/artifacts/reflectie-r4";
    private const string LegacyR3Dir = "/opt/cursor/artifacts/reflectie-r3";
    private const string BaseUrl = "http://127.0.0.1:5201";

    [Fact]
    public async Task Capture_reflectie_r4_live_layout_when_requested()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_REFLECTION_R4") ?? "").Trim() != "1")
        {
            return;
        }

        if (!await KbE2e.IsReachableAsync(BaseUrl))
        {
            return;
        }

        if (Directory.Exists(LegacyR3Dir))
        {
            Directory.Delete(LegacyR3Dir, recursive: true);
        }

        Directory.CreateDirectory(OutputDir);
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb")
            ?? "Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres";
        Environment.SetEnvironmentVariable("ConnectionStrings__JobsyDb", conn);

        await CandidateJourneyE2e.EnsureTestAiConsentForE2eAsync(Email);
        await SeedSocialCareProfileAsync(Email, "dierenarts");

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await CaptureAsync(browser, 1440, 900, "nl", "1440", fullPage: true, scrollTo: null);
        await CaptureAsync(browser, 1440, 900, "en", "1440", fullPage: true, scrollTo: null);
        await CaptureAsync(browser, 390, 844, "nl", "390-top", fullPage: false, scrollTo: null);
        await CaptureAsync(browser, 390, 844, "nl", "390-mid", fullPage: false, scrollTo: ".test-mirror__card--spot-energy");
    }

    private static async Task CaptureAsync(
        IBrowser browser,
        int width,
        int height,
        string lang,
        string fileSuffix,
        bool fullPage,
        string? scrollTo)
    {
        var mobile = width <= 480;
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = lang == "en" ? "en-GB" : "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        Assert.True(await KbE2e.TryLoginAsync(page, BaseUrl, Email, Password));
        if (!string.Equals(lang, "nl", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync($"{BaseUrl}/taal/{lang}?returnUrl=%2Fprofiel%2Ftests%2Fcompetence", new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 120_000
            });
        }
        else
        {
            await page.GotoAsync($"{BaseUrl}/profiel/tests/competence", new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 120_000
            });
        }

        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await KbE2e.WaitForStyledAppAsync(page);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await page.WaitForSelectorAsync(".test-mirror__title", new() { Timeout = 180_000 });
        await page.WaitForSelectorAsync(".test-result-locked", new() { Timeout = 60_000 });
        await page.EvaluateAsync("() => document.fonts.ready");
        await page.WaitForTimeoutAsync(1500);

        if (!string.IsNullOrWhiteSpace(scrollTo))
        {
            await page.Locator(scrollTo).ScrollIntoViewIfNeededAsync();
            await page.WaitForTimeoutAsync(400);
        }

        var body = await page.InnerTextAsync("body");
        if (lang == "nl")
        {
            Assert.Contains("thema's uit 25 vragen", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("scores uit 25 vragen", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Johnson", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("2.707", body, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Contains("themes from 25 questions", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("scores from 25 questions", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Johnson", body, StringComparison.OrdinalIgnoreCase);
        }

        var lockedHtml = await page.Locator(".test-result-locked").InnerHTMLAsync();
        Assert.DoesNotContain("<strong>", lockedHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("%", lockedHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<small>", lockedHtml, StringComparison.OrdinalIgnoreCase);

        var path = Path.Combine(OutputDir, $"mirror-{lang}-{fileSuffix}.png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = fullPage });
    }

    private static async Task SeedSocialCareProfileAsync(string candidateEmail, string dreamTitle)
    {
        await SeedCompetenceAsync(
            candidateEmail,
            samenwerken: 73,
            resultaat: 72,
            stress: 71,
            innovatie: 38,
            extraversie: 74,
            dreamTitle);
    }

    private static async Task SeedCompetenceAsync(
        string candidateEmail,
        int samenwerken,
        int resultaat,
        int stress,
        int innovatie,
        int extraversie,
        string? dreamTitle)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            return;
        }

        var version = AssessmentTestVersions.V3;
        var full = TestDepthRules.FullCount(AssessmentKind.Competence, version);
        var ids = TestDepthRules.QuestionIdsUpTo(AssessmentKind.Competence, full, version);
        var answers = ids.ToDictionary(id => id.ToString(CultureInfo.InvariantCulture), _ => 4);
        var json = JsonSerializer.Serialize(answers);

        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var normalized = candidateEmail.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized)
            ?? throw new InvalidOperationException($"Demo user {candidateEmail} not found.");

        var now = DateTime.UtcNow;
        var row = await db.CandidateCompetencies.FirstOrDefaultAsync(c => c.UserId == user.Id);
        if (row is null)
        {
            row = new CandidateCompetency
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedAtUtc = now
            };
            db.CandidateCompetencies.Add(row);
        }

        row.Status = CandidateCompetencyStatuses.Completed;
        row.AnswersJson = json;
        row.QuickScanVersion = version;
        row.QuickScanHighestAnswered = ids.Count;
        row.SamenwerkenPercent = samenwerken;
        row.ResultaatgerichtheidPercent = resultaat;
        row.StressbestendigheidPercent = stress;
        row.InnovatiePercent = innovatie;
        row.ExtraversiePercent = extraversie;
        row.CompletedAtUtc = now;
        row.UpdatedAtUtc = now;

        if (!string.IsNullOrWhiteSpace(dreamTitle))
        {
            var plan = await db.CandidateCareerPlans.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (plan is null)
            {
                plan = new CandidateCareerPlan
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CreatedAtUtc = now
                };
                db.CandidateCareerPlans.Add(plan);
            }

            plan.DreamTitle = dreamTitle.Trim();
            plan.UpdatedAtUtc = now;
        }

        var reflections = await db.CandidateTestReflections.Where(r => r.UserId == user.Id).ToListAsync();
        if (reflections.Count > 0)
        {
            db.CandidateTestReflections.RemoveRange(reflections);
        }

        await db.SaveChangesAsync();
    }
}
