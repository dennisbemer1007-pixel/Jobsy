using Jobsy.Tests.E2e;
using Microsoft.Playwright;
using Xunit;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Kreeft B2 + wolk must not overlap MBO pupil controls (390 / 768 / 1440). Fresh leerlingcode via DB seed.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class MboHboCoachOverlapPlaywrightTests
{
    private static readonly CoachVariantBOverlapAuditSupport.AuditPage[] MboPages =
    [
        new("mbo-uitleg", "/leerling/mbo-hbo/uitleg", null),
        new("mbo-overzicht", "/leerling/mbo-hbo/overzicht", null),
        new("mbo-test-interesse", "/leerling/mbo-hbo/test/interesse", null),
        new("mbo-test-werkwaarden", "/leerling/mbo-hbo/test/werkwaarden", null),
        new("mbo-uitkomst-vakken", "/leerling/mbo-hbo/uitkomst/vakken", null),
        new("mbo-paspoort", "/leerling/mbo-hbo/paspoort", null),
    ];

    [Theory(Timeout = 720_000)]
    [InlineData(390, 844)]
    [InlineData(768, 1024)]
    [InlineData(1440, 900)]
    public async Task Mbo_coach_bbox_does_not_overlap_clickables(int width, int height)
    {
        _ = TestContext.Current.CancellationToken;
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await CoachVariantBOverlapAuditSupport.EnsureSchoolsEnabledAsync();

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var mboSeed = await CoachVariantBOverlapAuditSupport.SeedFreshMboPupilAsync(browser, baseUrl);
        Assert.NotNull(mboSeed);
        Assert.DoesNotContain("2V3-3RG", mboSeed.Value.Code, StringComparison.OrdinalIgnoreCase);

        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var failures = new List<string>();
        foreach (var entry in MboPages)
        {
            await context.ClearCookiesAsync();
            var ok = await CoachVariantBOverlapAuditSupport.PupilLoginAsync(
                page, baseUrl, mboSeed.Value.SchoolId, mboSeed.Value.ClassId, mboSeed.Value.Code);
            Assert.True(ok, $"MBO pupil login failed for {entry.Name}");

            await page.GotoAsync($"{baseUrl}{entry.Path}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CoachVariantBOverlapAuditSupport.WaitForCoachChromeAsync(page, entry.Name);
            await page.WaitForTimeoutAsync(350);

            var audit = await CoachVariantBOverlapAuditSupport.EvaluateOverlapAsync(page);
            if (audit.Skip || !audit.Ok)
            {
                failures.Add($"{entry.Name}@{width}: {audit.Reason}; hits={string.Join(", ", audit.Hits)}");
            }
        }

        Assert.Empty(failures);
    }
}