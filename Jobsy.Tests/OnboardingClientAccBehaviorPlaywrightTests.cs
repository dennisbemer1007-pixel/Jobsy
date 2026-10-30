using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Features;
using Jobsy.Core.Privacy;
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

/// <summary>Acc-like onboarding: Production env, separate API host stub, slow human pacing, DOM/BFF regressions.</summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingClientAccBehaviorPlaywrightTests : IAsyncLifetime
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
    public Task Step3_survives_back_and_forward_with_real_clicks()
        => RunAccScenarioAsync(_browser!, async (page, web, root) =>
        {
            web.SavedStep = 3;
            await page.GotoAsync(root + "/candidate/start");
            await WaitForStepMountedAsync(page, 3);
            await HumanPauseAsync(page);
            var fiets = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile", new() { HasText = "Fiets" });
            if (await fiets.CountAsync() == 0)
            {
                fiets = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile").First;
            }

            await fiets.ClickAsync();
            await HumanPauseAsync(page);
            await page.Locator("[data-ob-client=travelGrid] .lobsy-choice-tile").First.ClickAsync();
            await HumanPauseAsync(page);
            await ClickObStepAdvanceAsync(page, 3);
            await WaitForStepMountedAsync(page, 4);
            await page.Locator("[data-testid=onboarding-back]").ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.Load);
            await WaitForStepMountedAsync(page, 3);
            await HumanPauseAsync(page);
            var pressed = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile[aria-pressed=\"true\"]");
            Assert.True(await pressed.CountAsync() >= 1, "Transport tile should stay selected after Terug.");
            await ClickObStepAdvanceAsync(page, 3);
            await WaitForStepMountedAsync(page, 4);
        });

    [Fact]
    public Task Step6_dream_input_keeps_first_letter()
        => RunAccScenarioAsync(_browser!, async (page, web, root) =>
        {
            web.SavedStep = 6;
            await page.GotoAsync(root + "/candidate/start");
            await WaitForStepMountedAsync(page, 6);
            await HumanPauseAsync(page);
            var input = page.Locator("[data-ob-client=dreamInput]");
            await input.ClickAsync();
            await input.PressSequentiallyAsync("Kok", new() { Delay = 120 });
            await HumanPauseAsync(page);
            Assert.Equal("Kok", await input.InputValueAsync());
        });

    [Fact]
    public Task Step7_consent_and_mini_bff_succeeds_after_slow_clicks()
        => RunAccScenarioAsync(_browser!, async (page, web, root) =>
        {
            web.SavedStep = 7;
            await page.GotoAsync(root + "/candidate/start");
            await WaitForStepMountedAsync(page, 7);
            await HumanPauseAsync(page);
            await page.Locator("[data-ob-client=consent]").CheckAsync();
            await AnswerMiniBlockAsync(page);
            await HumanPauseAsync(page);
            await ClickObStepAdvanceAsync(page, 7);
            await page.Locator("[data-ob-step-error]").WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5_000 });
            Assert.True(web.SavedStep >= 8 || web.CompetencyAnswers.Count >= 5,
            $"Expected step >= 8 or competency answers saved after step 7, got step {web.SavedStep}, answers {web.CompetencyAnswers.Count}.");
        });

    [Fact]
    public Task Full_walk_step2_through_kompas_with_acc_pacing()
        => RunAccScenarioAsync(_browser!, async (page, web, root) =>
        {
            web.SavedStep = 2;
            await page.GotoAsync(root + "/candidate/start");
            for (var step = 2; step <= 6; step++)
            {
                await WaitForStepMountedAsync(page, step);
                await HumanPauseAsync(page);
                if (step == 2)
                {
                    await page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile").First.ClickAsync();
                }
                else if (step == 3)
                {
                    await page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile").First.ClickAsync();
                    await page.Locator("[data-ob-client=travelGrid] .lobsy-choice-tile").First.ClickAsync();
                }
                else if (step == 6)
                {
                    await page.Locator("[data-ob-client=dreamInput]").PressSequentiallyAsync("Kok", new() { Delay = 100 });
                }

                await ClickObStepAdvanceAsync(page, step);
            }

            await WaitForStepMountedAsync(page, 7);
            await HumanPauseAsync(page);
            await page.Locator("[data-ob-client=consent]").CheckAsync();
            await AnswerMiniBlockAsync(page);
            await ClickObStepAdvanceAsync(page, 7);

            for (var step = 8; step <= 10; step++)
            {
                await WaitForStepMountedAsync(page, step);
                await HumanPauseAsync(page);
                await AnswerMiniBlockAsync(page);
                await ClickObStepAdvanceAsync(page, step);
            }

            await page.Locator("[data-ob-kompas-complete]").First.WaitForAsync(new() { Timeout = 90_000 });
            await HumanPauseAsync(page);
            var kompasResponse = page.WaitForResponseAsync(r =>
                r.Request.Method == "POST"
                && r.Url.Contains("/api/onboarding-client/kompas/complete", StringComparison.OrdinalIgnoreCase));
            await page.Locator("[data-ob-kompas-complete]").First.ClickAsync();
            var kompasHttp = await kompasResponse;
            Assert.True(kompasHttp.Ok, $"Kompas HTTP {(int)kompasHttp.Status}.");
        });

    private static async Task RunAccScenarioAsync(IBrowser browser, Func<IPage, AccWebFactory, string, Task> body)
    {
        using var web = new AccWebFactory();
        var root = web.EnsureServerAddress();
        await using var context = await browser.NewContextAsync(AccWebFactory.ContextOptions(false));
        await context.RouteAsync(
            url => url.Contains("/_blazor", StringComparison.OrdinalIgnoreCase),
            route => route.AbortAsync());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        web.StoredProfile.DateOfBirth = new DateOnly(1990, 3, 12);
        web.StoredProfile.HasDateOfBirth = true;
        web.StoredProfile.HomeLatitude = 52.1;
        web.StoredProfile.HomeLongitude = 4.2;
        web.StoredProfile.Preferences.HomeAddress = "Kalverstraat 1 Amsterdam";
        web.StoredProfile.Preferences.WorkStatuses = ["student"];
        await body(page, web, root);
    }

    private static Task HumanPauseAsync(IPage page) => page.WaitForTimeoutAsync(12_000);

    private static async Task WaitForStepMountedAsync(IPage page, int step)
    {
        await page.Locator($"[data-ob-client-step=\"{step}\"]").WaitForAsync(new() { Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            $$"""() => document.querySelector('[data-ob-client-step="{{step}}"]')?.getAttribute('data-ob-client-mounted') === '1'""",
            null,
            new() { Timeout = 90_000 });
    }

    private static async Task ClickObStepAdvanceAsync(IPage page, int step)
    {
        var advanceResponse = page.WaitForResponseAsync(r =>
            r.Request.Method == "PUT"
            && r.Url.Contains($"/api/onboarding-client/steps/{step}", StringComparison.OrdinalIgnoreCase)
            && (r.Request.PostData ?? "").Contains("\"advance\":true", StringComparison.Ordinal));
        await page.Locator("[data-testid=onboarding-next]").ClickAsync();
        var response = await advanceResponse;
        var responseBody = response.Ok ? "" : await response.TextAsync();
        Assert.True(response.Ok, $"Step {step} advance HTTP {(int)response.Status}. {responseBody}");
        await page.WaitForLoadStateAsync(LoadState.Load, new() { Timeout = 90_000 });
    }

    private static async Task AnswerMiniBlockAsync(IPage page)
    {
        var cards = page.Locator(".ob-client-mini-q");
        var count = await cards.CountAsync();
        for (var i = 0; i < count; i++)
        {
            await cards.Nth(i).Locator(".lobsy-choice-tile").Nth(2).ClickAsync();
            await page.WaitForTimeoutAsync(200);
        }
    }

    private sealed class AccWebFactory : WebWebApplicationFactory
    {
        private IHost? _kestrel;
        internal int SavedStep { get; set; } = 1;
        internal bool OnboardingComplete { get; set; }
        internal MeProfile StoredProfile { get; } = CreateProfile();
        internal Dictionary<string, int> CompetencyAnswers { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, int> CareerAnswers { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, int> CultureAnswers { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, int> ValuesAnswers { get; } = new(StringComparer.Ordinal);

        private static MeProfile CreateProfile()
        {
            var (first, last) = CandidateNameRules.ResolveOnboardingPrefill(
                "Test Kandidaat", "Kandidaat", "Test Kandidaat", "test.kandidaat@example.com");
            return new MeProfile
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                FirstName = first,
                LastName = last,
                FullName = "Test Kandidaat",
                Email = "test.kandidaat@example.com",
                Preferences = new CandidatePreferences(),
            };
        }

        public string ServerAddress { get; private set; } = "";

        public string EnsureServerAddress()
        {
            if (string.IsNullOrWhiteSpace(ServerAddress))
            {
                _ = CreateClient();
            }

            return ServerAddress;
        }

        internal static BrowserNewContextOptions ContextOptions(bool mobile)
            => new()
            {
                ViewportSize = new() { Width = mobile ? 390 : 1280, Height = mobile ? 844 : 900 },
                IsMobile = mobile,
                HasTouch = mobile,
                Locale = "nl-NL",
            };

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiBaseUrl"] = "https://lobsy-acc-api.test/",
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
                    ["Onboarding:RustigEnabled"] = "true",
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
                        BaseAddress = new Uri("https://lobsy-acc-api.test/"),
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
                => Task.FromResult<string?>("Amsterdam");
        }

        private sealed class StubFlags : IFeatureFlags
        {
            public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
                => ValueTask.FromResult(new FeatureFlagSnapshot(
                    EmployersEnabled: true,
                    CandidatePassportEnabled: false,
                    OnboardingFinaleV2Enabled: true));

            public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
                => (await GetAsync(cancellationToken)).IsEnabled(feature);

            public void Invalidate() { }
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
                            new Claim(ClaimTypes.Name, "Acc behavior E2E"),
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

        private sealed class StubHttpFactory(AccWebFactory factory) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) =>
                new(new StubApiHandler(factory), disposeHandler: false) { BaseAddress = new Uri("https://lobsy-acc-api.test/") };
        }

        private sealed class StubApiHandler(AccWebFactory factory) : HttpMessageHandler
        {
            private static readonly JsonSerializerOptions Json = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(30, cancellationToken);
                var path = request.RequestUri?.AbsolutePath ?? "";

                if (path.Contains("/test-ai-consent", StringComparison.OrdinalIgnoreCase) && request.Method == HttpMethod.Post)
                {
                    factory.StoredProfile.TestAiConsentAt = DateTime.UtcNow;
                    factory.StoredProfile.TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion;
                    return JsonResponse(factory.StoredProfile);
                }

                if (path.Contains("/onboarding/complete", StringComparison.OrdinalIgnoreCase) && request.Method == HttpMethod.Post)
                {
                    factory.OnboardingComplete = true;
                    return JsonResponse(OnboardingDto());
                }

                if (path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase)
                    && !path.Contains("dream-job", StringComparison.OrdinalIgnoreCase)
                    && request.Method == HttpMethod.Put)
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
                    await MergeProfilePutAsync(request, cancellationToken);
                    return JsonResponse(factory.StoredProfile);
                }

                if (path.Contains("/competencies", StringComparison.OrdinalIgnoreCase))
                {
                    if (request.Method == HttpMethod.Put
                        && factory.StoredProfile.TestAiConsentAt is null)
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent(
                                JsonSerializer.Serialize(new { message = CandidateConsentRules.TestConsentRequiredMessage }, Json),
                                System.Text.Encoding.UTF8,
                                "application/json"),
                        };
                    }

                    if (request.Method == HttpMethod.Put)
                    {
                        await MergeAssessmentPutAsync(request, factory.CompetencyAnswers, cancellationToken);
                    }

                    return JsonResponse(new { answers = factory.CompetencyAnswers, quickScanVersion = 1 });
                }

                if (path.Contains("/career-interests", StringComparison.OrdinalIgnoreCase))
                {
                    if (request.Method == HttpMethod.Put)
                    {
                        await MergeAssessmentPutAsync(request, factory.CareerAnswers, cancellationToken);
                    }

                    return JsonResponse(new { answers = factory.CareerAnswers, quickScanVersion = 1 });
                }

                if (path.Contains("/culture", StringComparison.OrdinalIgnoreCase))
                {
                    if (request.Method == HttpMethod.Put)
                    {
                        await MergeAssessmentPutAsync(request, factory.CultureAnswers, cancellationToken);
                    }

                    return JsonResponse(new { answers = factory.CultureAnswers, quickScanVersion = 1 });
                }

                if (path.Contains("/values", StringComparison.OrdinalIgnoreCase))
                {
                    if (request.Method == HttpMethod.Put)
                    {
                        await MergeAssessmentPutAsync(request, factory.ValuesAnswers, cancellationToken);
                    }

                    return JsonResponse(new { answers = factory.ValuesAnswers, quickScanVersion = 1 });
                }

                object payload = path switch
                {
                    var p when p.Contains("/onboarding", StringComparison.OrdinalIgnoreCase) => OnboardingDto(),
                    var p when p.Contains("/profile", StringComparison.OrdinalIgnoreCase) => factory.StoredProfile,
                    _ => new { },
                };

                return JsonResponse(payload);

                OnboardingState OnboardingDto() => new()
                {
                    CurrentStep = factory.SavedStep,
                    StartedAtUtc = DateTime.UtcNow,
                    IsComplete = factory.OnboardingComplete,
                    ShouldShow = true,
                    WizardVersion = 2,
                    FinishReached = false,
                    Steps = [],
                };
            }

            private async Task MergeProfilePutAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                var root = doc.RootElement;
                if (root.TryGetProperty("preferences", out var prefs) && prefs.ValueKind == JsonValueKind.Object)
                {
                    if (prefs.TryGetProperty("preferredTransport", out var transport))
                    {
                        factory.StoredProfile.Preferences.PreferredTransport = transport.GetString();
                    }
                }
            }

            private static async Task MergeAssessmentPutAsync(
                HttpRequestMessage request,
                Dictionary<string, int> target,
                CancellationToken cancellationToken)
            {
                using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                if (!doc.RootElement.TryGetProperty("answers", out var answers) || answers.ValueKind != JsonValueKind.Object)
                {
                    return;
                }

                foreach (var prop in answers.EnumerateObject())
                {
                    if (prop.Value.TryGetInt32(out var value) && value is >= 1 and <= 5)
                    {
                        target[prop.Name] = value;
                    }
                }
            }

            private static HttpResponseMessage JsonResponse(object payload)
                => new(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload, Json), System.Text.Encoding.UTF8, "application/json"),
                };
        }
    }
}
