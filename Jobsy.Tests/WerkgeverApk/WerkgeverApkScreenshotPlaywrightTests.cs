using System.Security.Claims;
using System.Text.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.WerkgeverApk;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Privacy;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.ValueObjects;
using Jobsy.Core.WerkgeverApk;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.WerkgeverApk;
using Jobsy.Tests.TestSupport;
using Jobsy.Web.Auth;
using Jobsy.Web.Hosting;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests.WerkgeverApk;

[Collection("PlaywrightSmoke")]
public sealed class WerkgeverApkScreenshotPlaywrightTests
    : IClassFixture<RoleFunctionalWebAppFactory>, IAsyncLifetime
{
    private const string EnvRun = "JOBSY_WERKGEVER_APK_SCREENSHOTS";

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    private static string OutDir => "/opt/cursor/artifacts/werkgever-apk";


    private readonly RoleFunctionalWebAppFactory _api;
    private WerkgeverApkWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private WerkgeverApkSeed? _seed;

    public WerkgeverApkScreenshotPlaywrightTests(RoleFunctionalWebAppFactory api) => _api = api;

    public async ValueTask InitializeAsync()
    {
        if (!ShouldRun)
        {
            return;
        }

        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        await EnableLoopbaanApkFeaturesAsync();
        _seed = await SeedDemoTeamAsync();
        var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Jobsy.Web"));
        if (Directory.Exists(webRoot))
        {
            WerkgeverApkTemplateLocator.SetWebContentRoot(webRoot);
        }

        _web = new WerkgeverApkWebFactory(_api.Server.CreateHandler());
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Fact]
    public void Login_ssr_includes_coach_markup_in_employee_frame()
    {
        var html = WerkgeverApkEmployeeSsrHtml.RenderDocument(
            "Test",
            "<p>body</p>",
            new Dictionary<string, string> { ["company"] = "Demo", ["team"] = "Team", ["code"] = "ABC" });
        Assert.Contains("data-ssr-coach-open", html, StringComparison.Ordinal);
    }

    [Fact(Timeout = 600_000)]
    public async Task Capture_e2e_loopbaan_apk_screens_when_env_set()
    {
        _ = TestContext.Current.CancellationToken;
        if (!ShouldRun || _browser is null || _web is null || _seed is null)
        {
            return;
        }

        Directory.CreateDirectory(OutDir);
        var baseUrl = _web.ServerAddress.TrimEnd('/');

        // 01 — login (code only, no navigation)
        await using (var context = await NewContextAsync(1440, 900))
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync($"{baseUrl}/loopbaan-apk", DomReady);
            await AssertBodyContainsAsync(page, "Jouw Loopbaan-APK");
            await CapturePairAsync(page, "01-login");
        }

        // 02–03 — employee: invite → welkom → start → beantwoord 1 vraag
        await using (var context = await NewContextAsync(1440, 900))
        {
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);
            await EmployeeLoginAsync(page, baseUrl, _seed.InvitedPlainCode);
            await AssertUrlContainsAsync(page, "/loopbaan-apk/welkom");
            await AssertBodyContainsAsync(page, "Dit is jouw ontwikkelgesprek, niet een beoordeling");
            await CapturePairAsync(page, "02-welkom");
            await page.Locator("button:has-text('Start de interessetest')").ClickAsync();
            await page.WaitForURLAsync("**/loopbaan-apk/test/**", DomReadyUrl);
            await AssertBodyContainsAsync(page, "met mijn handen aan iets wat je kunt zien");
            await AssertBodyContainsAsync(page, "Vraag 1 van 48");
            var progressLabels = await page.Locator(".q-progress, .step.on .s, .mob-progress").AllInnerTextsAsync();
            Assert.All(progressLabels, t => Assert.Contains("1", t, StringComparison.Ordinal));
            Assert.All(progressLabels, t => Assert.DoesNotContain("12 / 40", t, StringComparison.Ordinal));
            await page.Locator("input[type=radio][value=\"3\"]").First.CheckAsync(new() { Force = true });
            await AssertBodyContainsAsync(page, "Hoe goed past dit bij jou");
            await CapturePairAsync(page, "03-test");
            await page.Locator("footer.foot button[type=submit]").ClickAsync(new() { Force = true });
        }

        // 04 — delen (klaar, nog niet gedeeld) met voorbeeld
        await using (var context = await NewContextAsync(1440, 900))
        {
            var page = await context.NewPageAsync();
            await EmployeeLoginAsync(page, baseUrl, _seed.DelenPlainCode);
            await AssertUrlContainsAsync(page, "/loopbaan-apk/delen");
            await AssertBodyContainsAsync(page, "Wil je jouw beeld delen met je leidinggevende?");
            await AssertBodyContainsAsync(page, "te zien krijgt");
            await CapturePairAsync(page, "04-delen");
        }

        // 05 — vergelijking (gedeeld + manager ingevuld)
        await using (var context = await NewContextAsync(1440, 900))
        {
            var page = await context.NewPageAsync();
            await EmployeeLoginAsync(page, baseUrl, _seed.ComparisonPlainCode);
            await AssertUrlContainsAsync(page, "/loopbaan-apk/vergelijking");
            await AssertBodyContainsAsync(page, "Hier zijn jullie het over eens");
            await AssertBodyContainsAsync(page, "prettig samenwerkt en rekening houdt");
            await AssertBodyContainsAsync(page, "Hier kijken jullie anders");
            await AssertBodyContainsAsync(page, "Nieuwe dingen proberen");
            var codeChips = await page.Locator(".topbar .chip-code").CountAsync();
            Assert.Equal(1, codeChips);
            await CapturePairAsync(page, "05-vergelijking");
        }

        // 06 — teamoverzicht bedrijfsmanager
        await using (var context = await NewContextAsync(1440, 900))
        {
            var page = await context.NewPageAsync();
            var teamPath = $"/werkgever/loopbaan-apk/teams/{_seed.TeamId:D}";
            await ManagerGotoAsync(page, baseUrl, _api.EnterpriseId, _api.CompanyId, teamPath);
            await AssertBodyContainsAsync(page, "Wie is uitgenodigd, bezig of heeft gedeeld?");
            await AssertBodyContainsAsync(page, "Gedeeld");
            await CapturePairAsync(page, "06-team");
        }

        // 07 — 10 vragen voor Sara (manager-sheet deels ingevuld)
        await using (var context = await NewContextAsync(1440, 900))
        {
            var page = await context.NewPageAsync();
            var vragen = $"/werkgever/loopbaan-apk/codes/{_seed.ManagerVragenCodeId:D}/vragen";
            await ManagerGotoAsync(page, baseUrl, _api.EnterpriseId, _api.CompanyId, vragen);
            await AssertBodyContainsAsync(page, "Hoe herken jij dit bij");
            await AssertBodyContainsAsync(page, "Vraag 4 van 10");
            await AssertBodyContainsAsync(page, "Dit is geen beoordeling");
            await CapturePairAsync(page, "07-vragen");
        }

        // 08 — groepsbeeld (7 gedeeld, mockup-grafieken)
        await using (var context = await NewContextAsync(1440, 900))
        {
            var page = await context.NewPageAsync();
            var groep = $"/werkgever/loopbaan-apk/teams/{_seed.TeamId:D}/groep";
            await ManagerGotoAsync(page, baseUrl, _api.EnterpriseId, _api.CompanyId, groep);
            await AssertBodyContainsAsync(page, "Team Logistiek · 7 deelnemers");
            await AssertBodyContainsAsync(page, "Interesses in de groep");
            await CapturePairAsync(page, "08-groep");
        }

        // 09 — admin schakelaar
        await using (var context = await NewContextAsync(1440, 900))
        {
            var page = await context.NewPageAsync();
            await PlaywrightCookieConsent.AcceptAsync(context);
            await page.GotoAsync(
                $"{baseUrl}/__test/staff?user={_api.AdminId:D}&role=Admin&return={Uri.EscapeDataString("/admin/organisaties")}",
                DomReady);
            await page.GotoAsync($"{baseUrl}/admin/organisaties", new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 120_000
            });
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await DismissConsentReacceptIfPresentAsync(page);
            await page.WaitForSelectorAsync(".admin-orgs", new() { Timeout = 60_000 });
            var demoRow = page.Locator("tr.admin-org-row").Filter(new() { HasText = "Demo Logistiek B.V." });
            await demoRow.First.ClickAsync();
            await page.GetByText("Loopbaan-APK aan", new() { Exact = true }).WaitForAsync(new() { Timeout = 30_000 });
            await CapturePairAsync(page, "09-admin-loopbaan-toggle");
        }
    }

    private async Task<IBrowserContext> NewContextAsync(int width, int height)
        => await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            Locale = "nl-NL"
        });

    private static async Task EmployeeLoginAsync(IPage page, string baseUrl, string plainCode)
    {
        await page.Context.ClearCookiesAsync();
        await page.GotoAsync($"{baseUrl}/loopbaan-apk", DomReady);
        await page.Locator("input[name=code]").FillAsync(plainCode);
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(
            url => url.Contains("/loopbaan-apk/", StringComparison.OrdinalIgnoreCase)
                   && !url.Contains("/loopbaan-apk?", StringComparison.OrdinalIgnoreCase),
            DomReadyUrl);
    }

    private static async Task ManagerGotoAsync(
        IPage page,
        string baseUrl,
        Guid enterpriseUserId,
        Guid companyId,
        string returnPath)
    {
        await page.GotoAsync(
            $"{baseUrl}/__test/staff?user={enterpriseUserId:D}&role=EnterpriseManager&company={companyId:D}&return={Uri.EscapeDataString(returnPath)}",
            DomReady);
        await page.GotoAsync(baseUrl + returnPath, DomReady);
    }

    private static async Task AssertBodyContainsAsync(IPage page, string text)
    {
        var body = await page.ContentAsync();
        if (!body.Contains(text, StringComparison.Ordinal))
        {
            var debug = Path.Combine(OutDir, "debug-last-page.html");
            await File.WriteAllTextAsync(debug, body);
        }

        Assert.True(
            body.Contains(text, StringComparison.Ordinal),
            $"Expected “{text}” on {page.Url}. Wrote HTML to {Path.Combine(OutDir, "debug-last-page.html")}");
    }

    private static void AssertUrlContains(IPage page, string fragment)
        => Assert.Contains(fragment, page.Url, StringComparison.OrdinalIgnoreCase);

    private static Task AssertUrlContainsAsync(IPage page, string fragment)
    {
        AssertUrlContains(page, fragment);
        return Task.CompletedTask;
    }

    private async Task CapturePairAsync(IPage page, string slug)
    {
        await CaptureViewportAsync(page, slug, 1440, 900);
        await CaptureViewportAsync(page, slug, 390, 844);
    }

    private async Task CaptureViewportAsync(IPage page, string slug, int width, int height)
    {
        await page.SetViewportSizeAsync(width, height);
        for (var pass = 0; pass < 3; pass++)
        {
            var scrollY = pass switch
            {
                0 => 0,
                1 => await page.EvaluateAsync<int>("() => Math.max(0, Math.floor((document.body.scrollHeight - window.innerHeight) / 2))"),
                _ => await page.EvaluateAsync<int>("() => Math.max(0, document.body.scrollHeight - window.innerHeight)")
            };
            await page.EvaluateAsync("y => window.scrollTo(0, y)", scrollY);
            await AssertNoCoachOverlapAsync(page, $"{slug}@{width} scroll={scrollY}");
        }

        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(OutDir, $"e2e-{slug}-{width}.png"),
            FullPage = true
        });
    }

    private static async Task DismissConsentReacceptIfPresentAsync(IPage page)
    {
        var modal = page.Locator(".consent-reaccept-modal");
        if (await modal.CountAsync() == 0)
        {
            return;
        }

        await modal.Locator(".consent-reaccept__check input").CheckAsync();
        await modal.Locator(".btn-compact--primary").ClickAsync();
        await modal.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 30_000 });
    }

    private static async Task AssertNoCoachOverlapAsync(IPage page, string context)
    {
        var overlap = await page.EvaluateAsync<bool>(
            """
            () => {
              const coach = document.querySelector('[data-ssr-coach-open], .coach-fab, #lobsy-coach-btn, .ll-g78-coach, button.coach, .coach');
              if (!coach) return false;
              const bubble = coach.querySelector('.bubble');
              if (!bubble) return false;
              const br = bubble.getBoundingClientRect();
              if (br.width < 2 || br.height < 2) return false;
              const targets = document.querySelectorAll('button, a, .btn, .btn-primary, .card, h1, .next');
              for (const el of targets) {
                if (el === coach || coach.contains(el)) continue;
                if (!el.offsetParent) continue;
                const st = window.getComputedStyle(el);
                if (st.display === 'none' || st.visibility === 'hidden' || st.opacity === '0') continue;
                const r = el.getBoundingClientRect();
                if (r.width < 2 || r.height < 2) continue;
                const pad = 4;
                const hitBubble = !(r.right < br.left + pad || r.left > br.right - pad || r.bottom < br.top + pad || r.top > br.bottom - pad);
                if (hitBubble) return true;
              }
              return false;
            }
            """);
        Assert.False(overlap, $"Coach overlaps primary UI on {context} (url: {page.Url})");
    }

    private async Task EnableLoopbaanApkFeaturesAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            row = new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
            };
            db.PlatformFeatureSettings.Add(row);
        }

        row.EmployersEnabled = false;
        row.EmployerLoopbaanApkEnabled = true;

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Id == _api.AdminId);
        if (admin is not null)
        {
            admin.TermsAcceptedAt = DateTime.UtcNow;
            admin.ConsentVersion = PrivacyConstants.CurrentConsentVersion;
        }

        await db.SaveChangesAsync();
        scope.ServiceProvider.GetRequiredService<IFeatureFlags>().Invalidate();
    }

    private async Task<WerkgeverApkSeed> SeedDemoTeamAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IWerkgeverApkEmployeeCodeService>();

        var companyId = _api.CompanyId;
        var company = await db.Companies.FirstAsync(c => c.Id == companyId);
        company.LoopbaanApkEnabled = true;
        company.Name = "Demo Logistiek B.V.";
        company.ParentCompanyId = null;

        var team = await db.WerkgeverApkTeams.FirstOrDefaultAsync(t =>
            t.CompanyId == companyId && t.Name == "Team Logistiek");
        if (team is null)
        {
            team = new WerkgeverApkTeam
            {
                Id = Guid.Parse("d1000000-0000-0000-0000-0000000000a1"),
                CompanyId = companyId,
                Name = "Team Logistiek",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = _api.EnterpriseId
            };
            db.WerkgeverApkTeams.Add(team);
            await db.SaveChangesAsync();
        }

        var existing = await db.WerkgeverApkEmployeeCodes.CountAsync(c => c.TeamId == team.Id);
        if (existing < 9)
        {
            await codes.GenerateAsync(9 - existing, team);
            await db.SaveChangesAsync();
        }

        var allCodes = await db.WerkgeverApkEmployeeCodes
            .Include(c => c.Progress)
            .Include(c => c.ManagerSheet)
            .Where(c => c.TeamId == team.Id)
            .OrderBy(c => c.Number)
            .ToListAsync();

        var plainCodes = allCodes.Select(c => codes.Unprotect(c.CodeProtected)!).ToList();
        var progressJson = CompleteProgressJson();
        var comparisonProgressJson = ComparisonProgressJson();
        var managerPartial = WerkgeverApkManagerQuestionCatalog.All
            .Take(3)
            .ToDictionary(q => q.Id, _ => 4);
        var managerAnswers = JsonSerializer.Serialize(
            WerkgeverApkManagerQuestionCatalog.All.ToDictionary(q => q.Id, _ => 4));
        var comparisonManagerAnswers = ComparisonManagerAnswersJson();
        var managerPartialJson = JsonSerializer.Serialize(managerPartial);

        for (var i = 0; i < allCodes.Count; i++)
        {
            var code = allCodes[i];
            if (i < 7)
            {
                code.Status = WerkgeverApkEmployeeCodeStatus.Completed;
                code.SharedWithManager = true;
                code.SharedAtUtc = DateTime.UtcNow;
                EnsureProgress(code, i == 0 ? comparisonProgressJson : progressJson, welcomeSeen: true);
                if (i == 0)
                {
                    EnsureManagerSheet(code, comparisonManagerAnswers, completed: true);
                }
                else if (i == 1)
                {
                    EnsureManagerSheet(code, managerPartialJson, completed: false);
                }
                else
                {
                    EnsureManagerSheet(code, "{}", completed: false);
                }
            }
            else if (i == 7)
            {
                code.Status = WerkgeverApkEmployeeCodeStatus.NotStarted;
                code.SharedWithManager = false;
                code.Label = null;
            }
            else
            {
                code.Status = WerkgeverApkEmployeeCodeStatus.Completed;
                code.SharedWithManager = false;
                EnsureProgress(code, progressJson, welcomeSeen: true);
                EnsureManagerSheet(code, "{}", completed: false);
            }
        }

        allCodes[0].Label = "Sara de Wit";
        allCodes[1].Label = "Sara de Wit";
        await db.SaveChangesAsync();

        return new WerkgeverApkSeed(
            team.Id,
            allCodes[0].Id,
            allCodes[1].Id,
            plainCodes[0],
            plainCodes[7],
            plainCodes[8]);
    }

    private static void EnsureProgress(WerkgeverApkEmployeeCode code, string json, bool welcomeSeen)
    {
        code.Progress ??= new WerkgeverApkEmployeeProgress { EmployeeCodeId = code.Id };
        code.Progress.ProgressJson = json;
        code.Progress.WelcomeSeen = welcomeSeen;
        code.Progress.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void EnsureManagerSheet(WerkgeverApkEmployeeCode code, string answersJson, bool completed)
    {
        code.ManagerSheet ??= new WerkgeverApkManagerSheet { EmployeeCodeId = code.Id };
        code.ManagerSheet.AnswersJson = answersJson;
        code.ManagerSheet.Completed = completed;
        code.ManagerSheet.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string CompleteProgressJson()
    {
        var catalog = WerkgeverApkEmployeeCatalog.Default;
        var answers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var block in WerkgeverApkEntryPaths.EmployeeBlocks)
        {
            foreach (var q in catalog.QuestionsFor(block, SchoolLevel.Mbo3))
            {
                answers[q.Id] = 3;
            }
        }

        return JsonSerializer.Serialize(new { answers });
    }

    private static string ComparisonProgressJson()
    {
        var catalog = WerkgeverApkEmployeeCatalog.Default;
        var answers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var block in WerkgeverApkEntryPaths.EmployeeBlocks)
        {
            foreach (var q in catalog.QuestionsFor(block, SchoolLevel.Mbo3))
            {
                answers[q.Id] = 3;
            }
        }

        foreach (var q in catalog.QuestionsFor(PupilMboHboBlock.Competentie, SchoolLevel.Mbo3))
        {
            if (string.Equals(q.Dimension, "Samenwerken", StringComparison.Ordinal))
            {
                answers[q.Id] = 5;
            }
            else if (q.Dimension.StartsWith("Creativiteit", StringComparison.Ordinal))
            {
                answers[q.Id] = 1;
            }
            else if (string.Equals(q.Dimension, "Contact maken (Extraversie)", StringComparison.Ordinal))
            {
                answers[q.Id] = 5;
            }
        }

        return JsonSerializer.Serialize(new { answers });
    }

    private static string ComparisonManagerAnswersJson()
    {
        var dict = WerkgeverApkManagerQuestionCatalog.All.ToDictionary(q => q.Id, _ => 4);
        return JsonSerializer.Serialize(dict);
    }

    private static readonly PageGotoOptions DomReady = new()
    {
        WaitUntil = WaitUntilState.DOMContentLoaded,
        Timeout = 90_000
    };

    private static readonly PageWaitForURLOptions DomReadyUrl = new()
    {
        WaitUntil = WaitUntilState.DOMContentLoaded,
        Timeout = 90_000
    };

    private sealed record WerkgeverApkSeed(
        Guid TeamId,
        Guid ComparisonCodeId,
        Guid ManagerVragenCodeId,
        string ComparisonPlainCode,
        string InvitedPlainCode,
        string DelenPlainCode);

    private sealed class WerkgeverApkWebFactory : WebWebApplicationFactory
    {
        private readonly HttpMessageHandler _api;
        private IHost? _kestrel;

        public string ServerAddress { get; private set; } = "";

        public WerkgeverApkWebFactory(HttpMessageHandler api)
        {
            _api = api;
            _ = CreateClient();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiBaseUrl"] = "http://api.test/",
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.Configure<HostOptions>(o =>
                    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
                services.AddSingleton<IStartupFilter, StaffTestSignInFilter>();
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new LoopbaanApkOnFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new ForwardFactory(_api));
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(sp =>
                {
                    var auth = new JobsyApiAuthHandler(
                        sp.GetRequiredService<IHttpContextAccessor>(),
                        sp.GetRequiredService<AuthenticationStateProvider>(),
                        sp,
                        sp.GetRequiredService<IConfiguration>(),
                        sp.GetRequiredService<JobsyAccessTokenIssuer>())
                    {
                        InnerHandler = new ForwardHandler(_api)
                    };
                    return new JobsyApiClient(new HttpClient(auth, disposeHandler: false)
                    {
                        BaseAddress = new Uri("http://api.test/")
                    });
                });
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var testHost = builder.Build();
            builder.ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseKestrel();
                webHostBuilder.UseUrls("http://127.0.0.1:0");
            });
            _kestrel = builder.Build();
            _kestrel.Start();
            ServerAddress = _kestrel.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!
                .Addresses.First(a => a.StartsWith("http://", StringComparison.Ordinal));
            testHost.Start();
            return testHost;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _kestrel?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class LoopbaanApkOnFlags : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(
            EmployersEnabled: false,
            CandidatePassportEnabled: true,
            EmployerLoopbaanApkEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class StaffTestSignInFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (ctx, nxt) =>
                {
                    if (!ctx.Request.Path.Equals("/__test/staff", StringComparison.OrdinalIgnoreCase))
                    {
                        await nxt();
                        return;
                    }

                    var user = ctx.Request.Query["user"].ToString();
                    var role = ctx.Request.Query["role"].ToString();
                    if (string.IsNullOrWhiteSpace(role))
                    {
                        role = JobsyRoles.EnterpriseManager;
                    }

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user),
                        new(ClaimTypes.Role, role),
                        new(JobsyClaimTypes.SessionVersion, "0"),
                        new(JobsyClaimTypes.MfaVerified, "1")
                    };
                    var company = ctx.Request.Query["company"].ToString();
                    if (Guid.TryParse(company, out var companyId))
                    {
                        claims.Add(new Claim(JobsyClaimTypes.CompanyId, companyId.ToString("D")));
                    }
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/') || ret.StartsWith("//", StringComparison.Ordinal))
                    {
                        ret = "/werkgever/loopbaan-apk";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class ForwardFactory(HttpMessageHandler api) : IHttpClientFactory, IDisposable
    {
        private readonly ForwardHandler _handler = new(api);

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false)
        {
            BaseAddress = new Uri("http://api.test/")
        };

        public void Dispose() => _handler.Dispose();
    }

    private sealed class ForwardHandler(HttpMessageHandler api) : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _invoker = new(api, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? "/";
            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }

            using var clone = new HttpRequestMessage(request.Method, new Uri("http://localhost" + path));
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is not null)
            {
                clone.Content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync(cancellationToken));
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return await _invoker.SendAsync(clone, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _invoker.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
