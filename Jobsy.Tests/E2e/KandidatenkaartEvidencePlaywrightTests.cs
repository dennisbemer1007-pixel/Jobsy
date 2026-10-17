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
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        const string employerEmail = "ondernemer@jobsy.local";
        const string password = KbE2e.DefaultPassword;

        foreach (var (suffix, width, height) in new[] { ("1440", 1440, 900) })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height }
            });
            var page = await context.NewPageAsync();
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, employerEmail, password));
            await page.GotoAsync($"{baseUrl.TrimEnd('/')}/werkgever/kandidatenkaart", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.EvaluateAsync("""
                document.documentElement.classList.remove('vp-mobile');
                document.documentElement.classList.add('vp-desktop');
                """);
            await page.WaitForTimeoutAsync(2500);
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"kk-map-areas-{suffix}.png"), FullPage = true });

            if (width >= 1280)
            {
                var more = page.GetByRole(AriaRole.Button, new() { Name = "Meer filters" });
                if (await more.CountAsync() > 0)
                {
                    await more.First.ClickAsync();
                    await page.WaitForTimeoutAsync(400);
                    await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"kk-filters-open-{suffix}.png"), FullPage = true });
                }

                var strictMatch = page.GetByRole(AriaRole.Button, new() { Name = "80%" });
                if (await strictMatch.CountAsync() > 0)
                {
                    await strictMatch.First.ClickAsync();
                    await page.WaitForTimeoutAsync(2000);
                }

                var travel15 = page.GetByRole(AriaRole.Button, new() { Name = "15 km" });
                if (await travel15.CountAsync() > 0)
                {
                    await travel15.First.ClickAsync();
                    await page.WaitForTimeoutAsync(2000);
                }

                if (await page.Locator(".kk-below-min").CountAsync() > 0)
                {
                    await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"kk-below-30-{suffix}.png"), FullPage = true });
                }

                if (await page.Locator(".kk-map-cta").CountAsync() > 0)
                {
                    await page.Locator(".kk-map-cta").ClickAsync();
                    await page.WaitForTimeoutAsync(800);
                    await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"kk-invite-preview-{suffix}.png"), FullPage = true });
                }
            }
        }

        // Mobile-only map capture (filters UI is desktop-only in current layout).
        await using (var mobileCtx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 }
        }))
        {
            var page = await mobileCtx.NewPageAsync();
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, employerEmail, password));
            await page.GotoAsync($"{baseUrl.TrimEnd('/')}/werkgever/kandidatenkaart", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2500);
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "kk-map-areas-390.png"), FullPage = true });
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
            await page.WaitForTimeoutAsync(800);
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "kk-route-flag-off-404-1440.png"), FullPage = true });
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
        await Task.Delay(enabled ? 5000 : 3000);
    }
}
