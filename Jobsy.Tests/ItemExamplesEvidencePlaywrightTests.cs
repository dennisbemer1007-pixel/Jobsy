using System.Net;
using System.Net.Http.Json;
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

/// <summary>Agent-store evidence for TestItemExamples (Quick-Scan + one deep question).</summary>
[Collection("DeepPlaywrightSerial")]
public sealed class ItemExamplesEvidencePlaywrightTests : IAsyncLifetime
{
    private const string ArtifactDir = "artifacts/examples";
    private const string AgentStoreDir = "/cursor/stores/self/examples";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        var root = FindRepoRoot();
        Directory.CreateDirectory(Path.Combine(root, ArtifactDir));
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

    [Fact]
    public void Export_all_nl_example_sentences_for_tone_review()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, ArtifactDir, "all-examples-nl.txt");
        var lines = new List<string> { "# NL item examples (TestItemExamples catalog)", "" };

        void AddKind(AssessmentKind kind, IEnumerable<(int Id, string Domain, bool Reverse)> items)
        {
            foreach (var (id, domain, reverse) in items.OrderBy(i => i.Id))
            {
                var text = AssessmentItemExamples.ExampleNl(kind, id, domain, reverse);
                lines.Add($"{kind}\t{id}\t{text}");
            }

            lines.Add("");
        }

        AddKind(AssessmentKind.Competence,
            CompetencyTestCatalogV2.Questions.Select(q => (q.Id, q.Category, q.Reverse)));
        AddKind(AssessmentKind.Career,
            CareerTestCatalogV2.Questions.Select(q => (q.Id, q.Category, q.Reverse)));
        AddKind(AssessmentKind.Culture,
            CulturePersonalityCatalogV2.Questions.Select(q => (q.Id, q.Category, q.Reverse)));
        AddKind(AssessmentKind.Values,
            SchwartzValuesCatalogV2.Questions.Select(q => (q.Id, q.Category, q.Reverse)));

        foreach (var kind in new[] { AssessmentKind.Competence, AssessmentKind.Career, AssessmentKind.Culture, AssessmentKind.Values })
        {
            AddKind(kind, DeepAnalysisCatalog.QuestionsFor(kind).Select(q => (q.Id, q.Domain, q.Reverse)));
        }

        File.WriteAllLines(path, lines);
        CopyToAgentStore(path, "all-examples-nl.txt");
        Assert.True(new FileInfo(path).Length > 500);
    }

    [Fact]
    public async Task Quick_scan_questions_show_calm_example_lines()
    {
        using var web = new ExamplesWebFactory();
        var baseUrl = web.EnsureServerAddress() + "/__test/sign-in?return=";

        foreach (var (path, slug) in new (string, string)[]
                 {
                     ("/candidate/competencies?stap=vraag", "competence"),
                     ("/candidate/career?stap=vraag", "career"),
                     ("/candidate/culture?stap=vraag", "culture"),
                     ("/candidate/values?stap=vraag", "values")
                 })
        {
            foreach (var (width, height, tag) in new (int, int, string)[]
                     {
                         (1440, 900, "desktop-1440"),
                         (390, 844, "mobile-390")
                     })
            {
                var url = baseUrl + Uri.EscapeDataString(path);
                await using var context = await _browser!.NewContextAsync(new()
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height },
                    Locale = "nl-NL"
                });
                await PlaywrightCookieConsent.AcceptAsync(context);
                var page = await context.NewPageAsync();
                page.SetDefaultTimeout(45_000);
                await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
                await PlaywrightCookieConsent.AcceptOnPageAsync(page);

                var example = page.Locator("[data-testid=test-item-example]");
                await example.WaitForAsync();
                var text = await example.InnerTextAsync();
                Assert.Contains("Voorbeeld", text, StringComparison.OrdinalIgnoreCase);

                var fileName = $"{slug}-question-{tag}.png";
                var artifactPath = Path.Combine(FindRepoRoot(), ArtifactDir, fileName);
                await page.ScreenshotAsync(new() { Path = artifactPath, FullPage = true });
                CopyToAgentStore(artifactPath, fileName);
            }
        }
    }

    [Fact]
    public async Task Deep_analysis_question_shows_example_line()
    {
        using var web = new DeepExamplesWebFactory();
        var url = web.EnsureServerAddress()
                  + "/__test/sign-in?return="
                  + Uri.EscapeDataString("/candidate/deep-analysis/competence");

        foreach (var (width, height, tag) in new (int, int, string)[]
                 {
                     (1440, 900, "desktop-1440"),
                     (390, 844, "mobile-390")
                 })
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
                Locale = "nl-NL"
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(45_000);
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await page.Locator(".test-shell[data-mode=Question]").WaitForAsync(new() { Timeout = 60_000 });

            var example = page.Locator("[data-testid=test-item-example]");
            await example.WaitForAsync();
            var text = await example.InnerTextAsync();
            Assert.Contains("Voorbeeld", text, StringComparison.OrdinalIgnoreCase);

            var fileName = $"deep-competence-question-{tag}.png";
            var artifactPath = Path.Combine(FindRepoRoot(), ArtifactDir, fileName);
            await page.ScreenshotAsync(new() { Path = artifactPath, FullPage = true });
            CopyToAgentStore(artifactPath, fileName);
        }
    }

    private static void CopyToAgentStore(string sourcePath, string fileName)
    {
        var dest = Path.Combine(AgentStoreDir, fileName);
        File.Copy(sourcePath, dest, overwrite: true);
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

    private sealed class ExamplesWebFactory : WebWebApplicationFactory
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
                services.AddSingleton<IStartupFilter>(new QuickScanSignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new ExamplesStubFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new QuickScanStubFactory());
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new QuickScanStubHandler(), disposeHandler: false)
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

    private sealed class DeepExamplesWebFactory : WebWebApplicationFactory
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
                services.AddSingleton<IStartupFilter>(new QuickScanSignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new ExamplesStubFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new DeepStubFactory());
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new DeepStubHandler(), disposeHandler: false)
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

    private sealed class ExamplesStubFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: false,
                CandidatePassportEnabled: true,
                TestsV2Enabled: true,
                TestItemExamplesEnabled: true));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class QuickScanSignInFilter : IStartupFilter
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
                        new Claim(ClaimTypes.Name, "Examples Demo")
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

    private sealed class QuickScanStubFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new QuickScanStubHandler(), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class QuickScanStubHandler : HttpMessageHandler
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
                FullName = "Examples Demo",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                TestAiConsentAt = DateTime.UtcNow.AddDays(-1),
                TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
            };

        private static CandidateCompetencyState BuildCompetency()
            => new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V3,
                QuickScanFullCount = 25,
                QuestionCount = 25,
                IsIndicativeDepth = true,
                Answers = new Dictionary<string, int>()
            };

        private static CandidateCareerInterestState BuildCareer()
            => new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V3,
                QuickScanFullCount = 25,
                QuestionCount = 25,
                Answers = new Dictionary<string, int>()
            };

        private static CandidateCultureState BuildCulture()
            => new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V3,
                QuickScanFullCount = 25,
                Answers = new Dictionary<string, int>()
            };

        private static CandidateValuesState BuildValues()
            => new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V3,
                QuickScanFullCount = 25,
                Answers = new Dictionary<string, int>()
            };
    }

    private sealed class DeepStubFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new DeepStubHandler(), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class DeepStubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            object payload = path switch
            {
                var p when p.Contains("/api/me/profile", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/api/me/test-ai-consent", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/api/me/deep-analysis/payment-mode", StringComparison.OrdinalIgnoreCase) => new { mode = "stub" },
                var p when p.Contains("/api/me/deep-analysis", StringComparison.OrdinalIgnoreCase) => new DeepAnalysisState
                {
                    Kind = "Competence",
                    IsUnlocked = true,
                    IsCompleted = false,
                    QuestionCount = 150,
                    PriceEuro = 2.99m,
                    Answers = []
                },
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
                FullName = "Examples Demo",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                TestAiConsentAt = DateTime.UtcNow.AddDays(-1),
                TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
            };
    }
}
