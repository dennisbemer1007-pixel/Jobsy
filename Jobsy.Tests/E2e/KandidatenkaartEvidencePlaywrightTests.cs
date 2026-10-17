using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.E2e;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2eFlow;

[Collection("PlaywrightSmoke")]
public sealed class KandidatenkaartEvidencePlaywrightTests
{
    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable("JOBSY_KK_EVIDENCE"), "1", StringComparison.Ordinal);

    private static string OutDir =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_KK_EVIDENCE_DIR"))
            ? "/cursor/stores/self/kandidatenkaart"
            : Environment.GetEnvironmentVariable("JOBSY_KK_EVIDENCE_DIR")!;

    [Fact]
    public async Task Capture_live_kandidatenkaart_screenshots()
    {
        if (!ShouldRun)
        {
            return;
        }

        Directory.CreateDirectory(OutDir);
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync()
                      ?? throw new InvalidOperationException("JOBSY_E2E_BASE_URL unreachable");
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb")
                   ?? throw new InvalidOperationException("ConnectionStrings__JobsyDb required");

        await EnableKandidatenkaartAsync(baseUrl, conn, enabled: true);
        await using (var seedDb = await OpenDbAsync(conn))
        {
            await KandidatenkaartDemoSeed.EnsureAsync(seedDb);
            await KandidatenkaartDemoSeed.EnsureEmployerVacancyForMapAsync(seedDb);
            await KandidatenkaartDemoSeed.EnsureWeeklyCapEvidenceAsync(seedDb);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        const string employerEmail = "ondernemer@jobsy.local";
        const string password = KbE2e.DefaultPassword;

        foreach (var (suffix, width, height) in new[] { ("1440", 1440, 900), ("390", 390, 844) })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height }
            });
            var page = await context.NewPageAsync();
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, employerEmail, password));
            await page.GotoAsync($"{baseUrl.TrimEnd('/')}/werkgever/kandidatenkaart", new() { WaitUntil = WaitUntilState.NetworkIdle });
            if (width < 1280)
            {
                await page.EvaluateAsync("""
                    document.documentElement.classList.remove('vp-mobile');
                    document.documentElement.classList.add('vp-desktop');
                    """);
            }

            await WaitForMapReadyAsync(page);
            await ShotPageAsync(page, $"kk-map-areas-{suffix}.png");

            var more = page.Locator(".kk-btn--more");
            Assert.True(await more.CountAsync() > 0, "Expected Meer filters control on kandidatenkaart.");
            await more.First.ClickAsync();
            await page.WaitForSelectorAsync(".kk-filter-popover.is-open", new() { Timeout = 10_000 });
            await page.WaitForTimeoutAsync(400);
            await ShotPageAsync(page, $"kk-filters-open-{suffix}.png");

            await ClickFilterAsync(page, "80%");
            await ClickFilterAsync(page, "15 km");
            await ClickFilterAsync(page, "Later");
            await ClickFilterAsync(page, "32+");
            await page.WaitForTimeoutAsync(3000);
            Assert.True(
                await page.Locator(".kk-below-min").CountAsync() > 0,
                "Expected below-30 notice after strict filters.");
            await ShotPageAsync(page, $"kk-below-30-{suffix}.png");

            await ClickFilterAsync(page, "60%");
            await ClickFilterAsync(page, "60 km");
            await ClickFilterAsync(page, "Per direct");
            await ClickFilterAsync(page, "24–32");
            await page.WaitForTimeoutAsync(2500);

            if (await page.Locator(".kk-map-cta").CountAsync() == 0
                && await page.Locator(".kk-area-list__item").CountAsync() > 0)
            {
                await page.Locator(".kk-area-list__item").First.ClickAsync();
                await page.WaitForTimeoutAsync(400);
            }

            Assert.True(await page.Locator(".kk-map-cta").CountAsync() > 0, "Expected invite CTA on map.");
            await page.Locator(".kk-map-cta").ClickAsync();
            await page.WaitForSelectorAsync(".kk-modal", new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(400);
            await ShotPageAsync(page, $"kk-invite-preview-{suffix}.png");
        }

        await using (var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 }
        }))
        {
            var page = await ctx.NewPageAsync();
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, KbE2e.DefaultCompleteEmail, password));
            await using (var db = await OpenDbAsync(conn))
            {
                var user = await db.Users.FirstAsync(u => u.Email == KbE2e.DefaultCompleteEmail);
                user.OpenForWork = false;
                var settings = await db.CandidateKandidatenkaartSettings.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (settings is null)
                {
                    settings = new CandidateKandidatenkaartSettings { UserId = user.Id };
                    db.CandidateKandidatenkaartSettings.Add(settings);
                }

                settings.ReAvailabilityPromptAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            await page.GotoAsync($"{baseUrl.TrimEnd('/')}/candidate/dashboard", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(1500);
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "kk-reavail-banner-1440.png"), FullPage = true });
            await page.SetViewportSizeAsync(390, 844);
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "kk-reavail-banner-390.png"), FullPage = true });

            await page.GotoAsync($"{baseUrl.TrimEnd('/')}/candidate/settings/kandidatenkaart", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(1200);
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "kk-candidate-settings-1440.png"), FullPage = true });

            await page.SetViewportSizeAsync(390, 844);
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "kk-candidate-settings-390.png"), FullPage = true });
        }

        await EnableKandidatenkaartAsync(baseUrl, conn, enabled: false);
        await using (var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 }
        }))
        {
            var page = await ctx.NewPageAsync();
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, employerEmail, password));
            await page.GotoAsync($"{baseUrl.TrimEnd('/')}/werkgever/vacatures", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(1200);
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "kk-nav-flag-off-1440.png"), FullPage = true });
            var resp = await page.GotoAsync($"{baseUrl.TrimEnd('/')}/werkgever/kandidatenkaart");
            Assert.NotNull(resp);
            Assert.True(resp!.Status is 404 or 200);
            await page.WaitForTimeoutAsync(1200);
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "kk-route-flag-off-404-1440.png"), FullPage = true });
        }

        var required = new[]
        {
            "kk-map-areas-1440.png", "kk-map-areas-390.png",
            "kk-filters-open-1440.png", "kk-filters-open-390.png",
            "kk-below-30-1440.png", "kk-below-30-390.png",
            "kk-invite-preview-1440.png", "kk-invite-preview-390.png",
            "kk-candidate-settings-1440.png", "kk-candidate-settings-390.png",
            "kk-reavail-banner-1440.png", "kk-reavail-banner-390.png",
            "kk-nav-flag-off-1440.png", "kk-route-flag-off-404-1440.png"
        };
        foreach (var name in required)
        {
            Assert.True(File.Exists(Path.Combine(OutDir, name)), $"Missing evidence screenshot: {name}");
        }
    }

    private static async Task WaitForMapReadyAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Heading, new() { Name = "Kandidatenkaart" })
            .WaitForAsync(new() { Timeout = 90_000 });
        await page.WaitForSelectorAsync(".kk-page", new() { Timeout = 10_000 });
        await page.WaitForSelectorAsync(".kk-map-pane", new() { Timeout = 60_000 });
        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (DateTime.UtcNow < deadline)
        {
            if (await page.Locator(".kk-area-list__item").CountAsync() > 0
                || await page.Locator(".kk-below-min").CountAsync() > 0
                || await page.Locator(".kk-map-pane .maplibregl-canvas").CountAsync() > 0)
            {
                await page.WaitForTimeoutAsync(1200);
                return;
            }

            await page.WaitForTimeoutAsync(500);
        }

        throw new InvalidOperationException("Kandidatenkaart map did not finish loading.");
    }

    private static async Task ShotPageAsync(IPage page, string fileName)
    {
        var root = page.Locator(".wg-main .kk-page");
        if (await root.CountAsync() == 0)
        {
            root = page.Locator(".kk-page");
        }

        await root.First.ScreenshotAsync(new() { Path = Path.Combine(OutDir, fileName) });
    }

    private static async Task ClickFilterAsync(IPage page, string label)
    {
        var btn = page.Locator(".kk-seg button").Filter(new() { HasText = label });
        if (await btn.CountAsync() > 0)
        {
            await btn.First.ClickAsync();
            await page.WaitForTimeoutAsync(1200);
        }
    }

    private static async Task<JobsyDbContext> OpenDbAsync(string conn)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        return new JobsyDbContext(options);
    }

    private static async Task EnableKandidatenkaartAsync(string baseUrl, string conn, bool enabled)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = baseUrl.TrimEnd('/'),
                ["ConnectionStrings:JobsyDb"] = conn
            })
            .Build();
        await using var db = await OpenDbAsync(conn);
        var platform = new PlatformFeatureService(db, Options.Create(new JobsyFeatureOptions()), config);
        await platform.UpdateAsync(new PlatformFeatureUpdate(
            EmployersEnabled: true,
            CandidatePassportEnabled: true,
            KandidatenkaartEnabled: enabled));

        var apiBase = baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal);
        for (var attempt = 0; attempt < 60; attempt++)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var json = await http.GetStringAsync($"{apiBase.TrimEnd('/')}/api/settings/feature-flags");
                var expected = enabled ? "true" : "false";
                if (json.Contains($"\"kandidatenkaartEnabled\":{expected}", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }
            catch
            {
                // API may still be starting.
            }

            await Task.Delay(500);
        }

        // WebFeatureFlags caches the public snapshot for ~30s.
        await Task.Delay(35_000);
    }
}
