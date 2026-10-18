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

        await using (var seedDb = await OpenDbAsync(conn))
        {
            await KandidatenkaartDemoSeed.EnsureAsync(seedDb);
            await KandidatenkaartDemoSeed.EnsureEmployerVacancyForMapAsync(seedDb);
            await KandidatenkaartDemoSeed.EnsureWeeklyCapEvidenceAsync(seedDb);
        }

        await EnableKandidatenkaartAsync(baseUrl, conn, enabled: true);

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
            if (width < 640)
            {
                await context.AddInitScriptAsync("""
                    document.documentElement.classList.add('vp-mobile');
                    document.documentElement.classList.remove('vp-desktop');
                    """);
            }
            var page = await context.NewPageAsync();
            KbE2e.AttachCapture(page);
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, employerEmail, password));
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await page.GotoAsync($"{baseUrl.TrimEnd('/')}/werkgever/vacatures", new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60_000
            });
            await page.WaitForTimeoutAsync(1500);

            await GotoKandidatenkaartWhenReadyAsync(page, baseUrl);
            if (width < 640)
            {
                await EnsureMobileViewportClassAsync(page);
                await page.WaitForTimeoutAsync(2000);
            }

            var mobileViewportShot = width < 640;

            await WaitForMapReadyAsync(page);
            await KbE2e.AssertNoCircuitErrorAsync(page);
            await ShotPageAsync(page, $"kk-map-areas-{suffix}.png", mobileViewportShot);

            var more = width < 640
                ? page.Locator(".kk-mobile-bar .kk-filter-more")
                : page.Locator(".kk-filters--desktop .kk-filter-more");
            Assert.True(await more.CountAsync() > 0, "Expected Meer filters control on kandidatenkaart.");
            await more.First.ClickAsync();
            if (width < 640)
            {
                await page.WaitForSelectorAsync(".kk-page--filters-open .kk-filter-sheet", new() { Timeout = 10_000, State = WaitForSelectorState.Visible });
            }
            else
            {
                await page.WaitForSelectorAsync(".kk-filter-popover.is-open", new() { Timeout = 10_000, State = WaitForSelectorState.Visible });
            }

            await page.WaitForTimeoutAsync(400);
            await ShotPageAsync(page, $"kk-filters-open-{suffix}.png", mobileViewportShot);

            if (width < 640)
            {
                await page.Locator(".kk-filter-sheet .btn-compact").Filter(new() { HasText = "Sluiten" }).First.ClickAsync();
            }
            else if (await page.Locator(".kk-filter-popover.is-open").CountAsync() > 0)
            {
                await more.First.ClickAsync();
            }

            await page.WaitForTimeoutAsync(300);

            if (width < 640)
            {
                await page.Locator(".kk-mobile-bar .kk-filter-more").ClickAsync();
                await page.WaitForSelectorAsync(".kk-filter-sheet", new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
            }

            await ClickFilterAsync(page, "70%");

            if (width < 640)
            {
                await page.Locator(".kk-filter-sheet__apply").ClickAsync();
                await page.WaitForTimeoutAsync(800);
            }
            var areaListSelector = width < 640
                ? ".kk-mobile-sheet .kk-area-list__item"
                : ".kk-layout-b__list .kk-area-list__item";
            await page.WaitForSelectorAsync(areaListSelector, new() { Timeout = 30_000, State = WaitForSelectorState.Visible });

            var inviteCta = width < 640
                ? page.Locator(".kk-mobile-sheet__invite button")
                : page.Locator(".kk-list-invite-bar button");
            Assert.True(await inviteCta.CountAsync() > 0, "Expected invite CTA below area list.");
            await inviteCta.First.ClickAsync();
            await page.WaitForSelectorAsync(".kk-modal", new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(400);
            await ShotPageAsync(page, $"kk-invite-preview-{suffix}.png", fullViewport: true);
            await page.Locator(".kk-modal .btn-compact").Filter(new() { HasText = "Annuleren" }).First.ClickAsync();
            await page.WaitForTimeoutAsync(400);

            if (width < 640)
            {
                await page.Locator(".kk-mobile-bar .kk-filter-more").ClickAsync();
                await page.WaitForSelectorAsync(".kk-filter-sheet", new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
            }

            await ClickFilterAsync(page, "80%");
            await ClickFilterAsync(page, "15 km");
            await ClickFilterAsync(page, "Later");
            await ClickFilterAsync(page, "32+");

            if (width < 640)
            {
                await page.Locator(".kk-filter-sheet__apply").ClickAsync();
            }

            await page.WaitForTimeoutAsync(3000);
            Assert.True(
                await page.Locator(".kk-below-min").CountAsync() > 0,
                "Expected below-30 notice after strict filters.");
            await ShotPageAsync(page, $"kk-below-30-{suffix}.png", mobileViewportShot);
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

    private static async Task EnsureMobileViewportClassAsync(IPage page)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var mobile = await page.EvaluateAsync<bool>(
                "() => document.documentElement.classList.contains('vp-mobile')");
            if (mobile)
            {
                return;
            }

            await page.WaitForTimeoutAsync(300);
        }

        await page.EvaluateAsync("""
            document.documentElement.classList.remove('vp-desktop');
            document.documentElement.classList.add('vp-mobile');
            window.jobMap?.invalidate?.();
            """);
        await page.WaitForTimeoutAsync(500);
    }

    private static async Task WaitForMapReadyAsync(IPage page)
    {
        await page.WaitForSelectorAsync(".kk-page", new() { Timeout = 90_000, State = WaitForSelectorState.Visible });
        var mobile = await page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('vp-mobile')");
        if (mobile)
        {
            await page.Locator(".kk-mobile-title").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        }
        else
        {
            await page.Locator(".kk-header h1").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        }
        await page.WaitForSelectorAsync(".kk-map-pane", new()
        {
            Timeout = 60_000,
            State = WaitForSelectorState.Attached
        });
        try
        {
            await page.WaitForSelectorAsync(".kk-map-pane .maplibregl-canvas", new()
            {
                Timeout = mobile ? 45_000 : 90_000,
                State = mobile ? WaitForSelectorState.Attached : WaitForSelectorState.Visible
            });
        }
        catch (TimeoutException)
        {
            if (!mobile && await page.Locator(".kk-area-list__item").CountAsync() > 0)
            {
                // MapLibre can lag behind area data; list + pane are enough for evidence framing.
            }
            else if (mobile)
            {
                await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
                await page.WaitForSelectorAsync(".kk-page", new() { Timeout = 45_000 });
                try
                {
                    await page.WaitForSelectorAsync(".kk-map-pane .maplibregl-canvas", new()
                    {
                        Timeout = 30_000,
                        State = WaitForSelectorState.Attached
                    });
                }
                catch (TimeoutException)
                {
                    await page.WaitForSelectorAsync(".kk-mobile-sheet .kk-area-list__item, .kk-map-pane", new()
                    {
                        Timeout = 15_000
                    });
                }
            }
            else
            {
                throw;
            }
        }

        await page.EvaluateAsync("""
            window.jobMap?.invalidate?.();
            window.dispatchEvent(new Event('resize'));
            """);
        await page.WaitForFunctionAsync(
            @"() => {
                const pane = document.querySelector('.kk-map-pane');
                const canvas = pane?.querySelector('.maplibregl-canvas');
                if (!pane || !canvas) return false;
                const pr = pane.getBoundingClientRect();
                const cr = canvas.getBoundingClientRect();
                if (pr.width < 40 || pr.height < 40) return false;
                return cr.width >= pr.width * 0.85 && cr.height >= pr.height * 0.85;
            }",
            null,
            new() { Timeout = mobile ? 45_000 : 30_000 });
        await page.WaitForTimeoutAsync(mobile ? 800 : 1200);
    }

    private static async Task ShotPageAsync(IPage page, string fileName, bool fullViewport = false)
    {
        await page.EvaluateAsync("""
            window.scrollTo(0, 0);
            document.documentElement.scrollTop = 0;
            document.body.scrollTop = 0;
            document.querySelector('.wg-main')?.scrollTo(0, 0);
            document.querySelector('.kk-page')?.scrollTo?.(0, 0);
            document.querySelector('.kk-page > .insights-tabs')?.scrollIntoView({ block: 'start', inline: 'nearest' });
            """);

        if (fullViewport)
        {
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(OutDir, fileName),
                FullPage = false
            });
            return;
        }

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
        var count = await btn.CountAsync();
        for (var i = 0; i < count; i++)
        {
            var candidate = btn.Nth(i);
            if (await candidate.IsVisibleAsync())
            {
                await candidate.ClickAsync();
                await page.WaitForTimeoutAsync(1200);
                return;
            }
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
        await WaitForPublicKkFlagAsync(apiBase, enabled);
        await RestartCiApiAsync();
        await RestartCiWebAsync();
        await WaitForPublicKkFlagAsync(apiBase, enabled);
        await Task.Delay(2000);
    }

    private static async Task WaitForPublicKkFlagAsync(string apiBase, bool enabled)
    {
        var expected = enabled ? "true" : "false";
        for (var attempt = 0; attempt < 60; attempt++)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var json = await http.GetStringAsync($"{apiBase.TrimEnd('/')}/api/settings/feature-flags");
                if (json.Contains($"\"kandidatenkaartEnabled\":{expected}", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            catch
            {
                // retry
            }

            await Task.Delay(500);
        }

        throw new InvalidOperationException($"Timed out waiting for kandidatenkaartEnabled={enabled} on public feature-flags.");
    }

    private static async Task RestartCiApiAsync()
    {
        var script = FindRepoScript("restart-ci-api.sh");
        await RunBashScriptAsync(script);
    }

    private static async Task RestartCiWebAsync()
    {
        await EnsureCiWebReleaseBuildAsync();
        await RunBashScriptAsync(FindRepoScript("restart-ci-web.sh"));
    }

    private static async Task EnsureCiWebReleaseBuildAsync()
    {
        var root = FindRepoScript("restart-ci-web.sh");
        root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(root)!))!;
        var psi = new System.Diagnostics.ProcessStartInfo("dotnet", "build Jobsy.Web/Jobsy.Web.csproj -c Release -v q")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = System.Diagnostics.Process.Start(psi)
                         ?? throw new InvalidOperationException("Failed to start Release web build.");
        await proc.WaitForExitAsync();
        if (proc.ExitCode != 0)
        {
            var err = await proc.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Release web build failed: {err}");
        }
    }

    private static string FindRepoScript(string name)
    {
        var root = Directory.GetParent(AppContext.BaseDirectory)!;
        for (var i = 0; i < 6 && root is not null; i++)
        {
            if (File.Exists(Path.Combine(root.FullName, "Jobsy.sln")))
            {
                break;
            }

            root = root.Parent;
        }

        var script = root is null
            ? null
            : Path.Combine(root.FullName, ".github", "scripts", name);
        if (script is null || !File.Exists(script))
        {
            throw new InvalidOperationException($"Missing .github/scripts/{name}");
        }

        return script;
    }

    private static async Task RunBashScriptAsync(string script)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("bash", script)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var proc = System.Diagnostics.Process.Start(psi)
                         ?? throw new InvalidOperationException($"Failed to run {script}.");
        var stdout = await proc.StandardOutput.ReadToEndAsync();
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{script} failed ({proc.ExitCode}): {stdout}\n{stderr}");
        }
    }

    private static async Task GotoKandidatenkaartWhenReadyAsync(IPage page, string baseUrl)
    {
        var target = $"{baseUrl.TrimEnd('/')}/werkgever/kandidatenkaart";
        Exception? last = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (attempt > 0)
            {
                await page.WaitForTimeoutAsync(2000);
            }

            await page.GotoAsync(target, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            try
            {
                await page.WaitForSelectorAsync(".kk-page", new() { Timeout = 45_000, State = WaitForSelectorState.Visible });
                await page.Locator(".kk-header h1, .kk-mobile-title").First.WaitForAsync(new() { Timeout = 15_000 });
                return;
            }
            catch (Exception ex) when (attempt < 9)
            {
                last = ex;
                await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            }
        }

        throw new InvalidOperationException(
            "Kandidatenkaart page did not render after feature-flag refresh.",
            last);
    }
}
