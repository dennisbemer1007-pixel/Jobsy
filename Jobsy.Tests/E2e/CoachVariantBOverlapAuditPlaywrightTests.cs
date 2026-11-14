using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Variant-B coach must not overlap clickable controls (390 / 768 / 1440). Groep 7/8 excluded.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CoachVariantBOverlapAuditPlaywrightTests
{
    [Fact(Timeout = 900_000)]
    public async Task Coach_bbox_does_not_overlap_clickables_on_coach_pages()
    {
        _ = TestContext.Current.CancellationToken;
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await CoachVariantBOverlapAuditSupport.EnsureSchoolsEnabledAsync();

        var outDir = Environment.GetEnvironmentVariable("JOBSY_COACH_OVERLAP_ARTIFACTS")
                     ?? "/opt/cursor/artifacts/coach-variant-b-overlap";
        Directory.CreateDirectory(outDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var vacancyId = await CandidateJourneyE2e.SeedFreshVacancyFixtureAsync(
            baseUrl,
            KbE2e.DefaultCompleteEmail,
            "coach-overlap-audit");
        Assert.NotNull(vacancyId);

        var pages = CoachVariantBOverlapAuditSupport.Pages.ToList();
        pages.Add(new CoachVariantBOverlapAuditSupport.AuditPage(
            "vacancy-mock-interview",
            $"/vacancies/{vacancyId:D}",
            KbE2e.DefaultCompleteEmail));

        var allTableRows = new List<(string Page, int Viewport, bool Pass, string Note)>();
        var failures = new List<string>();

        foreach (var (width, height) in new[] { (390, 844), (768, 1024), (1440, 900) })
        {
            var mboSeed = await CoachVariantBOverlapAuditSupport.SeedFreshMboPupilAsync(browser, baseUrl);
            Assert.NotNull(mboSeed);

            await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            var jsonRows = new List<object>();

            string? activeLogin = "__guest__";
            foreach (var entry in pages)
            {
                var sessionKey = entry.Path.StartsWith("/leerling/", StringComparison.Ordinal)
                    ? "__pupil__"
                    : entry.LoginEmail ?? "__guest__";
                if (!string.Equals(activeLogin, sessionKey, StringComparison.Ordinal))
                {
                    await context.ClearCookiesAsync();
                    if (entry.Path.StartsWith("/leerling/", StringComparison.Ordinal))
                    {
                        var ok = await CoachVariantBOverlapAuditSupport.PupilLoginAsync(
                            page, baseUrl, mboSeed.Value.SchoolId, mboSeed.Value.ClassId, mboSeed.Value.Code);
                        Assert.True(ok, $"MBO pupil login failed (fresh seed code, not 2V3-3RG). class={mboSeed.Value.ClassId}");
                    }
                    else if (!string.IsNullOrEmpty(entry.LoginEmail))
                    {
                        var ok = await CoachVariantBOverlapAuditSupport.LoginAsync(page, baseUrl, entry.LoginEmail);
                        Assert.True(ok, $"Login failed for {entry.LoginEmail}");
                    }

                    activeLogin = sessionKey;
                }

                var waitUntil = entry.Name is "vacancy-mock-interview" or "candidate-home"
                    ? WaitUntilState.NetworkIdle
                    : WaitUntilState.DOMContentLoaded;
                await page.GotoAsync($"{baseUrl}{entry.Path}", new() { WaitUntil = waitUntil });
                await CoachVariantBOverlapAuditSupport.WaitForCoachChromeAsync(page, entry.Name);
                await page.WaitForTimeoutAsync(400);

                var audit = await CoachVariantBOverlapAuditSupport.EvaluateOverlapAsync(page);

                var slug = $"{entry.Name}-{width}";
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(outDir, $"coach-overlap-{slug}.png"),
                    FullPage = false,
                });

                var pass = !audit.Skip && audit.Ok;
                var note = audit.Skip ? $"unexpected-skip:{audit.Reason}" : audit.Reason;
                if (!pass)
                {
                    failures.Add($"{entry.Name}@{width}: {note}");
                }

                allTableRows.Add((entry.Name, width, pass, note));
                jsonRows.Add(new { page = entry.Name, viewport = width, pass, note, hits = audit.Hits });
            }

            await File.WriteAllTextAsync(
                Path.Combine(outDir, $"OVERLAP-AUDIT-{width}.json"),
                JsonSerializer.Serialize(jsonRows, new JsonSerializerOptions { WriteIndented = true }));
        }

        await CoachVariantBOverlapAuditSupport.WriteSummaryMarkdownAsync(outDir, allTableRows);

        if (failures.Count > 0)
        {
            Assert.Fail(string.Join(Environment.NewLine, failures));
        }
    }
}