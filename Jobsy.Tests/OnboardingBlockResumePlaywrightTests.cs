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
    public async Task Capture_terug_shows_selected_answer_when_requested()
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

        Directory.CreateDirectory(OutputDir);
        await PrepareFreshMiniBlockAsync(step: 8);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await using var ctx = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, Email, Password));
        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        await page.Locator(".test-flow__counter").First.WaitForAsync(new() { Timeout = 90_000 });
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);

        await page.Locator(".test-likert__opt").Nth(3).ClickAsync();
        await page.WaitForTimeoutAsync(500);
        await page.Locator("button.test-flow__back").ClickAsync();
        await page.WaitForTimeoutAsync(300);

        var selected = page.Locator(".test-likert__opt.is-on input[type=radio]");
        await selected.WaitForAsync(new() { Timeout = 15_000 });
        Assert.Equal("4", await selected.GetAttributeAsync("value"));
        await Assertions.Expect(page.Locator("button.test-flow__next")).ToBeEnabledAsync();

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutputDir, "terug-selected-answer-1440.png"),
            FullPage = false
        });
    }

    [Fact]
    public async Task Capture_sfeer_and_waarden_blocks_at_vraag_1_when_requested()
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

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await CaptureBlockStartAsync(browser, baseUrl, step: 9, slug: "sfeer");
        await CaptureBlockStartAsync(browser, baseUrl, step: 10, slug: "waarden");
    }

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

    private static async Task CaptureBlockStartAsync(IBrowser browser, string baseUrl, int step, string slug)
    {
        await PrepareFreshMiniBlockAsync(step);
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
            Assert.Contains("Vraag 1 van 5", await counter.InnerTextAsync(), StringComparison.Ordinal);

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(OutputDir, $"{slug}-block-start-{suffix}.png"),
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

    private static async Task PrepareFreshMiniBlockAsync(int step)
    {
        await CandidateJourneyE2e.EnsureTestAiConsentForE2eAsync(Email);
        await SeedMiniCompleteThroughAsync(step - 1);
        await ResetAssessmentForStepAsync(step);
        await CandidateJourneyE2e.SetDiscoveryJourneyStepsForE2eAsync(
            Email,
            completedThrough: step - 1,
            currentStep: step);
    }

    private static async Task SeedMiniCompleteThroughAsync(int throughStep)
    {
        if (throughStep >= 7)
        {
            await SeedMiniCompleteInDbAsync(Email, AssessmentKind.Competence);
        }

        if (throughStep >= 8)
        {
            await SeedMiniCompleteInDbAsync(Email, AssessmentKind.Career);
        }

        if (throughStep >= 9)
        {
            await SeedMiniCompleteInDbAsync(Email, AssessmentKind.Culture);
        }
    }

    private static async Task ResetAssessmentForStepAsync(int step)
    {
        switch (step)
        {
            case 8:
                await CandidateJourneyE2e.ResetDemoCareerInterestsForE2eAsync(Email);
                break;
            case 9:
                await ResetCultureForE2eAsync(Email);
                break;
            case 10:
                await ResetValuesForE2eAsync(Email);
                break;
        }
    }

    private static async Task ResetCultureForE2eAsync(string candidateEmail)
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

        var rows = await db.CandidateCulturePersonalityProfiles.Where(c => c.UserId == user.Id).ToListAsync();
        if (rows.Count > 0)
        {
            db.CandidateCulturePersonalityProfiles.RemoveRange(rows);
        }

        await db.SaveChangesAsync();
    }

    private static async Task ResetValuesForE2eAsync(string candidateEmail)
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

        var rows = await db.CandidateValuesProfiles.Where(c => c.UserId == user.Id).ToListAsync();
        if (rows.Count > 0)
        {
            db.CandidateValuesProfiles.RemoveRange(rows);
        }

        await db.SaveChangesAsync();
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
        switch (kind)
        {
            case AssessmentKind.Competence:
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
                break;
            }
            case AssessmentKind.Career:
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
                row.QuickScanVersion = QuickScanVersion;
                row.QuickScanHighestAnswered = ids.Length;
                row.UpdatedAtUtc = now;
                break;
            }
            case AssessmentKind.Culture:
            {
                var row = await db.CandidateCulturePersonalityProfiles.FirstOrDefaultAsync(c => c.UserId == user.Id);
                if (row is null)
                {
                    row = new CandidateCulturePersonalityProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        CreatedAtUtc = now
                    };
                    db.CandidateCulturePersonalityProfiles.Add(row);
                }

                row.AnswersJson = json;
                row.QuickScanVersion = QuickScanVersion;
                row.QuickScanHighestAnswered = ids.Length;
                row.UpdatedAtUtc = now;
                break;
            }
            case AssessmentKind.Values:
            {
                var row = await db.CandidateValuesProfiles.FirstOrDefaultAsync(c => c.UserId == user.Id);
                if (row is null)
                {
                    row = new CandidateValuesProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        CreatedAtUtc = now
                    };
                    db.CandidateValuesProfiles.Add(row);
                }

                row.AnswersJson = json;
                row.QuickScanVersion = QuickScanVersion;
                row.QuickScanHighestAnswered = ids.Length;
                row.UpdatedAtUtc = now;
                break;
            }
        }

        await db.SaveChangesAsync();
    }
}
