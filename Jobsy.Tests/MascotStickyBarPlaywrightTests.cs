using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Mobile viewport proof: Lobsy coach tab vs sticky bottom bars.</summary>
[Collection("PlaywrightSmoke")]
public sealed class MascotStickyBarPlaywrightTests
{
    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_MASCOT_STICKY"), "1", StringComparison.Ordinal);

    [Fact]
    public async Task Vacancy_detail_mobile_coach_and_sticky_apply_visible()
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

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(KbE2e.MobileContext());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var candidate = await CandidateJourneyE2e.SeedFreshApplyCandidateAsync("mascot-sticky", 0);
        Assert.NotNull(candidate);
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, candidate.Value.Email, candidate.Value.Password));
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);

        var vacancyId = await CandidateJourneyE2e.SeedFreshVacancyFixtureAsync(
            baseUrl,
            candidate.Value.Email,
            "MascotStickyBar-proof");
        Assert.NotNull(vacancyId);

        await page.GotoAsync($"{baseUrl}/vacancies/{vacancyId:D}", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForSelectorAsync("[data-testid=kb-detail], .kb-detail", new() { Timeout = 30_000 });

        var sticky = page.Locator("[data-testid=kb-sticky-apply]");
        await sticky.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var coach = page.Locator("#lobsy-coach-btn, .lobsy-coach-dock__btn");
        await coach.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var tag = Environment.GetEnvironmentVariable("JOBSY_E2E_MASCOT_STICKY_TAG") ?? "after";
        var dir = Path.Combine("/workspace/usability6/mobile", tag);
        Directory.CreateDirectory(dir);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(dir, "vacancy-sticky-apply-coach.png"),
            FullPage = false
        });

        var stickyBox = await sticky.First.BoundingBoxAsync();
        var coachBox = await coach.First.BoundingBoxAsync();
        Assert.NotNull(stickyBox);
        Assert.NotNull(coachBox);

        var overlap = stickyBox!.Y < coachBox!.Y + coachBox.Height
                      && stickyBox.Y + stickyBox.Height > coachBox.Y
                      && stickyBox.X < coachBox.X + coachBox.Width
                      && stickyBox.X + stickyBox.Width > coachBox.X;
        if (string.Equals(tag, "after", StringComparison.OrdinalIgnoreCase))
        {
            Assert.False(overlap, "Coach button must not overlap the sticky apply bar.");
        }
    }
}
