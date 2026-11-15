using Jobsy.Tests.E2e;
using Microsoft.Playwright;
using Xunit;

namespace Jobsy.Tests.Scholen;

/// <summary>Welkom, vraag, overgang, paspoort — 390 + 1440 naar JOBSY_MBO_EVIDENCE_DIR (default /opt/cursor/artifacts/mbo-b2).</summary>
[Collection("PlaywrightSmoke")]
public sealed class MboHboMockupScreenshotPlaywrightTests
{
    [Fact(Timeout = 420_000)]
    public async Task Capture_mbo_mockup_screenshots_390_and_1440()
    {
        _ = TestContext.Current.CancellationToken;
        var baseUrl = KbE2e.TryBaseUrl();
        Assert.False(string.IsNullOrWhiteSpace(baseUrl), "Zet JOBSY_E2E_BASE_URL.");
        Assert.True(await KbE2e.IsReachableAsync(baseUrl!));

        var outDir = Environment.GetEnvironmentVariable("JOBSY_MBO_EVIDENCE_DIR")
                     ?? "/opt/cursor/artifacts/mbo-b2";
        Directory.CreateDirectory(outDir);

        await CoachVariantBOverlapAuditSupport.EnsureSchoolsEnabledAsync();

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var seed = await CoachVariantBOverlapAuditSupport.SeedFreshMboPupilAsync(browser, baseUrl!);
        Assert.NotNull(seed);
        Assert.DoesNotContain("2V3-3RG", seed.Value.Code, StringComparison.OrdinalIgnoreCase);

        var routes = new (string Slug, string Path)[]
        {
            ("welkom", "/leerling/mbo-hbo/uitleg"),
            ("vraag", "/leerling/mbo-hbo/test/vakken"),
            ("overgang", "/leerling/mbo-hbo/uitkomst/vakken"),
            ("paspoort", "/leerling/mbo-hbo/paspoort"),
        };
        var onlySlug = Environment.GetEnvironmentVariable("JOBSY_MBO_SCREENSHOT_SLUG");
        if (!string.IsNullOrWhiteSpace(onlySlug))
        {
            routes = routes.Where(r => string.Equals(r.Slug, onlySlug, StringComparison.OrdinalIgnoreCase)).ToArray();
            Assert.NotEmpty(routes);
        }

        foreach (var (w, h, tag) in new[] { (390, 844, "390"), (1440, 900, "1440") })
        {
            await using var context = await browser.NewContextAsync(KbE2e.ContextFor(w, h));
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);
            Assert.True(await CoachVariantBOverlapAuditSupport.PupilLoginAsync(
                page, baseUrl!, seed.Value.SchoolId, seed.Value.ClassId, seed.Value.Code));

            foreach (var (slug, path) in routes)
            {
                await page.GotoAsync($"{baseUrl}{path}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                if (slug == "vraag")
                {
                    await page.Locator("[data-testid=mhbo-test-question]").WaitForAsync(new() { Timeout = 60_000 });
                }
                else if (slug == "welkom")
                {
                    await page.Locator("[data-testid=mhbo-welcome]").WaitForAsync(new() { Timeout = 60_000 });
                }
                else if (slug == "overgang")
                {
                    await page.Locator("[data-testid=mhbo-uitkomst]").WaitForAsync(new() { Timeout = 60_000 });
                }
                else
                {
                    await page.Locator("[data-testid=mhbo-paspoort]").WaitForAsync(new() { Timeout = 60_000 });
                }

                await page.WaitForTimeoutAsync(400);
                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(outDir, $"{slug}-{tag}.png"),
                    FullPage = false,
                });

                if (tag == "390")
                {
                    await page.EvaluateAsync(
                        """
                        () => {
                          const h = Math.max(
                            document.documentElement.scrollHeight,
                            document.body.scrollHeight);
                          window.scrollTo(0, Math.max(0, h - window.innerHeight));
                        }
                        """);
                    await page.WaitForTimeoutAsync(250);
                    await page.ScreenshotAsync(new()
                    {
                        Path = Path.Combine(outDir, $"{slug}-{tag}-foot.png"),
                        FullPage = false,
                    });
                    await page.EvaluateAsync("() => window.scrollTo(0, 0)");
                }
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(outDir, "seed-code.txt"),
            $"{seed.Value.Code} (nooit 2V3-3RG; class {seed.Value.ClassId:D})");
    }
}
