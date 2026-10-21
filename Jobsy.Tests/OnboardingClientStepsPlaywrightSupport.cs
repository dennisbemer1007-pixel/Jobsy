using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Features;
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

internal static class OnboardingClientStepsPlaywrightSupport
{
    internal const int MobileWidth = 390;
    internal const int MobileHeight = 844;

    internal static BrowserNewContextOptions ContextOptions(bool mobile)
        => new()
        {
            ViewportSize = new() { Width = mobile ? MobileWidth : 1280, Height = mobile ? MobileHeight : 900 },
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = "nl-NL",
        };

    internal static async Task RunAddressSuggestMinThreeCharsAsync(IBrowser browser, bool mobile)
    {
        using var web = new ClientStepsWebFactory();
        var serverRoot = web.EnsureServerAddress();
        var suggestGetCount = 0;

        await using var context = await browser.NewContextAsync(ContextOptions(mobile));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        page.Request += (_, request) =>
        {
            if (request.Method == "GET"
                && request.Url.Contains("/api/onboarding-client/address-suggest", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref suggestGetCount);
            }
        };

        await page.GotoAsync(serverRoot + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await FunctionalOnboardingInteractivityHelpers.WaitForCircuitReadyAsync(page, 120_000);
        await FunctionalOnboardingInteractivityHelpers.ClickBeginnenOnWelcomeAsync(page, mobile);
        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync(new() { Timeout = 90_000 });

        var address = page.Locator("#onboarding-home-address");
        await address.FillAsync("");
        await address.PressSequentiallyAsync("ab", new() { Delay = 50 });
        await page.WaitForTimeoutAsync(450);
        Assert.Equal(0, suggestGetCount);

        await address.PressSequentiallyAsync("c", new() { Delay = 50 });
        await page.WaitForTimeoutAsync(450);
        Assert.True(suggestGetCount >= 1, "Expected at least one suggest fetch after 3+ characters.");
    }

    internal static async Task RunSteps1And2SaveOnceAsync(IBrowser browser, bool mobile)
    {
        using var web = new ClientStepsWebFactory();
        var serverRoot = web.EnsureServerAddress();

        await using var context = await browser.NewContextAsync(ContextOptions(mobile));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var baseUrl = serverRoot + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start");
        await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await FunctionalOnboardingInteractivityHelpers.WaitForCircuitReadyAsync(page, 120_000);
        await FunctionalOnboardingInteractivityHelpers.ClickBeginnenOnWelcomeAsync(page, mobile);

        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync(new() { Timeout = 90_000 });

        var address = page.Locator("#onboarding-home-address");
        await address.FillAsync("");
        await address.PressSequentiallyAsync("Jan van den Bosch", new() { Delay = mobile ? 60 : 80 });
        await Assertions.Expect(address).ToHaveValueAsync("Jan van den Bosch");

        var birth = page.Locator("[data-ob-client=birthDate]");
        await birth.FillAsync("");
        await birth.PressSequentiallyAsync("12031990", new() { Delay = mobile ? 60 : 80 });
        await Assertions.Expect(birth).ToHaveValueAsync("12-03-1990");

        var scholier = page.Locator(".work-status-chips .lobsy-choice-tile").Filter(new() { HasText = "Scholier" }).First;
        for (var i = 0; i < 20; i++)
        {
            await scholier.ClickAsync(new() { Timeout = 5_000 });
            await page.WaitForTimeoutAsync(mobile ? 100 : 150);
        }

        Assert.Equal(0, web.ProfilePutCount);

        await page.GetByTestId("onboarding-next").ClickAsync(new() { Timeout = 15_000 });
        await page.Locator("[data-ob-client-step=\"2\"]").WaitForAsync(new() { Timeout = 90_000 });
        Assert.Equal(1, web.ProfilePutCount);

        var custom = page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile").Filter(new() { HasText = "Zelf" }).First;
        var cell = page.Locator(".availability-matrix__cell--btn").First;
        for (var i = 0; i < 20; i++)
        {
            await custom.ClickAsync(new() { Timeout = 5_000 });
            await cell.ClickAsync(new() { Timeout = 5_000 });
            await page.WaitForTimeoutAsync(mobile ? 100 : 150);
        }

        Assert.Equal("true", await cell.GetAttributeAsync("aria-pressed"));
        Assert.Equal(1, web.ProfilePutCount);

        await page.GetByTestId("onboarding-next").ClickAsync(new() { Timeout = 15_000 });
        await page.Locator("[data-ob-client-step=\"3\"]").WaitForAsync(new() { Timeout = 90_000 });
        Assert.Equal(2, web.ProfilePutCount);

        var transport = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile").First;
        for (var i = 0; i < 15; i++)
        {
            await transport.TapAsync(new() { Timeout = 5_000 });
            await page.WaitForTimeoutAsync(80);
        }

        await page.GetByTestId("onboarding-next").ClickAsync(new() { Timeout = 15_000 });
        await page.Locator("[data-ob-client-step=\"4\"]").WaitForAsync(new() { Timeout = 90_000 });
        Assert.Equal(3, web.ProfilePutCount);

        Assert.Equal("ready", await page.EvaluateAsync<string>(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') || ''"));
    }

    internal static async Task RunHappyPathTowardDreamAsync(IBrowser browser, bool mobile)
    {
        using var web = new ClientStepsWebFactory();
        var serverRoot = web.EnsureServerAddress();
        await using var context = await browser.NewContextAsync(ContextOptions(mobile));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(serverRoot + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await FunctionalOnboardingInteractivityHelpers.WaitForCircuitReadyAsync(page, 120_000);
        await FunctionalOnboardingInteractivityHelpers.ClickBeginnenOnWelcomeAsync(page, mobile);

        await CompleteStep1MinimalAsync(page);
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.Locator("[data-ob-client-step=\"2\"]").WaitForAsync(new() { Timeout = 90_000 });

        var preset = page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile").First;
        await preset.TapAsync();
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.Locator("[data-ob-client-step=\"3\"]").WaitForAsync(new() { Timeout = 90_000 });

        await page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile").First.TapAsync();
        await page.Locator("[data-ob-client=travelGrid] .lobsy-choice-tile").First.TapAsync();
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.Locator("[data-ob-client-step=\"4\"]").WaitForAsync(new() { Timeout = 90_000 });

        await page.GetByTestId("onboarding-next").ClickAsync(new() { Force = true });
        await page.Locator("[data-ob-client-step=\"5\"]").WaitForAsync(new() { Timeout = 90_000 });
        await page.GetByTestId("onboarding-next").ClickAsync(new() { Force = true });
        await page.Locator("[data-ob-client-step=\"6\"]").WaitForAsync(new() { Timeout = 90_000 });

        var dream = page.Locator("[data-ob-client=dreamInput]");
        await dream.FillAsync("Kassière");
        await Assertions.Expect(dream).ToHaveValueAsync("Kassière");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.Locator("[data-ob-client-step=\"7\"]").WaitForAsync(new() { Timeout = 90_000 });

        Assert.True(web.ProfilePutCount >= 5);
    }

    private static async Task CompleteStep1MinimalAsync(IPage page)
    {
        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync(new() { Timeout = 90_000 });
        var address = page.Locator("#onboarding-home-address");
        await address.FillAsync("2671GB");
        await page.Locator(".address-suggestions__item").First.ClickAsync(new() { Timeout = 15_000 });
        var birth = page.Locator("[data-ob-client=birthDate]");
        await birth.FillAsync("12-03-1990");
        await page.Locator(".work-status-chips .lobsy-choice-tile").First.TapAsync();
    }

    internal sealed class ClientStepsWebFactory : WebWebApplicationFactory
    {
        private IHost? _kestrel;
        internal int SavedStep { get; set; } = 1;
        internal string? OnboardingSource;
        internal int ProfilePutCount;

        public string ServerAddress { get; private set; } = "";

        public string EnsureServerAddress()
        {
            if (string.IsNullOrWhiteSpace(ServerAddress))
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
    }

    private sealed class StubGeocoder : IGeocodingClient
    {
        public async Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
        {
            await Task.Delay(80, cancellationToken);
            return [new AddressSuggestion($"{query} (test)", 52.1, 4.2)];
        }

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("2671GB Den Haag");
    }

    private sealed class StubFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(EmployersEnabled: true, CandidatePassportEnabled: false));

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
                        new Claim(ClaimTypes.Name, "Client Steps E2E"),
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

    private sealed class StubHttpFactory(ClientStepsWebFactory factory) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new StubApiHandler(factory), disposeHandler: false) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubApiHandler(ClientStepsWebFactory factory) : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(80, cancellationToken);
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase) && request.Method == HttpMethod.Put)
            {
                using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                if (doc.RootElement.TryGetProperty("currentStep", out var stepProp))
                {
                    factory.SavedStep = stepProp.GetInt32();
                }

                if (doc.RootElement.TryGetProperty("source", out var sourceProp) && sourceProp.ValueKind == JsonValueKind.String)
                {
                    factory.OnboardingSource = sourceProp.GetString();
                }

                return JsonResponse(new
                {
                    currentStep = factory.SavedStep,
                    shouldShow = true,
                    source = factory.OnboardingSource,
                    steps = Array.Empty<object>(),
                });
            }

            if (path.Contains("/profile", StringComparison.OrdinalIgnoreCase) && request.Method == HttpMethod.Put)
            {
                Interlocked.Increment(ref factory.ProfilePutCount);
                return JsonResponse(new MeProfile { FirstName = "T", LastName = "E" });
            }

            object payload = path switch
            {
                var p when p.Contains("/onboarding", StringComparison.OrdinalIgnoreCase) => new
                {
                    currentStep = factory.SavedStep,
                    shouldShow = true,
                    source = factory.OnboardingSource,
                    steps = Array.Empty<object>(),
                },
                var p when p.Contains("/profile", StringComparison.OrdinalIgnoreCase) => new MeProfile
                {
                    FirstName = "Onboarding",
                    LastName = "Tester",
                },
                var p when p.Contains("/competencies", StringComparison.OrdinalIgnoreCase) => new { answers = new Dictionary<string, int>(), quickScanVersion = 1 },
                var p when p.Contains("/career-interests", StringComparison.OrdinalIgnoreCase) => new { answers = new Dictionary<string, int>(), quickScanVersion = 1 },
                var p when p.Contains("/culture", StringComparison.OrdinalIgnoreCase) => new { answers = new Dictionary<string, int>(), quickScanVersion = 1 },
                var p when p.Contains("/values", StringComparison.OrdinalIgnoreCase) => new { answers = new Dictionary<string, int>(), quickScanVersion = 1 },
                var p when p.Contains("/site/status", StringComparison.OrdinalIgnoreCase) => new { maintenance = false },
                _ => new { },
            };

            return JsonResponse(payload);
        }

        private static HttpResponseMessage JsonResponse(object payload)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, Json), System.Text.Encoding.UTF8, "application/json"),
            };
    }
}
