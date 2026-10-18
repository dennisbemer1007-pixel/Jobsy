using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
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
/// Evidence: step 6 inline dream error, then clean step 7 after "Weet ik nog niet" (1440 + 390).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingDreamValidationScreenshotTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
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
    public async Task Dream_error_stays_on_step_6_and_clears_on_step_7_at_mobile_and_desktop()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_CAPTURE_DREAM_VALIDATION")))
        {
            return;
        }

        using var web = new DreamStepWebFactory();
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString("/candidate/start");

        var artifactDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "artifacts", "onboarding-dream-validation");
        artifactDir = Path.GetFullPath(artifactDir);
        Directory.CreateDirectory(artifactDir);
        var storeDir = "/cursor/stores/self/onboarding-dream-validation";
        Directory.CreateDirectory(storeDir);

        foreach (var (width, height, tag) in new (int W, int H, string Tag)[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                Locale = "nl-NL",
                IsMobile = width < 600,
                HasTouch = width < 600,
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);

            await page.RunAndWaitForWebSocketAsync(
                async () => await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 }),
                new()
                {
                    Timeout = 60_000,
                    Predicate = socket => (socket.Url ?? "").Contains("_blazor", StringComparison.OrdinalIgnoreCase)
                });
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);

            await page.Locator(".ob-wizard__dream-search").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
            await page.WaitForTimeoutAsync(500);

            await page.Locator("[data-testid=onboarding-next]").ClickAsync(new() { Timeout = 20_000 });
            await Assertions.Expect(page.GetByText("Kies of typ een droombaan")).ToBeVisibleAsync(new() { Timeout = 30_000 });

            var errorPath = Path.Combine(artifactDir, $"step6-dream-error-{tag}.png");
            await page.ScreenshotAsync(new() { Path = errorPath, FullPage = true });
            File.Copy(errorPath, Path.Combine(storeDir, Path.GetFileName(errorPath)), overwrite: true);

            await page.Locator(".ob-wizard__footer-extra").Filter(new() { HasText = "Weet ik nog niet" }).ClickAsync();
            await page.Locator("#onboarding-field-consent, .ob-wizard__consent").First
                .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            Assert.Equal(0, await page.Locator("text=Kies of typ een droombaan").CountAsync());
            Assert.Equal(0, await page.Locator(".state-message.error").CountAsync());

            var cleanPath = Path.Combine(artifactDir, $"step7-clean-{tag}.png");
            await page.ScreenshotAsync(new() { Path = cleanPath, FullPage = true });
            File.Copy(cleanPath, Path.Combine(storeDir, Path.GetFileName(cleanPath)), overwrite: true);
        }
    }

    private sealed class DreamStepWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        private IHost? _kestrel;
        internal int SavedStep { get; set; } = 6;

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
                services.Configure<HostOptions>(o =>
                    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
                services.AddSingleton<IStartupFilter>(new SignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new StubFlags());
                services.RemoveAll<IGeocodingClient>();
                services.AddSingleton<IGeocodingClient>(new StubGeocoder());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubFactory(this));
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(_ => new JobsyApiClient(
                    new HttpClient(new StubHandler(this), disposeHandler: false)
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
            => ValueTask.FromResult(new FeatureFlagSnapshot(true, false));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class StubGeocoder : IGeocodingClient
    {
        public Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AddressSuggestion>>([]);

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
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
                        new Claim(ClaimTypes.Name, "Dream validation E2E")
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

    private sealed class StubFactory(DreamStepWebFactory factory) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler(factory), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubHandler(DreamStepWebFactory factory) : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/onboarding/dream-job", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase)
                && request.Method == HttpMethod.Put)
            {
                using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                if (doc.RootElement.TryGetProperty("currentStep", out var stepProp))
                {
                    factory.SavedStep = stepProp.GetInt32();
                }

                return JsonResponse(BuildOnboarding(factory.SavedStep));
            }

            object payload = path switch
            {
                var p when p.Contains("/onboarding", StringComparison.OrdinalIgnoreCase) => BuildOnboarding(factory.SavedStep),
                var p when p.Contains("/profile", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/competenc", StringComparison.OrdinalIgnoreCase) => EmptyTest(),
                _ => new { }
            };

            return JsonResponse(payload);
        }

        private static HttpResponseMessage JsonResponse(object payload)
        {
            var json = JsonSerializer.Serialize(payload, Json);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }

        private static OnboardingState BuildOnboarding(int step)
            => new()
            {
                CurrentStep = step,
                ShouldShow = true,
                Steps = []
            };

        private static MeProfile BuildProfile()
            => new()
            {
                FirstName = "Onboarding",
                LastName = "Tester",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                Preferences = new()
            };

        private static object EmptyTest()
            => new
            {
                status = "Draft",
                answeredCount = 0,
                questionCount = 0,
                answers = new Dictionary<string, int>()
            };
    }
}
