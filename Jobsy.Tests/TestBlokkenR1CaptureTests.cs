using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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

/// <summary>
/// Real UI captures for Dennis review. Run: JOBSY_CAPTURE_TESTBLOKKEN=1 dotnet test --filter FullyQualifiedName~TestBlokkenR1CaptureTests
/// </summary>
[Collection("DeepPlaywrightSerial")]
public sealed class TestBlokkenR1CaptureTests : IAsyncLifetime
{
    public const string OutputDir = "/opt/cursor/artifacts/testblokken-r1";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(OutputDir);
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = ["--disable-dev-shm-usage", "--no-sandbox"]
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    private sealed record CaptureTarget(string FileStem, string ReturnPath, AssessmentKind Kind, int Answered, bool PaidDeep, string ExpectedBlock);

    private static readonly CaptureTarget[] Targets =
    [
        new("quick-scan-hoe-je-werkt", "/__test/capture-test-block?kind=competence&variant=quick", AssessmentKind.Competence, 6, false, "hoe-je-werkt"),
        new("quick-scan-leuk", "/__test/capture-test-block?kind=career&variant=quick", AssessmentKind.Career, 6, false, "leuk"),
        new("quick-scan-sfeer", "/__test/capture-test-block?kind=culture&variant=quick", AssessmentKind.Culture, 6, false, "sfeer"),
        new("quick-scan-waarden", "/__test/capture-test-block?kind=values&variant=quick", AssessmentKind.Values, 6, false, "waarden"),
        new("verdieping-leuk", "/__test/capture-test-block?kind=career&variant=verdieping", AssessmentKind.Career, 12, false, "leuk"),
        new("diepte-competenties", "/__test/capture-test-block?kind=competence&variant=deep", AssessmentKind.Competence, 35, true, "hoe-je-werkt")
    ];

    [Fact]
    public async Task Capture_testblokken_r1_desktop_and_mobile()
    {
        var capture = (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_TESTBLOKKEN") ?? "").Trim() == "1";
        if (!capture)
        {
            return;
        }

        PurgeLegacyArtifactDirs();

        var onlyStem = (Environment.GetEnvironmentVariable("JOBSY_TESTBLOKKEN_STEM") ?? "").Trim();
        var targets = Targets;
        if (!string.IsNullOrEmpty(onlyStem))
        {
            targets = targets.Where(t => t.FileStem.Equals(onlyStem, StringComparison.OrdinalIgnoreCase)).ToArray();
            Assert.NotEmpty(targets);
        }

        foreach (var target in targets)
        {
            await using var web = new CaptureWebFactory(target);
            var baseUrl = web.EnsureServerAddress().TrimEnd('/');

            foreach (var (width, height, tag) in new (int, int, string)[] { (1440, 900, "1440"), (390, 844, "390") })
            {
                await using var context = await _browser!.NewContextAsync(new()
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height },
                    Locale = "nl-NL",
                    IsMobile = width < 640,
                    HasTouch = width < 640,
                    IgnoreHTTPSErrors = true
                });
                await PlaywrightCookieConsent.AcceptAsync(context);
                await SignInViaHttpAsync(context, baseUrl, target.ReturnPath);

                var page = await context.NewPageAsync();
                page.SetDefaultTimeout(90_000);
                await page.GotoAsync(baseUrl + target.ReturnPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
                await PlaywrightCookieConsent.AcceptOnPageAsync(page);
                await page.Locator(".test-flow").WaitForAsync(new() { Timeout = 90_000 });

                var stage = page.Locator("[data-testid=test-block-stage]");
                await stage.WaitForAsync();
                Assert.Equal(target.ExpectedBlock, await stage.GetAttributeAsync("data-block"));

                await AssertInsightVisibleAsync(page, width);
                await AssertQualityAsync(page, width);

                var path = Path.Combine(OutputDir, $"{target.FileStem}-{tag}.png");
                await page.ScreenshotAsync(new() { Path = path, FullPage = false });
                Assert.True(File.Exists(path) && new FileInfo(path).Length > 10_000, path);
            }
        }

        if (string.IsNullOrEmpty(onlyStem))
        {
            var pngCount = Directory.GetFiles(OutputDir, "*.png").Length;
            Assert.Equal(12, pngCount);
        }
    }

    private static void PurgeLegacyArtifactDirs()
    {
        foreach (var dir in new[]
                 {
                     "/opt/cursor/artifacts/screenshots",
                     "/cursor/stores/self/test-blocks",
                     "/cursor/stores/self/insight",
                     Path.Combine(FindRepoRoot(), "artifacts/test-blocks")
                 })
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(f);
                }
                catch
                {
                    // best effort
                }
            }
        }
    }

    private static async Task SignInViaHttpAsync(IBrowserContext context, string baseUrl, string returnPath)
    {
        var signInUri = new Uri($"{baseUrl}/__test/sign-in?return={Uri.EscapeDataString(returnPath)}");
        var jar = new CookieContainer();
        using var handler = new HttpClientHandler { CookieContainer = jar, AllowAutoRedirect = false };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        using var response = await http.GetAsync(signInUri, HttpCompletionOption.ResponseHeadersRead);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Sign-in expected redirect, got {(int)response.StatusCode}");

        var baseUri = new Uri(baseUrl);
        var cookies = jar.GetCookies(baseUri).Cast<System.Net.Cookie>()
            .Select(c => new Microsoft.Playwright.Cookie
            {
                Name = c.Name,
                Value = c.Value,
                Domain = baseUri.Host,
                Path = string.IsNullOrEmpty(c.Path) ? "/" : c.Path,
                HttpOnly = c.HttpOnly,
                Secure = c.Secure,
                SameSite = SameSiteAttribute.Lax
            })
            .ToArray();
        await context.AddCookiesAsync(cookies);
    }

    private static async Task AssertInsightVisibleAsync(IPage page, int width)
    {
        if (width >= 1024)
        {
            var panel = page.Locator("[data-testid=test-progress-insight-panel]");
            await panel.WaitForAsync(new() { Timeout = 60_000 });
            var title = await panel.Locator(".test-progress-panel__title").InnerTextAsync();
            Assert.Contains("beeld", title, StringComparison.OrdinalIgnoreCase);
            var listCount = await panel.Locator(".test-progress-panel__list li").CountAsync();
            Assert.True(listCount >= 1, "Expected at least one insight line after one block.");
        }
        else
        {
            var mobile = page.Locator("[data-testid=test-progress-insight-mobile]");
            await mobile.WaitForAsync(new() { Timeout = 60_000 });
            var text = await mobile.InnerTextAsync();
            Assert.Contains("beeld", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task AssertQualityAsync(IPage page, int width)
    {
        var insightText = width >= 1024
            ? await page.Locator("[data-testid=test-progress-insight-panel]").InnerTextAsync()
            : await page.Locator("[data-testid=test-progress-insight-mobile]").InnerTextAsync();
        Assert.False(Regex.IsMatch(insightText, @"\d|%"), $"Insight text must not contain digits or %: {insightText}");

        var cardBox = await page.Locator(".test-shell__card, .ob-wizard__card").First.BoundingBoxAsync();
        var bandBox = await page.Locator(".test-block-stage__band").BoundingBoxAsync();
        Assert.NotNull(cardBox);
        Assert.NotNull(bandBox);
        Assert.True(cardBox!.Y + cardBox.Height <= bandBox!.Y + 2,
            "Question card must sit above the art band (not overlapping).");
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

    private sealed class CaptureWebFactory(CaptureTarget target) : WebWebApplicationFactory
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
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
                    ["E2E:SkipHttpsRedirect"] = "true"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new CaptureSignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new CaptureStubFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new CaptureStubFactory(target));
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new CaptureStubHandler(target), disposeHandler: false)
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

    private sealed class CaptureStubFlags : IFeatureFlags
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

    private sealed class CaptureSignInFilter : IStartupFilter
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
                        new Claim(ClaimTypes.Name, "Testblokken Capture")
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

    private sealed class CaptureStubFactory(CaptureTarget target) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new CaptureStubHandler(target), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class CaptureStubHandler(CaptureTarget target) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            object payload = path switch
            {
                var p when p.Contains("/profile", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/test-ai-consent", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/competencies", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Competence, target),
                var p when p.Contains("/career-interests", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Career, target),
                var p when p.Contains("/culture", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Culture, target),
                var p when p.Contains("/values", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Values, target),
                var p when p.Contains("/deep-analysis/payment-mode", StringComparison.OrdinalIgnoreCase) => new { mode = "stub" },
                var p when p.Contains("/deep-analysis", StringComparison.OrdinalIgnoreCase) => BuildDeep(target),
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
                FullName = "Testblokken Capture",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                TestAiConsentAt = DateTime.UtcNow.AddDays(-1),
                TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
            };

        private static object BuildQuickScan(AssessmentKind kind, CaptureTarget target)
        {
            if (target.PaidDeep || target.Kind != kind)
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
                    Answers = BuildQuickScanAnswers(kind, target.Answered)
                },
                AssessmentKind.Culture => new CandidateCultureState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    Answers = BuildQuickScanAnswers(kind, target.Answered)
                },
                AssessmentKind.Values => new CandidateValuesState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    Answers = BuildQuickScanAnswers(kind, target.Answered)
                },
                _ => new CandidateCompetencyState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    QuestionCount = 25,
                    IsIndicativeDepth = true,
                    Answers = BuildQuickScanAnswers(kind, target.Answered)
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

        private static DeepAnalysisState BuildDeep(CaptureTarget target)
        {
            var answers = target.PaidDeep
                ? BuildDeepAnswers(target.Kind, target.Answered)
                : [];
            return new()
            {
                Kind = AssessmentKindLabels.ToSlug(target.Kind),
                IsUnlocked = target.PaidDeep,
                IsCompleted = false,
                QuestionCount = TestDepthRules.Bottom(target.Kind),
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
