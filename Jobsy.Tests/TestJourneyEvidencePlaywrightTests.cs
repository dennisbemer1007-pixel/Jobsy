using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class TestJourneyEvidencePlaywrightTests
{
    private const string Email = "kandidaat@jobsy.local";
    private const string Password = "Jobsy123!";
    private const string OutputDir = "/cursor/stores/self/wie-ben-jij";

    [Fact]
    public async Task Capture_wie_ben_jij_journey_evidence_when_requested()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_JOURNEY") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        Directory.CreateDirectory(OutputDir);
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__JobsyDb",
            Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb")
            ?? "Host=localhost;Port=5432;Database=JobsyDb;Username=postgres;Password=postgres");

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await PrepareConsentScreenAsync(completedMiniTests: 0);
        foreach (var (w, h, name) in Viewports())
        {
            await CaptureAsync(browser, baseUrl, w, h, $"journey-flag-on-0done-{name}.png");
        }

        await PrepareConsentScreenAsync(completedMiniTests: 2);
        foreach (var (w, h, name) in Viewports())
        {
            await CaptureAsync(browser, baseUrl, w, h, $"journey-flag-on-2done-{name}.png");
        }
    }

    [Fact]
    public async Task Capture_classic_consent_when_flag_off_requested()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_JOURNEY_FLAG_OFF") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        Directory.CreateDirectory(OutputDir);
        await PrepareConsentScreenAsync(completedMiniTests: 0);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await CaptureClassicAsync(browser, baseUrl, 1440, 900, "journey-flag-off-0done-1440.png");
    }

    private static IEnumerable<(int Width, int Height, string Name)> Viewports()
    {
        yield return (1440, 900, "1440");
        yield return (390, 844, "390");
        yield return (360, 800, "360");
    }

    private static async Task CaptureClassicAsync(IBrowser browser, string baseUrl, int width, int height, string fileName)
    {
        await using var ctx = await browser.NewContextAsync(new() { ViewportSize = new() { Width = width, Height = height } });
        var page = await ctx.NewPageAsync();
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, Email, Password));
        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        await page.Locator(".ob-wizard__consent, h1").First.WaitForAsync(new() { Timeout = 90_000 });
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await page.WaitForTimeoutAsync(600);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutputDir, fileName), FullPage = false });
    }

    private static async Task CaptureAsync(IBrowser browser, string baseUrl, int width, int height, string fileName)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 500,
            HasTouch = width < 500
        });
        var page = await ctx.NewPageAsync();
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, Email, Password));
        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        await page.Locator("[data-testid=test-journey-consent], .ob-wizard__consent, h1")
            .First
            .WaitForAsync(new() { Timeout = 90_000 });
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await page.WaitForTimeoutAsync(600);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutputDir, fileName), FullPage = false });
    }

    private static async Task PrepareConsentScreenAsync(int completedMiniTests)
    {
        await ClearTestAiConsentForE2eAsync(Email);
        await CandidateJourneyE2e.SetDiscoveryJourneyStepsForE2eAsync(Email, completedThrough: 6, currentStep: 7);
        await CandidateJourneyE2e.ResetDemoCompetencyForE2eAsync(Email);
        await CandidateJourneyE2e.ResetDemoCareerInterestsForE2eAsync(Email);

        if (completedMiniTests >= 1)
        {
            await SeedMiniCompleteInDbAsync(Email, AssessmentKind.Competence);
        }

        if (completedMiniTests >= 2)
        {
            await SeedMiniCompleteInDbAsync(Email, AssessmentKind.Career);
        }
    }

    private static async Task SeedMiniCompleteInDbAsync(string candidateEmail, AssessmentKind kind)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            return;
        }

        var version = AssessmentTestVersions.V1;
        var ids = TestDepthRules.MiniIds(kind, version);
        var answers = ids.ToDictionary(id => id.ToString(), _ => 3);
        var json = JsonSerializer.Serialize(answers);

        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var normalized = candidateEmail.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized);
        if (user is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (kind == AssessmentKind.Competence)
        {
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

            row.AnswersJson = json;
            row.QuickScanVersion = version;
            row.QuickScanHighestAnswered = ids.Length;
            row.UpdatedAtUtc = now;
        }
        else if (kind == AssessmentKind.Career)
        {
            var row = await db.CandidateCareerInterests.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (row is null)
            {
                row = new CandidateCareerInterest
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    CreatedAtUtc = now
                };
                db.CandidateCareerInterests.Add(row);
            }

            row.AnswersJson = json;
            row.QuickScanVersion = version;
            row.QuickScanHighestAnswered = ids.Length;
            row.UpdatedAtUtc = now;
        }

        await db.SaveChangesAsync();
    }

    private static async Task ClearTestAiConsentForE2eAsync(string candidateEmail)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var normalized = candidateEmail.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized);
        if (user is null)
        {
            return;
        }

        user.TestAiConsentAt = null;
        user.TestAiConsentVersion = null;
        await db.SaveChangesAsync();
    }
}
