using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

internal static class OnboardingRustigKompasE2eFlow
{
    internal static Task ReachRustigKompasAsync(IPage page, string baseUrl, bool mobile) =>
        ReachKompasAsync(page, baseUrl, mobile, rustig: true);

    internal static Task ReachBaselineKompasAsync(IPage page, string baseUrl, bool mobile) =>
        ReachKompasAsync(page, baseUrl, mobile, rustig: false);

    private static async Task ReachKompasAsync(IPage page, string baseUrl, bool mobile, bool rustig)
    {
        _ = mobile;
        var email = await RegisterFreshCandidateEmailAsync(page, baseUrl, rustig ? "rustig-kompas" : "baseline-kompas");
        await CandidateJourneyE2e.EnsureTestAiConsentForE2eAsync(email);
        await CandidateJourneyE2e.SeedMiniBlockCompleteForE2eAsync(email, AssessmentKind.Competence);
        await CandidateJourneyE2e.SeedMiniBlockCompleteForE2eAsync(email, AssessmentKind.Career);
        await CandidateJourneyE2e.SeedMiniBlockCompleteForE2eAsync(email, AssessmentKind.Culture);
        await CandidateJourneyE2e.SeedMiniBlockCompleteForE2eAsync(email, AssessmentKind.Values);
        await CandidateJourneyE2e.SetDiscoveryJourneyStepsForE2eAsync(email, completedThrough: 10, currentStep: 10);
        await CandidateJourneyE2e.EnsureKompasLiveMatchesForE2eAsync(email, baseUrl);
        await SetFinishReachedForE2eAsync(email);

        if (!rustig)
        {
            await CandidateJourneyE2e.EnsureOnboardingFinaleV2EnabledForE2eAsync();
        }

        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await FunctionalOnboardingInteractivityHelpers.WaitForCircuitReadyAsync(page, 120_000);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);

        if (rustig)
        {
            await page.Locator("[data-ob-rustig-screen=kompas]").WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 90_000,
            });
        }
        else
        {
            await page.Locator(".ob-finale__wrap, .ob-wizard__finish .ob-finale").First.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 90_000,
            });
        }
    }

    private static async Task SetFinishReachedForE2eAsync(string candidateEmail)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn) || string.IsNullOrWhiteSpace(candidateEmail))
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

        var onboarding = await db.CandidateOnboardings.FirstOrDefaultAsync(o => o.UserId == user.Id);
        if (onboarding is null)
        {
            return;
        }

        onboarding.FinishReached = true;
        onboarding.CompletedAtUtc = null;
        onboarding.CurrentStep = OnboardingWizardCatalog.V3StepCount;
        onboarding.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static async Task<string> RegisterFreshCandidateEmailAsync(IPage page, string baseUrl, string emailPrefix)
    {
        var email = $"{emailPrefix}.{Guid.NewGuid():N}@jobsy.local";
        await CandidateJourneyE2e.ClearCapturedEmailCodesAsync(baseUrl);
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Func");
        await page.Locator("button.login-submit, button.au-submit[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Expected OTP from E2E mail capture.");
        }

        await page.Locator("input[name=code]").FillAsync(code);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });
        return email;
    }
}
