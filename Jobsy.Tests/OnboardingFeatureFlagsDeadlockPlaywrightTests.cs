using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Tests.E2e;
using Jobsy.Web.Models;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Regression: expired WebFeatureFlags cache + render must not sync-block the Blazor circuit (acc Volgende hang).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingFeatureFlagsDeadlockPlaywrightTests : IAsyncLifetime
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
    public Task Volgende_after_stale_feature_flags_cache_advances_on_live_circuit()
        => RunDeadlockRegressionAsync(_browser!);

    private static async Task RunDeadlockRegressionAsync(IBrowser browser)
    {
        using var web = new DeadlockWebFactory();
        var root = web.EnsureServerAddress();

        await using var context = await browser.NewContextAsync(DeadlockWebFactory.ContextOptions());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await FunctionalOnboardingInteractivityHelpers.WaitForCircuitReadyAsync(page, 120_000);
        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync(new() { Timeout = 90_000 });

        var invalidate = await page.APIRequest.GetAsync(root + "/__test/invalidate-feature-flags");
        Assert.True(invalidate.Ok);

        await page.WaitForTimeoutAsync(11_000);

        var birth = page.Locator("[data-ob-client=birthDate]");
        await birth.FillAsync("");
        await birth.PressSequentiallyAsync("12031995", new() { Delay = 60 });
        await Assertions.Expect(birth).ToHaveValueAsync("12-03-1995");

        var chipB = page.Locator(".work-status-chips.lobsy-choice-grid .lobsy-choice-tile").Nth(1);
        await chipB.ClickAsync(new() { Force = true });
        await Assertions.Expect(chipB).ToHaveAttributeAsync("aria-pressed", "true", new() { Timeout = 5_000 });

        var address = page.Locator("#onboarding-home-address");
        await address.FillAsync("Kalverstraat 1 Amsterdam");
        await page.WaitForTimeoutAsync(400);

        await page.Locator("[data-ob-step-next]").ClickAsync(new() { Timeout = 15_000 });
        await page.Locator("[data-ob-client-step=\"2\"]").WaitForAsync(new() { Timeout = 45_000 });
        Assert.Equal(2, web.SavedStep);
    }

    private sealed class DeadlockWebFactory : WebWebApplicationFactory
    {
        private IHost? _kestrel;
        internal int SavedStep { get; set; } = 1;
        internal MeProfile StoredProfile { get; private set; } = CreateReturningProfile();

        public string ServerAddress { get; private set; } = "";

        public string EnsureServerAddress()
        {
            if (string.IsNullOrWhiteSpace(ServerAddress))
            {
                _ = CreateClient();
            }

            return ServerAddress;
        }

        internal static BrowserNewContextOptions ContextOptions()
            => new()
            {
                ViewportSize = new() { Width = 1280, Height = 900 },
                Locale = "nl-NL",
            };

        private static MeProfile CreateReturningProfile()
        {
            var (first, last) = CandidateNameRules.ResolveOnboardingPrefill(
                "Test Kandidaat",
                "Kandidaat",
                "Test Kandidaat",
                "test.kandidaat@example.com");
            return new MeProfile
            {
                FirstName = first,
                LastName = last,
                FullName = "Test Kandidaat",
                Email = "test.kandidaat@example.com",
                Preferences = new CandidatePreferences(),
            };
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
                    ["Onboarding:RustigEnabled"] = "true",
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.Configure<HostOptions>(o =>
                    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
                services.AddSingleton<IStartupFilter>(new TestPathsFilter());
                services.RemoveAll<IGeocodingClient>();
                services.AddSingleton<IGeocodingClient>(new StubGeocoder());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubHttpFactory(this));
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(_ => new JobsyApiClient(
                    new HttpClient(new StubApiHandler(this), disposeHandler: false)
                    {
                        BaseAddress = new Uri("http://api.test/"),
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

        private sealed class StubGeocoder : IGeocodingClient
        {
            public Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<AddressSuggestion>>([new AddressSuggestion($"{query} (test)", 52.1, 4.2)]);

            public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
                => Task.FromResult<string?>("2671GB Den Haag");
        }

        private sealed class TestPathsFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
                => app =>
                {
                    app.Use(async (ctx, nxt) =>
                    {
                        if (ctx.Request.Path.Equals("/__test/invalidate-feature-flags", StringComparison.OrdinalIgnoreCase))
                        {
                            ctx.RequestServices.GetRequiredService<IFeatureFlags>().Invalidate();
                            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
                            return;
                        }

                        if (!ctx.Request.Path.Equals("/__test/sign-in", StringComparison.OrdinalIgnoreCase))
                        {
                            await nxt();
                            return;
                        }

                        var identity = new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                            new Claim(ClaimTypes.Role, "Candidate"),
                            new Claim(ClaimTypes.Name, "Flags deadlock E2E"),
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

        private sealed class StubHttpFactory(DeadlockWebFactory factory) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) =>
                new(new StubApiHandler(factory), disposeHandler: false) { BaseAddress = new Uri("http://api.test/") };
        }

        private sealed class StubApiHandler(DeadlockWebFactory factory) : HttpMessageHandler
        {
            private static readonly JsonSerializerOptions Json = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? "";

                if (path.Contains("/settings/feature-flags", StringComparison.OrdinalIgnoreCase)
                    && request.Method == HttpMethod.Get)
                {
                    await Task.Delay(150, cancellationToken);
                    return JsonResponse(new { testJourneyIllustrationEnabled = false, candidatePassportEnabled = true });
                }

                if (path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase) && request.Method == HttpMethod.Put)
                {
                    using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                    if (doc.RootElement.TryGetProperty("currentStep", out var stepProp))
                    {
                        factory.SavedStep = stepProp.GetInt32();
                    }

                    return JsonResponse(OnboardingDto());
                }

                if (path.Contains("/profile", StringComparison.OrdinalIgnoreCase) && request.Method == HttpMethod.Put)
                {
                    return JsonResponse(factory.StoredProfile);
                }

                object payload = path switch
                {
                    var p when p.Contains("/onboarding", StringComparison.OrdinalIgnoreCase) => OnboardingDto(),
                    var p when p.Contains("/profile", StringComparison.OrdinalIgnoreCase) => factory.StoredProfile,
                    var p when p.Contains("/site/status", StringComparison.OrdinalIgnoreCase) => new { maintenance = false },
                    _ => new { },
                };

                return JsonResponse(payload);

                OnboardingState OnboardingDto() => new()
                {
                    CurrentStep = factory.SavedStep,
                    StartedAtUtc = DateTime.UtcNow,
                    IsComplete = false,
                    ShouldShow = true,
                    WizardVersion = 2,
                    Source = "welcome",
                    Steps = [],
                };
            }

            private static HttpResponseMessage JsonResponse(object payload)
                => new(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload, Json), System.Text.Encoding.UTF8, "application/json"),
                };
        }
    }
}
