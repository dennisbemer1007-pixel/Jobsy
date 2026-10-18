using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Features;
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

[Collection("PlaywrightSmoke")]
public sealed class DreamJobIllustrationScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/screenshots/droombaan";
    private const string StoreDir = "/cursor/stores/self/droombaan";
    private DreamJobScreenshotWebFactory? _webOn;
    private DreamJobScreenshotWebFactory? _webOff;
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
        _webOn = new DreamJobScreenshotWebFactory(illustrationEnabled: true);
        _webOff = new DreamJobScreenshotWebFactory(illustrationEnabled: false);
        _ = _webOn.ServerAddress;
        _ = _webOff.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _webOn?.Dispose();
        _webOff?.Dispose();
    }

    [Fact]
    public async Task Capture_dream_job_step_screenshots()
    {
        Directory.CreateDirectory(ArtifactsDir);
        Directory.CreateDirectory(StoreDir);

        await CaptureSetAsync(_webOn!, illustrationOn: true);
        await CaptureSetAsync(_webOff!, illustrationOn: false);
    }

    private async Task CaptureSetAsync(DreamJobScreenshotWebFactory web, bool illustrationOn)
    {
        var prefix = illustrationOn ? "flag-on" : "flag-off";

        await CaptureAsync(web, 1440, 900, 1, $"{prefix}-desktop-1440-empty.png", chip: false, focusSearch: false);
        if (illustrationOn)
        {
            await CaptureAsync(web, 1440, 900, 1, $"{prefix}-desktop-1440-chip.png", chip: true, focusSearch: false);
            await CaptureAsync(web, 390, 844, 2, $"{prefix}-mobile-390-empty.png", chip: false, focusSearch: false);
            await CaptureAsync(web, 390, 844, 2, $"{prefix}-mobile-390-chip.png", chip: true, focusSearch: false);
            await CaptureAsync(web, 390, 844, 2, $"{prefix}-mobile-390-focus.png", chip: false, focusSearch: true);
            await CaptureAsync(web, 360, 780, 2, $"{prefix}-mobile-360-empty.png", chip: false, focusSearch: false);
        }
    }

    private async Task CaptureAsync(
        DreamJobScreenshotWebFactory web,
        int width,
        int height,
        int scale,
        string fileName,
        bool chip,
        bool focusSearch)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            DeviceScaleFactor = scale,
            IsMobile = width < 500,
            HasTouch = width < 500,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await page.GotoAsync(
            $"{web.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString("/candidate/start")}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        await page.GetByRole(AriaRole.Heading, new() { Name = "Wat is je droombaan?" })
            .WaitForAsync(new() { Timeout = 60_000 });

        if (chip)
        {
            await page.Locator("[data-testid=dream-inspiration-chip]").First.ClickAsync();
            await page.Locator("[data-testid=dream-job-confirm]").WaitForAsync(new() { Timeout = 15_000 });
            await page.Locator(".ob-dream-typeahead").WaitForAsync(new() { Timeout = 15_000 });
        }

        if (focusSearch)
        {
            await page.Locator("[data-testid=dream-job-search]").FocusAsync();
            await page.WaitForTimeoutAsync(400);
        }

        var artifactPath = Path.Combine(ArtifactsDir, fileName);
        var storePath = Path.Combine(StoreDir, fileName);
        await page.ScreenshotAsync(new() { Path = artifactPath, FullPage = true });
        File.Copy(artifactPath, storePath, overwrite: true);
    }

    internal sealed class DreamJobScreenshotWebFactory : WebWebApplicationFactory
    {
        private readonly bool _illustrationEnabled;
        private IHost? _kestrel;

        public DreamJobScreenshotWebFactory(bool illustrationEnabled)
        {
            _illustrationEnabled = illustrationEnabled;
            _ = CreateClient();
        }

        public string ServerAddress { get; private set; } = "";

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
                    [Jobsy.Core.Options.DreamJobIllustrationOptions.EnabledKey] = _illustrationEnabled ? "true" : "false",
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.AddSingleton<IStartupFilter>(new CandidateSignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new OnboardingFlags());
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

    private sealed class CandidateSignInFilter : IStartupFilter
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
                        new Claim(ClaimTypes.Name, "Alex Demo")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/start";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class OnboardingFlags : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(EmployersEnabled: true, CandidatePassportEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature != PlatformFeature.CandidatePassport || Snap.CandidatePassportEnabled);

        public void Invalidate()
        {
        }
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
            var method = request.Method;

            if (path.Contains("/api/me/onboarding", StringComparison.OrdinalIgnoreCase)
                && method == HttpMethod.Get)
            {
                var onboarding = new OnboardingState
                {
                    CurrentStep = 6,
                    ShouldShow = true,
                    IsComplete = false,
                    FinishReached = false,
                    StartedAtUtc = DateTime.UtcNow.AddDays(-1),
                    Steps =
                    [
                        new OnboardingStepEvent { Step = 1, CompletedAtUtc = DateTime.UtcNow.AddHours(-1) },
                        new OnboardingStepEvent { Step = 2, CompletedAtUtc = DateTime.UtcNow.AddHours(-1) },
                        new OnboardingStepEvent { Step = 3, CompletedAtUtc = DateTime.UtcNow.AddHours(-1) },
                        new OnboardingStepEvent { Step = 4, CompletedAtUtc = DateTime.UtcNow.AddHours(-1) },
                        new OnboardingStepEvent { Step = 5, CompletedAtUtc = DateTime.UtcNow.AddHours(-1) },
                        new OnboardingStepEvent { Step = 6, StartedAtUtc = DateTime.UtcNow.AddMinutes(-5) },
                    ]
                };
                return JsonOk(onboarding);
            }

            if (path.Contains("/api/me/profile", StringComparison.OrdinalIgnoreCase) && method == HttpMethod.Get)
            {
                var profile = new MeProfile
                {
                    Id = Guid.Parse("d2a20000-0000-0000-0000-000000000042"),
                    FirstName = "Alex",
                    LastName = "Jansen",
                    DateOfBirth = new DateOnly(1995, 3, 12),
                    Preferences = new CandidatePreferences
                    {
                        HomeAddress = "2671 GB Naaldwijk",
                        MinHoursPerWeek = 16,
                        MaxHoursPerWeek = 32,
                        MaxTravelMinutes = 30,
                        PreferredTransport = "Auto",
                    }
                };
                return JsonOk(profile);
            }

            if (path.Contains("/site/status", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"maintenance":false}""", System.Text.Encoding.UTF8, "application/json")
                });
            }

            if (method == HttpMethod.Post || method == HttpMethod.Put)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            });
        }

        private static Task<HttpResponseMessage> JsonOk<T>(T payload)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, Json), System.Text.Encoding.UTF8, "application/json")
            });
    }
}
