using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Privacy;
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
/// Quick-Scan v2 UI contract: transparency block and version-aware test shells (desktop + mobile screenshots).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class TestsV2PlaywrightTests : IAsyncLifetime
{
    private const string ScreenshotDir = "artifacts/tests-v2-playwright";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(Path.Combine(FindRepoRoot(), ScreenshotDir));
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

    [Fact]
    public void Candidate_test_pages_are_quick_scan_version_aware()
    {
        var root = FindRepoRoot();
        foreach (var rel in new[]
                 {
                     "Jobsy.Web/Components/Pages/Candidate/CompetencyTest.razor",
                     "Jobsy.Web/Components/Pages/Candidate/CareerTest.razor",
                     "Jobsy.Web/Components/Pages/Candidate/CultureScan.razor",
                     "Jobsy.Web/Components/Pages/Candidate/ValuesScan.razor"
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, rel));
            Assert.Contains("_quickScanVersion", text, StringComparison.Ordinal);
            Assert.Contains("AssessmentQuickScanCatalog", text, StringComparison.Ordinal);
        }

        var gate = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/Tests/TestConsentGate.razor"));
        Assert.Contains("data-testid=\"assessment-v2-transparency\"", gate, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("desktop", 1440, 900, "nl-NL")]
    [InlineData("mobile", 390, 844, "nl-NL")]
    [InlineData("desktop", 1440, 900, "pl-PL")]
    [InlineData("mobile", 390, 844, "ar")]
    public async Task V2_competency_intro_shows_transparency_and_depth_pick(
        string name, int width, int height, string locale)
    {
        using var web = new TestsV2WebFactory(testsV2On: true);
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString("/candidate/competencies?stap=intro");

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            Locale = locale
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(45_000);
        await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var transparency = page.Locator("[data-testid=assessment-v2-transparency]");
        await transparency.WaitForAsync();
        Assert.Contains("Quick-Scan (v2)", await transparency.InnerTextAsync(), StringComparison.OrdinalIgnoreCase);

        var pick = page.Locator(".test-depth-pick");
        await pick.WaitForAsync();
        Assert.True(await pick.IsVisibleAsync());

        var overflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        Assert.False(overflow);

        var indicative = page.Locator("[data-testid=assessment-indicative-depth]");
        await indicative.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(FindRepoRoot(), ScreenshotDir, $"competency-v2-intro-{locale}-{name}.png"),
            FullPage = true
        });

        await page.Locator(".test-shell__footer button.btn-primary").ClickAsync();
        await page.Locator(".test-shell[data-mode=Question]").WaitForAsync(new() { Timeout = 45_000 });
        await indicative.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var body = await page.ContentAsync();
        Assert.DoesNotContain("AssessmentV2.Competency.Q", body, StringComparison.Ordinal);
        if (locale.StartsWith("nl", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains("samen", body, StringComparison.OrdinalIgnoreCase);
        }

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(FindRepoRoot(), ScreenshotDir, $"competency-v2-question-{locale}-{name}.png"),
            FullPage = true
        });
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

    private sealed class TestsV2WebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        private IHost? _kestrel;
        private readonly bool _testsV2On;

        public string ServerAddress { get; private set; } = "";

        public TestsV2WebFactory(bool testsV2On)
        {
            _testsV2On = testsV2On;
        }

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
                services.AddSingleton<IStartupFilter>(new SignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new StubFlags(_testsV2On));
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubFactory());
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new StubHandler(), disposeHandler: false)
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

    private sealed class StubFlags(bool testsV2On) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: false,
                CandidatePassportEnabled: true,
                TestsV2Enabled: testsV2On));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class SignInFilter : IStartupFilter
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
                        new Claim(ClaimTypes.Name, "Tests V2 Demo")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/competencies?stap=intro";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class StubFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler(), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            object payload = path switch
            {
                var p when p.Contains("/profile", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/competencies", StringComparison.OrdinalIgnoreCase) => BuildCompetency(),
                var p when p.Contains("/career-interests", StringComparison.OrdinalIgnoreCase) => BuildCareer(),
                var p when p.Contains("/culture", StringComparison.OrdinalIgnoreCase) => BuildCulture(),
                var p when p.Contains("/values", StringComparison.OrdinalIgnoreCase) => BuildValues(),
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
                FullName = "Tests V2 Demo",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                TestAiConsentAt = DateTime.UtcNow.AddDays(-1),
                TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
            };

        private static CandidateCompetencyState BuildCompetency()
            => new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V2,
                QuickScanFullCount = 40,
                QuestionCount = 40,
                IsIndicativeDepth = true,
                Answers = new Dictionary<string, int> { ["1"] = 4, ["2"] = 3 }
            };

        private static CandidateCareerInterestState BuildCareer()
            => new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V2,
                QuickScanFullCount = 48,
                QuestionCount = 48,
                Answers = new Dictionary<string, int>()
            };

        private static CandidateCultureState BuildCulture()
            => new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V2,
                QuickScanFullCount = 48,
                Answers = new Dictionary<string, int>()
            };

        private static CandidateValuesState BuildValues()
            => new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V2,
                QuickScanFullCount = 40,
                Answers = new Dictionary<string, int>()
            };
    }
}
