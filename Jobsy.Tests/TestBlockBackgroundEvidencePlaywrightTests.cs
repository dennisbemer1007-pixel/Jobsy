using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
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

[Collection("DeepPlaywrightSerial")]
public sealed class TestBlockBackgroundEvidencePlaywrightTests : IAsyncLifetime
{
    private const string ArtifactDir = "artifacts/test-blocks";
    private const string AgentStoreDir = "/cursor/stores/self/test-blocks";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(Path.Combine(FindRepoRoot(), ArtifactDir));
        Directory.CreateDirectory(AgentStoreDir);
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    private sealed record BlockSurface(
        string ReturnPath,
        string Slug,
        AssessmentKind Kind,
        int Answered,
        bool PaidDeep,
        string ExpectedBlock);

    private static readonly BlockSurface[] Surfaces =
    [
        new("/candidate/competencies?stap=vraag", "quick-scan-hoe-je-werkt", AssessmentKind.Competence, 2, false, "hoe-je-werkt"),
        new("/candidate/career?stap=vraag", "quick-scan-leuk", AssessmentKind.Career, 2, false, "leuk"),
        new("/candidate/culture?stap=vraag", "quick-scan-sfeer", AssessmentKind.Culture, 2, false, "sfeer"),
        new("/candidate/values?stap=vraag", "quick-scan-waarden", AssessmentKind.Values, 2, false, "waarden"),
        new("/candidate/competencies?stap=vraag", "verdieping-hoe-je-werkt", AssessmentKind.Competence, 12, false, "hoe-je-werkt"),
        new("/candidate/career?stap=vraag", "verdieping-leuk", AssessmentKind.Career, 12, false, "leuk"),
        new("/candidate/culture?stap=vraag", "verdieping-sfeer", AssessmentKind.Culture, 12, false, "sfeer"),
        new("/candidate/values?stap=vraag", "verdieping-waarden", AssessmentKind.Values, 12, false, "waarden"),
        new("/candidate/deep-analysis/competence", "diepte-hoe-je-werkt", AssessmentKind.Competence, 8, true, "hoe-je-werkt"),
        new("/candidate/deep-analysis/career", "diepte-leuk", AssessmentKind.Career, 8, true, "leuk"),
        new("/candidate/deep-analysis/culture", "diepte-sfeer", AssessmentKind.Culture, 8, true, "sfeer"),
        new("/candidate/deep-analysis/values", "diepte-waarden", AssessmentKind.Values, 8, true, "waarden")
    ];

    [Fact]
    public async Task Block_background_band_on_all_test_surfaces_desktop_and_mobile()
    {
        var capture = (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_TEST_BLOCKS") ?? "").Trim() == "1";
        var only = (Environment.GetEnvironmentVariable("JOBSY_TEST_BLOCKS_ONLY") ?? "").Trim();
        var surfaces = Surfaces;
        if (!string.IsNullOrEmpty(only))
        {
            surfaces = surfaces.Where(s => s.Slug.Equals(only, StringComparison.OrdinalIgnoreCase)).ToArray();
            Assert.NotEmpty(surfaces);
        }

        foreach (var surface in surfaces)
        {

            foreach (var (width, height, tag) in new (int, int, string)[]
                     {
                         (1440, 900, "1440"),
                         (390, 844, "390")
                     })
            {
                using var web = new BlockWebFactory(surface);
                var signInUrl = web.EnsureServerAddress()
                              + "/__test/sign-in?return="
                              + Uri.EscapeDataString(surface.ReturnPath);

                await using var context = await _browser!.NewContextAsync(new()
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height },
                    Locale = "nl-NL",
                    IsMobile = width < 640,
                    HasTouch = width < 640
                });
                await PlaywrightCookieConsent.AcceptAsync(context);
                var page = await context.NewPageAsync();
                page.SetDefaultTimeout(45_000);
                await page.GotoAsync(signInUrl, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
                await PlaywrightCookieConsent.AcceptOnPageAsync(page);
                await page.Locator(".test-flow").WaitForAsync(new() { Timeout = 60_000 });

                var stage = page.Locator("[data-testid=test-block-stage]");
                await stage.WaitForAsync();
                Assert.Equal(surface.ExpectedBlock, await stage.GetAttributeAsync("data-block"));
                Assert.Equal(1, await stage.CountAsync());
                await page.Locator(".test-block-stage__band").WaitForAsync();
                await page.Locator(".test-shell__card .test-flow__statement").WaitForAsync();

                if (capture)
                {
                    var fileName = $"{surface.Slug}-{tag}.png";
                    var artifactPath = Path.Combine(FindRepoRoot(), ArtifactDir, fileName);
                    await page.ScreenshotAsync(new() { Path = artifactPath, FullPage = true });
                    File.Copy(artifactPath, Path.Combine(AgentStoreDir, fileName), overwrite: true);
                }
            }
        }
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        return Directory.GetCurrentDirectory();
    }

    private sealed class BlockWebFactory(BlockSurface surface) : WebWebApplicationFactory
    {
        private IHost? _kestrel;
        public string ServerAddress { get; private set; } = "";

        public string EnsureServerAddress()
        {
            if (string.IsNullOrEmpty(ServerAddress))
            {
                _ = CreateClient();
            }

            return ServerAddress;
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
                services.AddSingleton<IStartupFilter>(new BlockSignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new BlockStubFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new BlockStubFactory(surface));
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new BlockStubHandler(surface), disposeHandler: false)
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

    private sealed class BlockStubFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: false,
                CandidatePassportEnabled: true,
                TestsV2Enabled: true,
                TestItemExamplesEnabled: false,
                TestProgressInsightEnabled: true));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class BlockSignInFilter : IStartupFilter
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
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Candidate"),
                        new Claim(ClaimTypes.Name, "Block Art Demo")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/competencies?stap=vraag";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class BlockStubFactory(BlockSurface surface) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new BlockStubHandler(surface), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class BlockStubHandler(BlockSurface surface) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            object payload = path switch
            {
                var p when p.Contains("/profile", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/test-ai-consent", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/competencies", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Competence),
                var p when p.Contains("/career-interests", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Career),
                var p when p.Contains("/culture", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Culture),
                var p when p.Contains("/values", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Values),
                var p when p.Contains("/deep-analysis/payment-mode", StringComparison.OrdinalIgnoreCase) => new { mode = "stub" },
                var p when p.Contains("/deep-analysis", StringComparison.OrdinalIgnoreCase) => BuildDeep(),
                var p when p.Contains("/site/status", StringComparison.OrdinalIgnoreCase) => new { maintenance = false },
                _ => new { }
            };

            var json = JsonSerializer.Serialize(payload, Json);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        private static MeProfile BuildProfile()
            => new()
            {
                FullName = "Block Art Demo",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                TestAiConsentAt = DateTime.UtcNow.AddDays(-1),
                TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
            };

        private object BuildQuickScan(AssessmentKind kind)
        {
            if (surface.PaidDeep || surface.Kind != kind)
            {
                return EmptyQuickScan(kind);
            }

            return kind switch
            {
                AssessmentKind.Career => new CandidateCareerInterestState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    QuestionCount = 25,
                    Answers = BuildQuickScanAnswers(kind, surface.Answered)
                },
                AssessmentKind.Culture => new CandidateCultureState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    Answers = BuildQuickScanAnswers(kind, surface.Answered)
                },
                AssessmentKind.Values => new CandidateValuesState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    Answers = BuildQuickScanAnswers(kind, surface.Answered)
                },
                _ => new CandidateCompetencyState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    QuestionCount = 25,
                    IsIndicativeDepth = true,
                    Answers = BuildQuickScanAnswers(kind, surface.Answered)
                }
            };
        }

        private static object EmptyQuickScan(AssessmentKind kind)
            => kind switch
            {
                AssessmentKind.Career => new CandidateCareerInterestState { Status = "Draft", QuickScanVersion = AssessmentTestVersions.V3, Answers = [] },
                AssessmentKind.Culture => new CandidateCultureState { Status = "Draft", QuickScanVersion = AssessmentTestVersions.V3, Answers = [] },
                AssessmentKind.Values => new CandidateValuesState { Status = "Draft", QuickScanVersion = AssessmentTestVersions.V3, Answers = [] },
                _ => new CandidateCompetencyState { Status = "Draft", QuickScanVersion = AssessmentTestVersions.V3, Answers = [] }
            };

        private static Dictionary<string, int> BuildQuickScanAnswers(AssessmentKind kind, int answeredCount)
        {
            var ids = TestDepthRules.QuestionIdsFor(
                    kind,
                    TestDepthLevel.Full,
                    AssessmentTestVersions.V3)
                .Take(Math.Clamp(answeredCount, 0, 25));
            return ids.ToDictionary(
                id => id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                id => 3 + (id % 2));
        }

        private DeepAnalysisState BuildDeep()
        {
            var answers = surface.PaidDeep
                ? BuildDeepAnswers(surface.Kind, surface.Answered)
                : [];
            return new()
            {
                Kind = AssessmentKindLabels.ToSlug(surface.Kind),
                IsUnlocked = surface.PaidDeep,
                IsCompleted = false,
                QuestionCount = TestDepthRules.Bottom(surface.Kind),
                PriceEuro = 2.99m,
                Answers = answers
            };
        }

        private static Dictionary<string, int> BuildDeepAnswers(AssessmentKind kind, int answeredCount)
        {
            var ids = TestDepthRules.Parts(kind)
                .SelectMany(p => p.QuestionIds)
                .Take(Math.Clamp(answeredCount, 0, TestDepthRules.Bottom(kind)));
            return ids.ToDictionary(
                id => id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                id => 3 + (id % 2));
        }
    }
}
