using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
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
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Clicks through candidate onboarding Volgende flow (local Kestrel + stub API).</summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateOnboardingWalkthroughPlaywrightTests : IAsyncLifetime
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

    public static TheoryData<int, int, bool, string> WalkthroughCases => new()
    {
        { 1280, 800, false, "office" },
        // Discovery journey (/ontdekkingsreis) uses query navigation; use OnboardingWizardNextPlaywrightTests + acceptatie for passport E2E.
    };

    [Theory(Skip = "Local Kestrel Blazor circuit is flaky in cloud CI; use OnboardingWizardNextPlaywrightTests against acceptatie (JOBSY_E2E_BASE_URL).")]
    [MemberData(nameof(WalkthroughCases))]
    public async Task Onboarding_volgende_shows_validation_then_advances_core_steps(
        int width,
        int height,
        bool passportOn,
        string presetMode)
    {
        using var web = new OnboardingWalkthroughWebFactory(passportOn);
        var startPath = passportOn ? "/candidate/ontdekkingsreis" : "/candidate/start";
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString(startPath);

        var consoleErrors = new List<string>();
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL",
            IsMobile = width < 640,
            HasTouch = width < 640,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        page.Console += (_, msg) =>
        {
            if (msg.Type is "error" or "warning")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        await page.RunAndWaitForWebSocketAsync(
            async () =>
            {
                await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
            },
            new()
            {
                Timeout = 60_000,
                Predicate = socket => (socket.Url ?? "").Contains("_blazor", StringComparison.OrdinalIgnoreCase)
            });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator(".ob-wizard__welcome, .ob-fields, .journey-step").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });

        if (passportOn)
        {
            await page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Start", RegexOptions.IgnoreCase) }).First.ClickAsync();
        }
        else if (await page.Locator(".ob-wizard__welcome").CountAsync() > 0)
        {
            var begin = page.Locator(".ob-wizard__welcome .ob-wizard__cta");
            await begin.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await begin.ClickAsync(new() { Delay = 100 });
        }

        try
        {
            await page.Locator(".ob-fields").First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        }
        catch (TimeoutException)
        {
            var err = "";
            if (await page.Locator(".state-message.error").CountAsync() > 0)
            {
                err = await page.Locator(".state-message.error").First.InnerTextAsync();
            }

            Assert.Fail(
                $"Personal step did not render (url={page.Url}, welcome={await page.Locator(".ob-wizard__welcome").CountAsync()}, "
                + $"journey={await page.Locator(".journey-step").CountAsync()}, error={err}).");
        }

        await FillPersonalStepAsync(page);

        await ClickVolgendeAsync(page);
        await page.WaitForTimeoutAsync(500);
        await page.Locator(".ob-wizard__presets").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });

        await SelectAvailabilityPresetAsync(page, presetMode);

        if (passportOn)
        {
            await SelectTransportAsync(page);
            await ClickVolgendeAsync(page);
        }
        else
        {
            await ClickVolgendeAsync(page);
            await page.WaitForTimeoutAsync(400);
            await SelectTransportAsync(page);
            await ClickVolgendeAsync(page);
        }

        await page.WaitForTimeoutAsync(800);

        var expectedStep = passportOn ? 3 : 4;
        Assert.True(
            web.SavedStep >= expectedStep,
            $"Expected onboarding step >={expectedStep}, got {web.SavedStep} (passport={passportOn}, preset={presetMode}).");

        Assert.DoesNotContain(consoleErrors, t => t.Contains("circuit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(consoleErrors, t => t.Contains("blazor", StringComparison.OrdinalIgnoreCase) && t.Contains("Error", StringComparison.Ordinal));
    }

    private static async Task FillPersonalStepAsync(IPage page)
    {
        var first = page.Locator(".ob-fields label:has-text('Voornaam') input, .ob-fields input").First;
        await first.FillAsync("Alex");
        await page.Locator(".ob-fields input").Nth(1).FillAsync("Jansen");
        await page.Locator(".ob-fields input[inputmode=text], .ob-fields input[autocomplete=postal-code]").First.FillAsync("2671GB");
        await page.WaitForTimeoutAsync(500);
        var suggestion = page.Locator(".ob-wizard__suggestions button").First;
        if (await suggestion.CountAsync() > 0)
        {
            await suggestion.ClickAsync();
        }

        var birth = page.Locator("[data-testid=onboarding-birth-date] input[type=date]").First;
        if (await birth.CountAsync() > 0)
        {
            var d = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-22);
            await birth.FillAsync(d.ToString("yyyy-MM-dd"));
            await birth.EvaluateAsync("el => el.dispatchEvent(new Event('change', { bubbles: true }))");
        }

        await page.WaitForTimeoutAsync(300);
    }

    private static async Task SelectAvailabilityPresetAsync(IPage page, string presetMode)
    {
        if (presetMode == "custom")
        {
            var custom = page.Locator(".ob-wizard__presets button").Filter(new() { HasText = "Zelf" }).First;
            await custom.ClickAsync();
            var cell = page.Locator(".availability-matrix__cell--btn").First;
            await cell.ClickAsync();
            return;
        }

        var office = page.Locator(".ob-wizard__presets button").Filter(new() { HasText = "Kantoor" }).First;
        if (await office.CountAsync() == 0)
        {
            await page.Locator(".ob-wizard__presets button").First.ClickAsync();
        }
        else
        {
            await office.ClickAsync();
        }
    }

    private static async Task SelectTransportAsync(IPage page)
    {
        var transport = page.Locator("#onboarding-field-transport .ob-wizard__transport button, .ob-wizard__transport button").First;
        if (await transport.CountAsync() > 0)
        {
            await transport.ClickAsync();
        }
    }

    private static async Task ClickVolgendeAsync(IPage page)
    {
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        var next = page.GetByRole(AriaRole.Button, new() { Name = "Volgende", Exact = true });
        if (await next.CountAsync() == 0)
        {
            next = page.Locator("[data-testid=onboarding-next], button.ob-wizard__cta.btn-primary");
        }

        var force = (page.ViewportSize?.Width ?? 1280) < 600;
        await next.First.ClickAsync(new() { Timeout = 20_000, Force = force });
    }

    private sealed class OnboardingWalkthroughWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        private readonly bool _passportOn;
        private IHost? _kestrel;
        internal int SavedStep { get; set; } = 1;

        public string ServerAddress { get; private set; } = "";

        public OnboardingWalkthroughWebFactory(bool passportOn) => _passportOn = passportOn;

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
                services.AddSingleton<IFeatureFlags>(new StubFlags(_passportOn));
                services.RemoveAll<IGeocodingClient>();
                services.AddSingleton<IGeocodingClient>(new StubGeocoder());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubFactory(this));
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
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

    private sealed class StubFlags(bool passportOn) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: true,
                CandidatePassportEnabled: passportOn));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class StubGeocoder : IGeocodingClient
    {
        public Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AddressSuggestion>>(
            [
                new("2671 GB Naaldwijk", 51.994, 4.205)
            ]);

        public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("2671 GB Naaldwijk");
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
                        new Claim(ClaimTypes.Name, "Onboarding E2E")
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

    private sealed class StubFactory(OnboardingWalkthroughWebFactory factory) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler(factory), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubHandler(OnboardingWalkthroughWebFactory factory) : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
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
                HomeLatitude = 51.994,
                HomeLongitude = 4.205,
                Preferences = new()
                {
                    HomeAddress = "2671 GB Naaldwijk"
                }
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
