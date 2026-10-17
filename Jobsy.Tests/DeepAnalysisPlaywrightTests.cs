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

[CollectionDefinition("DeepPlaywrightSerial", DisableParallelization = true)]
public sealed class DeepPlaywrightSerialCollection;

[Collection("DeepPlaywrightSerial")]
public sealed class DeepAnalysisPlaywrightTests : IAsyncLifetime
{
    private const string ScreenshotDir = "artifacts/deep-analysis-ui";

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

    [Theory]
    [InlineData("desktop", 1280, 800)]
    [InlineData("mobile", 390, 844)]
    public async Task Deep_offer_is_readable_with_progress_hint(string tag, int width, int height)
    {
        using var web = new DeepWebFactory(unlocked: false);
        var url = SignInUrl(web, "/candidate/deep-analysis/competence");
        await using var session = await OpenPageAsync(width, height, url);
        var page = session.Page;
        await AcceptConsentIfNeededAsync(page);
        await page.Locator(".deep-offer, .test-shell[data-mode=Offer]").First.WaitForAsync(new() { Timeout = 45_000 });
        var body = await page.Locator(".test-shell").InnerTextAsync();
        Assert.Contains("bodem", body, StringComparison.OrdinalIgnoreCase);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(FindRepoRoot(), ScreenshotDir, $"deep-competence-offer-{tag}.png"),
            FullPage = true
        });
    }

    [Fact]
    public async Task Deep_unlocked_question_shows_likert_desktop()
    {
        using var web = new DeepWebFactory(unlocked: true);
        var url = SignInUrl(web, "/candidate/deep-analysis/competence");
        await using var session = await OpenPageAsync(1280, 800, url);
        var page = session.Page;
        await AcceptConsentIfNeededAsync(page);
        await page.Locator(".test-shell[data-mode=Question]").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".test-likert").First.WaitForAsync(new() { Timeout = 30_000 });
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(FindRepoRoot(), ScreenshotDir, "deep-competence-question-desktop.png"),
            FullPage = true
        });
    }

    private async Task<PageSession> OpenPageAsync(int width, int height, string url)
    {
        var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(45_000);
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        return new PageSession(context, page);
    }

    private sealed class PageSession(IBrowserContext context, IPage page) : IAsyncDisposable
    {
        public IPage Page => page;

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
        }
    }

    private static async Task AcceptConsentIfNeededAsync(IPage page)
    {
        var btn = page.Locator("[data-consent=test] button.btn-primary");
        if (await btn.CountAsync() > 0)
        {
            await btn.ClickAsync();
            await page.WaitForTimeoutAsync(500);
        }
    }

    private static string SignInUrl(DeepWebFactory web, string returnPath)
        => web.EnsureServerAddress()
           + "/__test/sign-in?return="
           + Uri.EscapeDataString(returnPath);

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

    private sealed class DeepWebFactory(bool unlocked) : WebWebApplicationFactory
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
                services.AddSingleton<IStartupFilter>(new SignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new StubFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubFactory(unlocked));
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new StubHandler(unlocked), disposeHandler: false)
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

    private sealed class StubFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: false,
                CandidatePassportEnabled: true,
                TestsV2Enabled: true));

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
                        new Claim(ClaimTypes.Name, "Deep UI Demo")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/deep-analysis/competence";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class StubFactory(bool unlocked) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler(unlocked), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubHandler(bool unlocked) : HttpMessageHandler
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
                    IsUnlocked = unlocked,
                    IsCompleted = false,
                    QuestionCount = 150,
                    PriceEuro = 2.99m,
                    Answers = unlocked ? new Dictionary<string, int> { ["1"] = 4 } : []
                },
                var p when p.Contains("/site/status", StringComparison.OrdinalIgnoreCase) => new { maintenance = false },
                _ => new { }
            };

            if (request.Method == HttpMethod.Put || request.Method == HttpMethod.Post)
            {
                payload = new DeepAnalysisState
                {
                    Kind = "Competence",
                    IsUnlocked = true,
                    QuestionCount = 150,
                    Answers = new Dictionary<string, int> { ["1"] = 4 }
                };
            }

            var json = JsonSerializer.Serialize(payload, Json);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        private static MeProfile BuildProfile()
            => new()
            {
                FullName = "Deep UI Demo",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                TestAiConsentAt = DateTime.UtcNow.AddDays(-1),
                TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
            };
    }
}
