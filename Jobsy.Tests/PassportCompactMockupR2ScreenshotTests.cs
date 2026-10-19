using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Features;
using Jobsy.Web.Models;
using Jobsy.Tests.TestSupport;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Real Blazor paspoort with compact layout variants (r2 PNGs for PO review).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class PassportCompactMockupR2ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/paspoort-compact-mockups";
    private PaspoortCompactMockupWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new PaspoortCompactMockupWebFactory();
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        if (_web is not null)
        {
            await _web.DisposeAsync();
        }
    }

    [Fact]
    public async Task Capture_real_paspoort_compact_variant_c_r5()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var tableRows = new List<string>();
        foreach (var (width, height, tag) in Viewports())
        {
            tableRows.Add(await CaptureAsync("c", width, height, tag, revision: "r5"));
        }

        await SaveMarginCropsAsync(1920, 1080, revision: "r5");

        var md = BuildMarkdown(tableRows, revision: "r5", title: "Paspoort compact mockup variant C (r5, logo-E mark + soft spot fade)");
        await File.WriteAllTextAsync(Path.Combine(ArtifactsDir, "above-the-fold-coverage-r5.md"), md);
        await File.WriteAllTextAsync(
            Path.Combine(FindRepoRoot(), "tools", "paspoort-compact-mockups", "above-the-fold-coverage-r5.md"),
            md);
    }

    [Fact]
    public async Task Capture_real_paspoort_compact_variant_c_r4()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var tableRows = new List<string>();
        foreach (var (width, height, tag) in Viewports())
        {
            tableRows.Add(await CaptureAsync("c", width, height, tag, revision: "r4"));
        }

        await SaveMarginCropsAsync(1920, 1080, revision: "r4");

        var md = BuildMarkdown(tableRows, revision: "r4", title: "Paspoort compact mockup variant C (r4, margin art fix)");
        await File.WriteAllTextAsync(Path.Combine(ArtifactsDir, "above-the-fold-coverage-r4.md"), md);
        await File.WriteAllTextAsync(
            Path.Combine(FindRepoRoot(), "tools", "paspoort-compact-mockups", "above-the-fold-coverage-r4.md"),
            md);
    }

    [Fact]
    public async Task Capture_real_paspoort_compact_variant_c_r3()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var tableRows = new List<string>();
        foreach (var (width, height, tag) in Viewports())
        {
            tableRows.Add(await CaptureAsync("c", width, height, tag, revision: "r3"));
        }

        var md = BuildMarkdown(tableRows, revision: "r3", title: "Paspoort compact mockup variant C (r3, PO choice)");
        await File.WriteAllTextAsync(Path.Combine(ArtifactsDir, "above-the-fold-coverage-r3.md"), md);
        await File.WriteAllTextAsync(
            Path.Combine(FindRepoRoot(), "tools", "paspoort-compact-mockups", "above-the-fold-coverage-r3.md"),
            md);
    }

    [Fact]
    public async Task Capture_real_paspoort_compact_layout_variants_r2()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var tableRows = new List<string>();
        foreach (var layout in new[] { "a", "b", "c" })
        {
            foreach (var (width, height, tag) in Viewports())
            {
                tableRows.Add(await CaptureAsync(layout, width, height, tag, revision: "r2"));
            }
        }

        var md = BuildMarkdown(tableRows, revision: "r2", title: "Paspoort compact mockups r2 (real Blazor, NL)");
        await File.WriteAllTextAsync(Path.Combine(ArtifactsDir, "above-the-fold-coverage-r2.md"), md);
        await File.WriteAllTextAsync(
            Path.Combine(FindRepoRoot(), "tools", "paspoort-compact-mockups", "above-the-fold-coverage-r2.md"),
            md);
    }

    private static IEnumerable<(int width, int height, string tag)> Viewports()
    {
        yield return (1440, 900, "1440x900");
        yield return (1366, 768, "1366x768");
        yield return (1920, 1080, "1920x1080");
        yield return (390, 844, "390-mobile");
    }

    private async Task<string> CaptureAsync(string layout, int width, int height, string tag, string revision = "r2")
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        var returnUrl =
            $"/candidate/paspoort?compactLayout={layout}&encouragePreview=wist-tests-match";
        await page.GotoAsync(
            $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(returnUrl)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        await page.WaitForSelectorAsync(".passport-page", new() { State = WaitForSelectorState.Visible });
        await page.WaitForSelectorAsync(".passport-overview__title", new() { State = WaitForSelectorState.Visible });
        await page.WaitForSelectorAsync("[data-encourage-tip='wist-tests-match']", new() { State = WaitForSelectorState.Visible });
        await Assertions.Expect(page.Locator(".passport-overview__title")).ToHaveTextAsync("Dit ben jij");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var fileName = $"variant-{layout}-{tag}-{revision}.png";
        await page.ScreenshotAsync(new() { Path = Path.Combine(ArtifactsDir, fileName), FullPage = false });

        var foldJson = await page.EvaluateAsync<string>(
            """
            (foldY) => {
              const keys = [
                ["passport-card", ".passport-card"],
                ["overview", ".passport-overview"],
                ["nudge", ".profile-encourage"],
                ["tabs", ".passport-tabs"],
                ["tab-content", ".passport-panel:not([hidden])"]
              ];
              const blocks = {};
              for (const [key, sel] of keys) {
                const el = document.querySelector(sel);
                if (!el) {
                  blocks[key] = { visible: false, pct: 0, top: 0, bottom: 0 };
                  continue;
                }
                const r = el.getBoundingClientRect();
                const visibleTop = Math.max(0, Math.min(r.bottom, foldY) - Math.max(r.top, 0));
                blocks[key] = {
                  visible: r.top < foldY && r.bottom > 0,
                  pct: r.height > 0 ? Math.round((visibleTop / r.height) * 100) : 0,
                  top: Math.round(r.top),
                  bottom: Math.round(r.bottom)
                };
              }
              const lowest = Object.values(blocks).filter(b => b.visible).reduce((m, b) => Math.max(m, b.bottom), 0);
              return JSON.stringify({ blocks, lowest, spare: Math.round(foldY - lowest) });
            }
            """,
            height);

        var fold = JsonSerializer.Deserialize<FoldPayload>(foldJson, Json)!;
        return FormatRow(layout.ToUpperInvariant(), tag, fold, fileName);
    }

    private async Task SaveMarginCropsAsync(int width, int height, string revision = "r4")
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        var returnUrl = "/candidate/paspoort?compactLayout=c&encouragePreview=wist-tests-match";
        await page.GotoAsync(
            $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(returnUrl)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForSelectorAsync(".passport-margin-decor--left img", new() { State = WaitForSelectorState.Visible });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        foreach (var side in new[] { "left", "right" })
        {
            var locator = page.Locator($".passport-margin-decor--{side} img");
            if (await locator.CountAsync() == 0)
            {
                continue;
            }

            await locator.ScreenshotAsync(new()
            {
                Path = Path.Combine(ArtifactsDir, $"variant-c-{width}x{height}-margin-{side}-{revision}.png")
            });
        }
    }

    private static string FormatRow(string variant, string viewport, FoldPayload fold, string file)
    {
        string Cell(string key)
        {
            if (!fold.Blocks.TryGetValue(key, out var b))
            {
                return "hidden";
            }

            if (!b.Visible)
            {
                return "hidden";
            }

            return b.Pct >= 98 ? "full" : $"{b.Pct}% visible (top {b.Top}px)";
        }

        return $"| {variant} | {viewport} | {Cell("passport-card")} | {Cell("overview")} | {Cell("nudge")} | {Cell("tabs")} | {Cell("tab-content")} | {fold.Lowest}px | {fold.Spare}px | `{file}` |";
    }

    private static string BuildMarkdown(List<string> rows, string revision, string title)
    {
        var header =
            "| Variant | Viewport | Passport | Dit ben jij | Wist je dat… nudge | Tabs | Tab content | Lowest bottom | Spare px | File |\n" +
            "| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |\n";
        return $"""
            # {title}

            Fake candidate **Alex Demo**. Viewport height = fold line (browser chrome excluded). Components: real header, `PassportCard`, `PassportOverview`, `ProfileEncouragementNudge` (wist-tests-match). Revision **{revision}**.

            """ + header + string.Join('\n', rows) + "\n";
    }

    internal static CandidateKompasState BuildDemoKompas()
    {
        var profileId = Guid.Parse("d2a20000-0000-0000-0000-000000000099");
        return new CandidateKompasState
        {
            Profile = new MeProfile
            {
                Id = profileId,
                Email = "alex.demo@lobsy.local",
                FullName = "Alex Demo",
                FirstName = "Alex",
                LastName = "Demo",
                Role = "Candidate",
                OpenForWork = true,
                HomeLatitude = 51.994,
                HomeLongitude = 4.205,
                Preferences = new CandidatePreferences
                {
                    HomeAddress = "Naaldwijk",
                    AboutMe = null,
                    Roles = [],
                    SpokenLanguages = []
                }
            },
            ProfileCompletenessPercent = 35,
            Competencies = DraftCompetence(),
            CareerInterests = DraftCareer(),
            Culture = DraftCulture(),
            Values = DraftValues()
        };
    }

    private static CandidateCompetencyState DraftCompetence()
        => new() { Status = "Draft", AnsweredCount = 0, QuestionCount = 25 };

    private static CandidateCareerInterestState DraftCareer()
        => new() { Status = "Draft", AnsweredCount = 0, QuestionCount = 25 };

    private static CandidateCultureState DraftCulture()
        => new() { Status = "Draft", Answers = [] };

    private static CandidateValuesState DraftValues()
        => new() { Status = "Draft", Answers = [] };

    private sealed class FoldPayload
    {
        public Dictionary<string, FoldBlock> Blocks { get; set; } = new();
        public int Lowest { get; set; }
        public int Spare { get; set; }
    }

    private sealed class FoldBlock
    {
        public bool Visible { get; set; }
        public int Pct { get; set; }
        public int Top { get; set; }
        public int Bottom { get; set; }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repo root not found.");
    }

    internal sealed class PaspoortCompactMockupWebFactory : WebWebApplicationFactory
    {
        public string ServerAddress { get; private set; } = "";
        private IHost? _kestrel;

        public PaspoortCompactMockupWebFactory()
        {
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
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
                    ["Jobsy:PassportCompactMockups:Enabled"] = "true"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.AddSingleton<IStartupFilter>(new CompactMockupSignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new CompactMockupFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new CompactStubFactory());
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new CompactStubHandler(), disposeHandler: false)
                    {
                        BaseAddress = new Uri("http://api.test/")
                    }));
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
            var addresses = _kestrel.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
            ServerAddress = addresses!.Addresses.First(a => a.StartsWith("http://", StringComparison.Ordinal));
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

    private sealed class CompactMockupSignInFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (ctx, nxt) =>
                {
                    if (!ctx.Request.Path.Equals("/__test/sign-in", StringComparison.OrdinalIgnoreCase))
                    {
                        await nxt();
                        return;
                    }

                    var identity = new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.Parse("d2a20000-0000-0000-0000-000000000099").ToString()),
                        new Claim(ClaimTypes.Role, "Candidate"),
                        new Claim(ClaimTypes.Name, "Alex Demo")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/paspoort?compactLayout=a";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class CompactMockupFlags : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(
            EmployersEnabled: true,
            CandidatePassportEnabled: true,
            PassportPdfV2Enabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class CompactStubFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new CompactStubHandler(), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class CompactStubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var kompas = BuildDemoKompas();
            string json;
            if (path.Contains("/kompas/dna", StringComparison.OrdinalIgnoreCase))
            {
                json = JsonSerializer.Serialize(MapDna(kompas), Json);
            }
            else if (path.Contains("/kompas", StringComparison.OrdinalIgnoreCase))
            {
                json = JsonSerializer.Serialize(kompas, Json);
            }
            else if (path.Contains("/who-am-i", StringComparison.OrdinalIgnoreCase))
            {
                json = JsonSerializer.Serialize(new WhoAmIState { ProfileFilled = false }, Json);
            }
            else if (path.Contains("/values", StringComparison.OrdinalIgnoreCase))
            {
                json = JsonSerializer.Serialize(DraftValues(), Json);
            }
            else if (path.Contains("/applications", StringComparison.OrdinalIgnoreCase))
            {
                json = "[]";
            }
            else if (path.Contains("/notifications", StringComparison.OrdinalIgnoreCase))
            {
                json = "[]";
            }
            else if (path.Contains("/reference-verification", StringComparison.OrdinalIgnoreCase))
            {
                json = path.Contains("eligible", StringComparison.OrdinalIgnoreCase)
                    ? """{"eligible":false}"""
                    : "[]";
            }
            else if (path.Contains("/verified-competences", StringComparison.OrdinalIgnoreCase))
            {
                json = "[]";
            }
            else if (path.Contains("/site/status", StringComparison.OrdinalIgnoreCase))
            {
                json = """{"maintenance":false,"expectedEndUtc":null}""";
            }
            else
            {
                json = "{}";
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        private static CandidateDnaSummary MapDna(CandidateKompasState kompas)
            => new()
            {
                ProfileCompletenessPercent = kompas.ProfileCompletenessPercent,
                Competencies = new CandidateDnaCompetencySummary
                {
                    Status = kompas.Competencies?.Status ?? "Draft",
                    AnsweredCount = kompas.Competencies?.AnsweredCount ?? 0,
                    QuestionCount = kompas.Competencies?.QuestionCount ?? 25
                },
                CareerInterests = new CandidateDnaCareerSummary
                {
                    Status = kompas.CareerInterests?.Status ?? "Draft",
                    AnsweredCount = kompas.CareerInterests?.AnsweredCount ?? 0,
                    QuestionCount = kompas.CareerInterests?.QuestionCount ?? 25
                },
                Culture = new CandidateDnaCultureSummary
                {
                    Status = kompas.Culture?.Status ?? "Draft",
                    AnsweredCount = kompas.Culture?.Answers?.Count ?? 0,
                    QuestionCount = 18
                },
                Values = new CandidateDnaValuesSummary
                {
                    Status = kompas.Values?.Status ?? "Draft",
                    AnsweredCount = kompas.Values?.Answers?.Count ?? 0,
                    QuestionCount = 25
                }
            };
    }
}
