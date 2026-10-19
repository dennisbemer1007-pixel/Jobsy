using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Tests.E2e;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Xunit;

namespace Jobsy.Tests;

/// <summary>
/// Mockup captures with TestDepthValueMockupEnabled. Run:
/// JOBSY_CAPTURE_TEST_DEPTH_MOCKUP=1 dotnet test --filter Capture_test_depth_value_mockup
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class TestDepthValueMockupCapturePlaywrightTests
{
    private const string BaseUrl = "http://127.0.0.1:5201";
    private const string Password = DemoUsersSeeder.DemoPassword;
    private const string OutputDir = "/cursor/stores/self/artifacts/test-levels-mockup-r3";

    private static readonly (int N, string Email, string Name)[] Levels =
    [
        (5, "voorbeeld5@jobsy.local", "Voorbeeld 5"),
        (10, "voorbeeld10@jobsy.local", "Voorbeeld 10"),
        (25, "voorbeeld25@jobsy.local", "Voorbeeld 25"),
        (150, "voorbeeld150@jobsy.local", "Voorbeeld 150"),
    ];

    [Fact]
    public async Task Capture_test_depth_value_mockup()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_TEST_DEPTH_MOCKUP") ?? "").Trim() != "1")
        {
            return;
        }

        if (!await KbE2e.IsReachableAsync(BaseUrl))
        {
            throw new InvalidOperationException($"Stack not reachable at {BaseUrl}");
        }

        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb")
            ?? "Host=127.0.0.1;Port=5432;Database=JobsyCi;Username=postgres;Password=postgres";
        Environment.SetEnvironmentVariable("ConnectionStrings__JobsyDb", conn);

        Directory.CreateDirectory(OutputDir);
        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(BaseUrl);
        await EnableMockupFlagAsync(conn);

        await EnsureExampleUsersAsync(conn);
        await SeedDeepCompetenceFor150Async(conn);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var htmlChecks = new List<(int N, string File, string Text)>();

        foreach (var level in Levels)
        {
            if (level.N != 150)
            {
                await ResetCompetenceAsync(conn, level.Email);
                await SeedCompetenceAnswersAsync(conn, level.Email, level.N, level.N >= 25);
            }

            await CaptureLevelAsync(browser, level.N, level.Email, waitMirror: level.N >= 25, htmlChecks);
        }

        AssertNoScoreOrPercentInCandidateText(htmlChecks);
    }

    private static void AssertNoScoreOrPercentInCandidateText(IReadOnlyList<(int N, string File, string Text)> checks)
    {
        var scorePattern = new Regex(@"\bscore\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        foreach (var (n, file, text) in checks)
        {
            Assert.False(text.Contains('%', StringComparison.Ordinal),
                $"Level n{n} HTML ({file}) must not contain '%' in candidate text.");
            Assert.False(scorePattern.IsMatch(text),
                $"Level n{n} HTML ({file}) must not contain the word 'score' in candidate text.");
        }
    }

    private static async Task EnableMockupFlagAsync(string conn)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            row = new PlatformFeatureSettings { Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee") };
            db.PlatformFeatureSettings.Add(row);
        }

        row.TestDepthValueMockupEnabled = true;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static async Task CaptureLevelAsync(
        IBrowser browser,
        int n,
        string email,
        bool waitMirror,
        List<(int N, string File, string Text)> htmlChecks)
    {
        await CandidateJourneyE2e.EnsureTestAiConsentForE2eAsync(email);

        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            var isMobile = width < 900;
            await CaptureUrlAsync(browser, email, width, height, isMobile,
                $"{BaseUrl}/profiel/tests/competence", $"n{n}-result-{tag}.png",
                fullPage: !isMobile, waitMirror, htmlChecks, n);

            await CaptureUrlAsync(browser, email, width, height, isMobile,
                $"{BaseUrl}/candidate/paspoort", $"n{n}-paspoort-dna-{tag}.png",
                fullPage: !isMobile, waitMirror: false, htmlChecks, n);

            await CaptureUrlAsync(browser, email, width, height, isMobile,
                $"{BaseUrl}/candidate/profile?tab=tests", $"n{n}-mijn-tests-{tag}.png",
                fullPage: !isMobile, waitMirror: false, htmlChecks, n);
        }
    }

    private static async Task CaptureUrlAsync(
        IBrowser browser,
        string email,
        int width,
        int height,
        bool isMobile,
        string url,
        string fileName,
        bool fullPage,
        bool waitMirror,
        List<(int N, string File, string Text)> htmlChecks,
        int levelN)
    {
        await using var ctx = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = isMobile,
            HasTouch = isMobile,
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        Assert.True(await KbE2e.TryLoginAsync(page, BaseUrl, email, Password));
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await KbE2e.WaitForStyledAppAsync(page);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);

        if (waitMirror)
        {
            try
            {
                await page.WaitForSelectorAsync(".test-mirror__title", new() { Timeout = 180_000 });
            }
            catch (TimeoutException)
            {
            }

            try
            {
                await page.WaitForFunctionAsync(
                    "() => !document.body.innerText.toLowerCase().includes('met een score van')",
                    null,
                    new() { Timeout = 120_000 });
            }
            catch (TimeoutException)
            {
            }
        }
        else if (url.Contains("/profiel/tests/competence", StringComparison.Ordinal))
        {
            await page.WaitForSelectorAsync(
                ".test-depth-mini-mirror, .test-mirror__title, .test-detail__header, .competence-report",
                new() { Timeout = 60_000 });
        }
        else if (url.Contains("/paspoort", StringComparison.Ordinal))
        {
            await page.WaitForSelectorAsync(".passport-page, .passport-dna", new() { Timeout = 60_000 });
        }
        else if (url.Contains("tab=tests", StringComparison.Ordinal))
        {
            await page.WaitForSelectorAsync(".tests-overview, .kompas-card, .passport-tests", new() { Timeout = 60_000 });
        }

        await page.EvaluateAsync("() => document.fonts.ready");
        await page.WaitForTimeoutAsync(1200);

        if (isMobile)
        {
            await page.EvaluateAsync("""
                () => {
                  window.scrollTo(0, 0);
                  document.querySelectorAll('.lobsy-coach-dock, [data-lobsy-coach]').forEach(el => el.remove());
                }
                """);
            await page.AddStyleTagAsync(new()
            {
                Content = ".lobsy-coach-dock, [data-lobsy-coach] { display: none !important; }"
            });
        }
        else if (fullPage)
        {
            await PlaywrightScreenshotSanitizer.PreparePageAsync(page);
        }

        var path = Path.Combine(OutputDir, fileName);
        await page.ScreenshotAsync(new() { Path = path, FullPage = fullPage });

        if (url.Contains("/profiel/tests/competence", StringComparison.Ordinal))
        {
            var candidateText = await page.EvaluateAsync<string>("""
                () => {
                  const parts = [];
                  const mini = document.querySelector('.test-depth-mini-mirror');
                  const unlock = document.querySelector('.test-depth-unlock');
                  const mirror = document.querySelector('.test-mirror-page-shell');
                  const report = document.querySelector('.competence-report');
                  for (const el of [mini, unlock, mirror, report]) {
                    if (el) parts.push(el.innerText);
                  }
                  if (parts.length === 0) {
                    const main = document.querySelector('.test-detail-page') || document.querySelector('main');
                    return main ? main.innerText : document.body.innerText;
                  }
                  return parts.join('\n');
                }
                """);
            htmlChecks.Add((levelN, fileName, candidateText));
            var htmlPath = Path.Combine(OutputDir, Path.ChangeExtension(fileName, ".html"));
            await File.WriteAllTextAsync(htmlPath, await page.ContentAsync());
        }
    }

    private static async Task EnsureExampleUsersAsync(string conn)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);

        var ids = new Dictionary<int, Guid>
        {
            [5] = Guid.Parse("e0050000-0000-4000-8000-000000000005"),
            [10] = Guid.Parse("e0100000-0000-4000-8000-000000000010"),
            [25] = Guid.Parse("e0250000-0000-4000-8000-000000000025"),
            [150] = Guid.Parse("e0150000-0000-4000-8000-000000000150"),
        };

        foreach (var level in Levels)
        {
            var id = ids[level.N];
            var email = level.Email.ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                user = new User
                {
                    Id = id,
                    Email = email,
                    FullName = level.Name,
                    FirstName = "Voorbeeld",
                    Role = UserRole.Candidate,
                    IsActive = true,
                    IsTestAccount = true,
                    OpenForWork = true,
                    EmailVerifiedAtUtc = DateTime.UtcNow,
                    PreferencesJson = """{"language":"nl","roles":["zorg"],"maxTravelMinutes":30}""",
                    TestAiConsentAt = DateTime.UtcNow,
                    TestAiConsentVersion = Jobsy.Core.Privacy.PrivacyConstants.CurrentConsentVersion
                };
                db.Users.Add(user);
            }
            else
            {
                user.FullName = level.Name;
                user.IsTestAccount = true;
                user.TestAiConsentAt ??= DateTime.UtcNow;
            }

            var cred = await db.LocalAuthCredentials.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (cred is null)
            {
                db.LocalAuthCredentials.Add(new LocalAuthCredential
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Email = email,
                    PasswordHash = JobsyPasswordHasher.Hash(Password)
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedCompetenceAnswersAsync(string conn, string email, int answerCount, bool markFreeComplete)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var user = await db.Users.FirstAsync(u => u.Email == email.ToLowerInvariant());
        var version = AssessmentTestVersions.V3;
        var ids = TestDepthRules.QuestionIdsUpTo(AssessmentKind.Competence, answerCount, version);
        var answers = ids.ToDictionary(id => id, id => ((id * 7 + answerCount) % 5) + 1);
        var json = JsonSerializer.Serialize(answers.ToDictionary(
            k => k.Key.ToString(CultureInfo.InvariantCulture),
            v => v.Value));
        var now = DateTime.UtcNow;

        var row = new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            QuickScanVersion = version,
            QuickScanHighestAnswered = answerCount,
            AnswersJson = json,
            UpdatedAtUtc = now,
            CreatedAtUtc = now
        };

        if (markFreeComplete && answerCount >= TestDepthRules.FullCount(AssessmentKind.Competence, version))
        {
            var scored = QuickScanFreeTierV3.ScoreCompetence(answers)
                ?? throw new InvalidOperationException("Could not score competence answers.");
            row.Status = CandidateCompetencyStatuses.Completed;
            row.SamenwerkenPercent = scored.Samenwerken;
            row.ResultaatgerichtheidPercent = scored.Resultaatgerichtheid;
            row.StressbestendigheidPercent = scored.Stressbestendigheid;
            row.InnovatiePercent = scored.Innovatie;
            row.ExtraversiePercent = scored.Extraversie;
            row.CompletedAtUtc = now;
            row.MatchTagsJson = CompetencyTestCatalog.SerializeTags(
                QuickScanAssessmentBridge.DeriveCompetencyTags(scored, version));
        }
        else
        {
            row.Status = CandidateCompetencyStatuses.Draft;
        }

        db.CandidateCompetencies.Add(row);
        await db.SaveChangesAsync();
    }

    private static async Task ResetCompetenceAsync(string conn, string email)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var user = await db.Users.FirstAsync(u => u.Email == email.ToLowerInvariant());
        var rows = await db.CandidateCompetencies.Where(c => c.UserId == user.Id).ToListAsync();
        if (rows.Count > 0)
        {
            db.CandidateCompetencies.RemoveRange(rows);
        }

        var reflections = await db.CandidateTestReflections.Where(r => r.UserId == user.Id).ToListAsync();
        if (reflections.Count > 0)
        {
            db.CandidateTestReflections.RemoveRange(reflections);
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedDeepCompetenceFor150Async(string conn)
    {
        var email = "voorbeeld150@jobsy.local";
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var user = await db.Users.FirstAsync(u => u.Email == email);

        var compRows = await db.CandidateCompetencies.Where(c => c.UserId == user.Id).ToListAsync();
        if (compRows.Count > 0)
        {
            db.CandidateCompetencies.RemoveRange(compRows);
        }

        var version = AssessmentTestVersions.V3;
        var ids = TestDepthRules.QuestionIdsUpTo(AssessmentKind.Competence, 25, version);
        var answers = ids.ToDictionary(id => id, id => ((id + 2) % 5) + 1);
        var scored = QuickScanFreeTierV3.ScoreCompetence(answers)
            ?? throw new InvalidOperationException("Could not score competence answers.");
        var json = JsonSerializer.Serialize(answers.ToDictionary(k => k.Key.ToString(CultureInfo.InvariantCulture), v => v.Value));
        var now = DateTime.UtcNow;
        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = json,
            QuickScanVersion = version,
            QuickScanHighestAnswered = ids.Count,
            SamenwerkenPercent = scored.Samenwerken,
            ResultaatgerichtheidPercent = scored.Resultaatgerichtheid,
            StressbestendigheidPercent = scored.Stressbestendigheid,
            InnovatiePercent = scored.Innovatie,
            ExtraversiePercent = scored.Extraversie,
            CompletedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedAtUtc = now
        });

        var deepRows = await db.CandidateDeepAnalyses.Where(d => d.UserId == user.Id).ToListAsync();
        if (deepRows.Count > 0)
        {
            db.CandidateDeepAnalyses.RemoveRange(deepRows);
        }

        var deepAnswers = Enumerable.Range(1, 150).ToDictionary(i => i, i =>
        {
            var band = (i - 1) / 30;
            var bias = band switch { 0 => 4, 1 => 2, 2 => 5, 3 => 1, _ => 3 };
            return ((i * 11 + bias * 17) % 5) + 1;
        });
        var completedAt = DateTime.UtcNow.AddHours(-2);
        var report = CompetenceDeepReportBuilder.Build(
            deepAnswers,
            new FakeNormProvider(),
            jobTitle: "Helpende zorg",
            occupations: null,
            aiSummary: null,
            aiPlan: null,
            fromOpenAi: false,
            generatedAtUtc: completedAt);

        db.CandidateDeepAnalyses.Add(new CandidateDeepAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Kind = AssessmentKind.Competence,
            Status = CandidateDeepAnalysisStatuses.Completed,
            CompletedAtUtc = completedAt,
            ReportGeneratedAtUtc = completedAt,
            UnlockedAtUtc = completedAt.AddMinutes(-30),
            AnswersJson = DeepAnalysisCatalog.SerializeAnswers(deepAnswers, AssessmentKind.Competence),
            ReportJson = CompetenceDeepReportJson.Serialize(report),
            ReportVersion = CompetenceDeepReportJson.CurrentReportVersion,
            UpdatedAtUtc = completedAt
        });

        await db.SaveChangesAsync();
    }

    private sealed class FakeNormProvider : INormProvider
    {
        public bool IsAvailable => true;
        public BigFiveNormSet? Norms => null;
        public string? BandLabel(double score, string traitOrFacetKey) => "Vergelijkbaar met de meeste mensen";
        public double? Mean(string traitOrFacetKey) => 50.0;
        public string SourceDisclaimer => "Voorbeelddata.";
    }
}
