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
public sealed class ProgressInsightEvidencePlaywrightTests : IAsyncLifetime
{
    private const string ArtifactDir = "artifacts/insight";
    private const string AgentStoreDir = "/cursor/stores/self/insight";

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

    [Fact]
    public async Task Progress_insight_card_after_answer_milestones()
    {
        foreach (var answered in new[] { 5, 10, 15 })
        {
            foreach (var (width, height, tag) in new (int, int, string)[]
                     {
                         (1440, 900, "desktop-1440"),
                         (390, 844, "mobile-390")
                     })
            {
                using var web = new InsightWebFactory(answered);
                var url = web.EnsureServerAddress()
                          + "/__test/sign-in?return="
                          + Uri.EscapeDataString("/candidate/competencies?stap=vraag");

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

                var insight = page.Locator("[data-testid=test-progress-insight]");
                await insight.WaitForAsync();
                var title = await insight.Locator(".test-progress-insight__title").InnerTextAsync();
                Assert.Contains("beeld", title, StringComparison.OrdinalIgnoreCase);

                if (width < 640)
                {
                    var collapsed = await page.EvaluateAsync<bool>(
                        """
                        () => {
                          const d = document.querySelector('[data-testid=test-progress-insight]');
                          const body = d?.querySelector('.test-progress-insight__body');
                          if (!d || !body) return false;
                          return !d.open && getComputedStyle(body).display === 'none';
                        }
                        """);
                    Assert.True(collapsed);

                    var statementVisible = await page.EvaluateAsync<bool>(
                        """
                        () => {
                          const el = document.querySelector('.test-flow__statement');
                          if (!el) return false;
                          const r = el.getBoundingClientRect();
                          return r.top >= 0 && r.top < window.innerHeight * 0.85;
                        }
                        """);
                    Assert.True(statementVisible);

                    var coachHidden = await page.EvaluateAsync<bool>(
                        "() => !document.querySelector('.lobsy-coach-dock') || getComputedStyle(document.querySelector('.lobsy-coach-dock')).display === 'none'");
                    Assert.True(coachHidden);
                }
                else
                {
                    var listCount = await insight.Locator(".test-progress-insight__list li").CountAsync();
                    Assert.True(listCount >= 1);
                }

                var fileName = $"competence-insight-after-{answered}-answers-{tag}.png";
                var artifactPath = Path.Combine(FindRepoRoot(), ArtifactDir, fileName);
                await page.ScreenshotAsync(new() { Path = artifactPath, FullPage = true });
                File.Copy(artifactPath, Path.Combine(AgentStoreDir, fileName), overwrite: true);
            }
        }
    }

    [Theory]
    [InlineData(1440, 900, "desktop-1440")]
    [InlineData(390, 844, "mobile-390")]
    public async Task Progress_insight_shows_motivation_cue_at_block_boundary(int width, int height, string tag)
    {
        using var web = new InsightWebFactory(answered: 12);
        var url = web.EnsureServerAddress()
                  + "/__test/sign-in?return="
                  + Uri.EscapeDataString("/candidate/competencies?stap=vraag");

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        await page.EvaluateAsync(
            "() => { const d = document.querySelector('[data-testid=test-progress-insight]'); if (d) d.open = true; }");

        var motivation = page.Locator("[data-testid=test-progress-motivation]");
        await motivation.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var text = await motivation.InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(text));

        var fileName = $"competence-insight-motivation-{tag}.png";
        var artifactPath = Path.Combine(FindRepoRoot(), ArtifactDir, fileName);
        await page.ScreenshotAsync(new() { Path = artifactPath, FullPage = true });
        File.Copy(artifactPath, Path.Combine(AgentStoreDir, fileName), overwrite: true);
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

    private sealed class InsightWebFactory(int answered) : WebWebApplicationFactory
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
                services.AddSingleton<IStartupFilter>(new InsightSignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new InsightStubFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new InsightStubFactory(answered));
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new InsightStubHandler(answered), disposeHandler: false)
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

    private sealed class InsightStubFlags : IFeatureFlags
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

    private sealed class InsightSignInFilter : IStartupFilter
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
                        new Claim(ClaimTypes.Name, "Insight Demo")
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

    private sealed class InsightStubFactory(int answered) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new InsightStubHandler(answered), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class InsightStubHandler(int answered) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            object payload = path switch
            {
                var p when p.Contains("/profile", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/competencies", StringComparison.OrdinalIgnoreCase) => BuildCompetency(answered),
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
                FullName = "Insight Demo",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                TestAiConsentAt = DateTime.UtcNow.AddDays(-1),
                TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
            };

        private static CandidateCompetencyState BuildCompetency(int answeredCount)
        {
            var ids = TestDepthRules.QuestionIdsFor(
                    AssessmentKind.Competence,
                    TestDepthLevel.Full,
                    AssessmentTestVersions.V3)
                .Take(Math.Clamp(answeredCount, 0, 25))
                .ToList();
            return new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V3,
                QuickScanFullCount = 25,
                QuestionCount = 25,
                IsIndicativeDepth = true,
                Answers = ids.ToDictionary(
                    id => id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    id => 3 + (id % 2))
            };
        }
    }
}
