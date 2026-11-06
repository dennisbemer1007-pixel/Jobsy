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

/// <summary>
/// Regression: onboarding step 1 keeps exact address/date text while geocode latency is in flight.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingStep1TypingStayClickablePlaywrightTests : IAsyncLifetime
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

    [Theory]
    [InlineData(1440, 900, false)]
    [InlineData(390, 844, true)]
    public async Task Step1_slow_typing_keeps_exact_address_and_birth_date(int width, int height, bool mobile)
    {
        using var web = new SlowSuggestOnboardingWebFactory();
        var serverRoot = web.EnsureServerAddress();
        var baseUrl = serverRoot
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString("/candidate/start");

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL",
            IsMobile = mobile,
            HasTouch = mobile,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await FunctionalOnboardingInteractivityHelpers.WaitForCircuitReadyAsync(page, 120_000);
        await FunctionalOnboardingInteractivityHelpers.ClickBeginnenOnWelcomeAsync(page, mobile);

        await page.Locator("#onboarding-home-address").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });

        const string addressText = "Jan van den Bosch";
        const string birthDigits = "12031990";
        const string birthFormatted = "12-03-1990";

        var address = page.Locator("#onboarding-home-address").First;
        await address.ClickAsync(new() { Force = true });
        await address.FillAsync("");
        await address.PressSequentiallyAsync(addressText, new() { Delay = 250 });
        await Assertions.Expect(address).ToHaveValueAsync(addressText, new() { Timeout = 15_000 });

        var chip = page.Locator(".work-status-chips .lobsy-choice-tile").First;
        await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, chip, "werkstatus-during-address-typing");
        await AssertChipTogglesQuicklyAsync(page, chip, mobile, "during-address-typing");

        var birthText = page.Locator("[data-testid=onboarding-birth-date] .jobsy-date-field__text").First;
        await birthText.ClickAsync(new() { Force = true });
        await birthText.FillAsync("");
        await birthText.PressSequentiallyAsync(birthDigits, new() { Delay = 250 });
        await Assertions.Expect(birthText).ToHaveValueAsync(birthFormatted, new() { Timeout = 15_000 });
        await FunctionalOnboardingInteractivityHelpers.WaitForCircuitReadyAsync(page, 15_000);

        var suggestionsOpen = page.Locator(".ob-fields__address .address-suggestions");
        if (await suggestionsOpen.CountAsync() > 0)
        {
            await birthText.ClickAsync(new() { Force = true });
            await page.Keyboard.PressAsync("Escape");
            await page.WaitForTimeoutAsync(200);
        }

        await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, chip, "werkstatus-after-birth-date");
        await AssertChipTogglesQuicklyAsync(page, chip, mobile, "after-birth-date");

        var next = page.GetByTestId("onboarding-next");
        await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, next, "volgende-after-typing");
        await next.ClickAsync(new() { Force = mobile, Timeout = 10_000 });

        Assert.Equal("ready", await page.EvaluateAsync<string>(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') || 'missing'"));

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var health = await http.GetStringAsync($"{serverRoot.TrimEnd('/')}/healthz");
        Assert.Equal("ok", health);
    }

    private static async Task AssertChipTogglesQuicklyAsync(IPage page, ILocator chip, bool mobile, string label)
    {
        await chip.ScrollIntoViewIfNeededAsync();
        var before = await chip.GetAttributeAsync("aria-pressed") ?? "false";
        var deadline = DateTime.UtcNow.AddSeconds(4);
        if (mobile)
        {
            await chip.TapAsync(new() { Timeout = 4_000 });
        }
        else
        {
            await chip.ClickAsync(new() { Timeout = 4_000 });
        }

        while (DateTime.UtcNow < deadline)
        {
            var now = await chip.GetAttributeAsync("aria-pressed") ?? "false";
            if (now != before)
            {
                return;
            }

            await page.WaitForTimeoutAsync(75);
        }

        var audit = await FunctionalOnboardingInteractivityHelpers.DescribeHitAuditsAsync(page, [
            (".work-status-chips .lobsy-choice-tile", "werkstatus-chip"),
            ("[data-testid=onboarding-next]", "volgende"),
        ]);
        Assert.Fail($"{label}: work-status chip did not toggle within 4s. {audit}");
    }

    private sealed class SlowSuggestOnboardingWebFactory : WebWebApplicationFactory
    {
        private IHost? _kestrel;
        internal int SavedStep { get; set; } = 1;

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
                services.AddSingleton<IGeocodingClient>(new SlowGeocoder());
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

    private sealed class SlowGeocoder : IGeocodingClient
    {
        public async Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(150, cancellationToken);
            return [new AddressSuggestion($"{query} (test)", 52.0, 4.0)];
        }

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
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
                        new Claim(ClaimTypes.Name, "Step1 Typing E2E"),
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

    private sealed class StubHttpFactory(SlowSuggestOnboardingWebFactory factory) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new StubApiHandler(factory), disposeHandler: false) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubApiHandler(SlowSuggestOnboardingWebFactory factory) : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(150, cancellationToken);
            var path = request.RequestUri?.AbsolutePath ?? "";
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

            if (path.Contains("/profile", StringComparison.OrdinalIgnoreCase)
                && (request.Method == HttpMethod.Put || request.Method == HttpMethod.Post))
            {
                return JsonResponse(BuildProfile());
            }

            object payload = path switch
            {
                var p when p.Contains("/onboarding", StringComparison.OrdinalIgnoreCase) => BuildOnboarding(factory.SavedStep),
                var p when p.Contains("/profile", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/private-preferences", StringComparison.OrdinalIgnoreCase) => new { dislikes = Array.Empty<string>(), customDislikes = Array.Empty<string>() },
                var p when p.Contains("/career-path", StringComparison.OrdinalIgnoreCase) => new { dreamTitle = "" },
                var p when p.Contains("/competencies", StringComparison.OrdinalIgnoreCase) => EmptyTest(),
                var p when p.Contains("/career-interests", StringComparison.OrdinalIgnoreCase) => EmptyTest(),
                var p when p.Contains("/culture", StringComparison.OrdinalIgnoreCase) => EmptyTest(),
                var p when p.Contains("/values", StringComparison.OrdinalIgnoreCase) => EmptyTest(),
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

        private static OnboardingState BuildOnboarding(int step)
            => new()
            {
                CurrentStep = step,
                ShouldShow = true,
                Steps = [],
            };

        private static MeProfile BuildProfile()
            => new()
            {
                FirstName = "Onboarding",
                LastName = "Tester",
                Preferences = new() { HomeAddress = "" },
            };

        private static object EmptyTest()
            => new
            {
                status = "Draft",
                answeredCount = 0,
                questionCount = 0,
                answers = new Dictionary<string, int>(),
            };
    }
}
