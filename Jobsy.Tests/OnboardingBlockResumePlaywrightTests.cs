using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Tests.E2e;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Evidence for onboarding block 2 (Leuk) starting at vraag 1 after block 1 (Hoe je werkt).
/// Run with JOBSY_CAPTURE_BLOCK_RESUME=1 against a seeded local stack.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingBlockResumePlaywrightTests
{
    private const string Email = "kandidaat@jobsy.local";
    private const string Password = "Jobsy123!";
    private const string OutputDir = "/cursor/stores/self/block-resume";
    private const int QuickScanVersion = AssessmentTestVersions.V3;

    [Fact]
    public async Task Capture_leuk_block_starts_at_vraag_1_after_competency_when_requested()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_BLOCK_RESUME") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        Environment.SetEnvironmentVariable(
            "ConnectionStrings__JobsyDb",
            Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb")
            ?? "Host=localhost;Port=5432;Database=JobsyDb;Username=postgres;Password=postgres");

        Directory.CreateDirectory(OutputDir);
        await PrepareCareerBlockAfterCompetencyAsync();

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, suffix) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            await using var ctx = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = width < 500,
                HasTouch = width < 500
            });
            await PlaywrightCookieConsent.AcceptAsync(ctx);
            var page = await ctx.NewPageAsync();
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, Email, Password));
            await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
            await page.Locator(".test-flow__counter, .ob-wizard__test-tab").First
                .WaitForAsync(new() { Timeout = 90_000 });
            await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);

            var counter = page.Locator(".test-flow__counter").First;
            await counter.WaitForAsync(new() { Timeout = 60_000 });
            var counterText = await counter.InnerTextAsync();
            Assert.Contains("Vraag 1 van 5", counterText, StringComparison.Ordinal);

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(OutputDir, $"leuk-block-start-{suffix}.png"),
                FullPage = false
            });

            await page.Locator(".test-likert__opt").Nth(2).ClickAsync();
            await page.WaitForTimeoutAsync(400);
            await page.Locator(".test-likert__opt").Nth(2).ClickAsync();
            await page.WaitForTimeoutAsync(400);

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(OutputDir, $"leuk-block-mid-progress-{suffix}.png"),
                FullPage = false
            });
        }
    }

    private static async Task PrepareCareerBlockAfterCompetencyAsync()
    {
        await CandidateJourneyE2e.EnsureTestAiConsentForE2eAsync(Email);
        await CandidateJourneyE2e.ResetDemoCareerInterestsForE2eAsync(Email);
        await SeedMiniCompleteInDbAsync(Email, AssessmentKind.Competence);
        await CandidateJourneyE2e.SetDiscoveryJourneyStepsForE2eAsync(
            Email,
            completedThrough: 7,
            currentStep: 8);
    }

    private static async Task SeedMiniCompleteInDbAsync(string candidateEmail, AssessmentKind kind)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            return;
        }

        var ids = TestDepthRules.MiniIds(kind, QuickScanVersion);
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
            row.QuickScanVersion = QuickScanVersion;
            row.QuickScanHighestAnswered = ids.Length;
            row.UpdatedAtUtc = now;
        }
    }
}
