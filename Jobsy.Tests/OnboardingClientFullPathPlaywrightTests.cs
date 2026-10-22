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

/// <summary>Happy path steps 2–10 → Kompas with Blazor blocked (step 1: see Step1OnlyPlaywrightTests).</summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingClientFullPathPlaywrightTests : IAsyncLifetime
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
    [InlineData(false, "desktop-1280")]
    [InlineData(true, "mobile-390")]
    public Task Full_path_start_to_kompas(bool mobile, string label)
        => RunFullPathAsync(_browser!, mobile, capture: false, label);

    [Theory]
    [InlineData(false, "desktop-1280")]
    [InlineData(true, "mobile-390")]
    public Task Capture_full_path_screenshots(bool mobile, string label)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_ONBOARDING_FULL") ?? "").Trim() != "1")
        {
            return Task.CompletedTask;
        }

        return RunFullPathAsync(_browser!, mobile, capture: true, label);
    }

    private static async Task RunFullPathAsync(IBrowser browser, bool mobile, bool capture, string label)
    {
        using var web = new FullPathWebFactory();
        var root = web.EnsureServerAddress();
        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_FULL_ARTIFACTS") ?? "/opt/cursor/artifacts/onboarding-full";
        if (capture)
        {
            Directory.CreateDirectory(artifactDir);
        }

        await using var context = await browser.NewContextAsync(FullPathWebFactory.ContextOptions(mobile));
        await context.RouteAsync(
            url => url.Contains("/_blazor", StringComparison.OrdinalIgnoreCase),
            route => route.AbortAsync());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        web.SavedStep = 2;
        web.StoredProfile.DateOfBirth = new DateOnly(1990, 3, 12);
        web.StoredProfile.HasDateOfBirth = true;
        web.StoredProfile.HomeLatitude = 52.1;
        web.StoredProfile.HomeLongitude = 4.2;
        web.StoredProfile.Preferences.HomeAddress = "Kalverstraat 1 Amsterdam";
        web.StoredProfile.Preferences.WorkStatuses = ["student"];
        await page.GotoAsync(root + "/candidate/start");
        await page.Locator(".ob-wizard__loading").WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 120_000 });
        await page.WaitForFunctionAsync("() => typeof window.jobsyOnboardingClient !== 'undefined'", null, new() { Timeout = 60_000 });

        await AdvanceFromStepAsync(page, web, 2, capture, artifactDir, label, async () =>
        {
            await page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile").First.ClickAsync();
        });

        await AdvanceFromStepAsync(page, web, 3, capture, artifactDir, label, async () =>
        {
            await page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile").First.ClickAsync();
            await page.Locator("[data-ob-client=travelGrid] .lobsy-choice-tile").First.ClickAsync();
        });
        await page.Locator("[data-ob-client-step=\"4\"]").WaitForAsync();
        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, $"step4-{label}.png"), FullPage = true });
        }

        await ClickObStepAdvanceAsync(page, 4, skip: true);
        await page.Locator(".ob-wizard__loading").WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 120_000 });
        await page.Locator("[data-ob-client-step=\"5\"]").WaitForAsync();
        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, $"step5-{label}.png"), FullPage = true });
        }

        await ClickObStepAdvanceAsync(page, 5, skip: true);
        await page.Locator(".ob-wizard__loading").WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 120_000 });
        await AdvanceFromStepAsync(page, web, 6, capture, artifactDir, label, async () =>
        {
            await page.Locator("[data-ob-client=dreamInput]").FillAsync("Kassière");
        });
        await CompleteStep7ConsentAsync(page);
        await page.Locator("[data-ob-client-step=\"7\"]").WaitForAsync();
        await AnswerMiniBlockAsync(page);
        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, $"step7-mini-{label}.png"), FullPage = true });
        }

        await ClickObStepAdvanceAsync(page, 7);

        for (var step = 8; step <= 10; step++)
        {
            await page.Locator($"[data-ob-client-step=\"{step}\"]").WaitForAsync(new() { Timeout = 90_000 });
            await AnswerMiniBlockAsync(page);
            if (capture)
            {
                await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, $"step{step}-{label}.png"), FullPage = true });
            }

            await ClickObStepAdvanceAsync(page, step);
        }

        await page.Locator("[data-testid=onboarding-rustig-kompas], [data-testid=onboarding-finale-heroless-ctas]").First
            .WaitForAsync(new() { Timeout = 90_000 });
        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, $"kompas-{label}.png"), FullPage = true });
        }

        var kompasResponse = page.WaitForResponseAsync(r =>
            r.Request.Method == "POST"
            && r.Url.Contains("/api/onboarding-client/kompas/complete", StringComparison.OrdinalIgnoreCase));
        await page.Locator("[data-ob-kompas-complete]").First.ClickAsync();
        var kompasHttp = await kompasResponse;
        Assert.True(kompasHttp.Ok, $"Kompas complete HTTP {(int)kompasHttp.Status}.");
        await page.WaitForURLAsync("**/candidate/profile**", new() { Timeout = 90_000 });
        Assert.True(web.OnboardingComplete);
    }

    private static async Task ClickObStepAdvanceAsync(IPage page, int step, bool skip = false)
    {
        var selector = skip ? "[data-ob-step-skip]" : "[data-ob-step-next]";
        var advanceResponse = page.WaitForResponseAsync(r =>
        {
            if (r.Request.Method != "PUT"
                || !r.Url.Contains($"/api/onboarding-client/steps/{step}", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var body = r.Request.PostData ?? "";
            return skip
                ? body.Contains("\"skipOptional\":true", StringComparison.Ordinal)
                : body.Contains("\"advance\":true", StringComparison.Ordinal);
        });
        await page.Locator(selector).ClickAsync();
        var response = await advanceResponse;
        var responseBody = response.Ok ? "" : await response.TextAsync();
        Assert.True(response.Ok, $"Step {step} BFF {(skip ? "skip" : "advance")} HTTP {(int)response.Status}. {responseBody}");
        await page.WaitForLoadStateAsync(LoadState.Load, new() { Timeout = 90_000 });
    }

    private static async Task CompleteStep7ConsentAsync(IPage page)
    {
        await page.Locator("[data-ob-client-step=\"7\"]").WaitForAsync(new() { Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            """() => document.querySelector('[data-ob-client-step="7"]')?.getAttribute('data-ob-client-mounted') === '1'""",
            null,
            new() { Timeout = 90_000 });
        await page.Locator("[data-ob-client=consent]").CheckAsync();
        var consentResponse = page.WaitForResponseAsync(r =>
            r.Request.Method == "PUT"
            && r.Url.Contains("/api/onboarding-client/steps/7", StringComparison.OrdinalIgnoreCase)
            && (r.Request.PostData ?? "").Contains("\"advance\":true", StringComparison.Ordinal));
        await page.Locator("[data-ob-step-next]").ClickAsync();
        var consentHttp = await consentResponse;
        Assert.True(consentHttp.Ok, $"Step 7 consent HTTP {(int)consentHttp.Status}.");
        await page.WaitForLoadStateAsync(LoadState.Load, new() { Timeout = 90_000 });
        var err = page.Locator("[data-ob-step-error]");
        if (await err.IsVisibleAsync())
        {
            Assert.Fail("Step 7 consent error: " + await err.InnerTextAsync());
        }

        await page.Locator(".ob-wizard__loading").WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 120_000 });
        await page.WaitForFunctionAsync("() => typeof window.jobsyOnboardingClient !== 'undefined'", null, new() { Timeout = 60_000 });
    }

    private static async Task AdvanceFromStepAsync(
        IPage page,
        FullPathWebFactory web,
        int step,
        bool capture,
        string dir,
        string label,
        Func<Task> fill)
    {
        await page.Locator($"[data-ob-client-step=\"{step}\"]").WaitForAsync(new() { Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            $$"""() => document.querySelector('[data-ob-client-step="{{step}}"]')?.getAttribute('data-ob-client-mounted') === '1'""",
            null,
            new() { Timeout = 90_000 });
        await fill();
        await page.WaitForTimeoutAsync(400);
        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(dir, $"step{step}-{label}.png"), FullPage = true });
        }

        await ClickObStepAdvanceAsync(page, step);
        var err = page.Locator("[data-ob-step-error]");
        if (await err.IsVisibleAsync())
        {
            Assert.Fail($"Step {step} Volgende error: " + await err.InnerTextAsync());
        }

        Assert.True(web.SavedStep >= step + 1, $"Expected SavedStep >= {step + 1}, got {web.SavedStep}.");
        await page.Locator(".ob-wizard__loading").WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 120_000 });
        await page.WaitForFunctionAsync("() => typeof window.jobsyOnboardingClient !== 'undefined'", null, new() { Timeout = 60_000 });
        await page.Locator($"[data-ob-client-step=\"{step + 1}\"]").WaitForAsync(new() { Timeout = 90_000 });
    }

    private static async Task AnswerMiniBlockAsync(IPage page)
    {
        var cards = page.Locator(".ob-client-mini-q");
        var count = await cards.CountAsync();
        for (var i = 0; i < count; i++)
        {
            await cards.Nth(i).Locator(".lobsy-choice-tile").Nth(2).ClickAsync();
            await page.WaitForTimeoutAsync(80);
        }

        await page.WaitForTimeoutAsync(500);
    }

    private sealed class FullPathWebFactory : WebWebApplicationFactory
    {
        private IHost? _kestrel;
        internal int SavedStep { get; set; } = 1;
        internal string OnboardingSource { get; set; } = "welcome";
        internal bool FinishReached { get; set; }
        internal bool OnboardingComplete { get; set; }
        internal Dictionary<string, int> CompetencyAnswers { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, int> CareerAnswers { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, int> CultureAnswers { get; } = new(StringComparer.Ordinal);
        internal Dictionary<string, int> ValuesAnswers { get; } = new(StringComparer.Ordinal);
        internal MeProfile StoredProfile { get; private set; } = CreateReturningProfile();

        private static MeProfile CreateReturningProfile()
        {
            var (first, last) = CandidateNameRules.ResolveOnboardingPrefill(
                "Test Kandidaat",
                "Kandidaat",
                "Test Kandidaat",
                "test.kandidaat@example.com");
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
            builder.UseEnvironment("Production");
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
                            new Claim(ClaimTypes.Name, "Full path E2E"),
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

        private sealed class StubHttpFactory(FullPathWebFactory factory) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) =>
                new(new StubApiHandler(factory), disposeHandler: false) { BaseAddress = new Uri("https://lobsy-acc-api.test/") };
        }

        private sealed class StubApiHandler(FullPathWebFactory factory) : HttpMessageHandler
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

                    if (doc.RootElement.TryGetProperty("finishReached", out var fin)
                        && fin.ValueKind is JsonValueKind.True)
                    {
                        factory.FinishReached = true;
                    }

                    if (doc.RootElement.TryGetProperty("source", out var src) && src.ValueKind == JsonValueKind.String)
                    {
                        factory.OnboardingSource = src.GetString() ?? factory.OnboardingSource;
                    }

                    return JsonResponse(OnboardingDto());
                }

                if (path.Contains("/onboarding/dream-job", StringComparison.OrdinalIgnoreCase) && request.Method == HttpMethod.Put)
                {
                    return JsonResponse(new { saved = true });
                }

                if (path.Contains("/profile", StringComparison.OrdinalIgnoreCase) && request.Method == HttpMethod.Put)
                {
                    await MergeProfilePutAsync(request, cancellationToken);
                    return JsonResponse(factory.StoredProfile);
                }

                if (path.Contains("/competencies", StringComparison.OrdinalIgnoreCase))
                {
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
                    var p when p.Contains("/site/status", StringComparison.OrdinalIgnoreCase) => new { maintenance = false },
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
                    Source = factory.OnboardingSource,
                    FinishReached = factory.FinishReached,
                    Steps = [],
                };
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

            private async Task MergeProfilePutAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                var root = doc.RootElement;
                if (root.TryGetProperty("openForWork", out var open) && open.ValueKind is JsonValueKind.True)
                {
                    factory.StoredProfile.OpenForWork = true;
                }

                if (root.TryGetProperty("firstName", out var fn))
                {
                    factory.StoredProfile.FirstName = fn.GetString();
                }

                if (root.TryGetProperty("lastName", out var ln))
                {
                    factory.StoredProfile.LastName = ln.GetString();
                }

                if (root.TryGetProperty("dateOfBirth", out var dob) && dob.ValueKind == JsonValueKind.String
                    && DateOnly.TryParse(dob.GetString(), out var parsed))
                {
                    factory.StoredProfile.DateOfBirth = parsed;
                    factory.StoredProfile.HasDateOfBirth = true;
                }

                if (root.TryGetProperty("homeLatitude", out var lat) && lat.TryGetDouble(out var la))
                {
                    factory.StoredProfile.HomeLatitude = la;
                }

                if (root.TryGetProperty("homeLongitude", out var lng) && lng.TryGetDouble(out var lo))
                {
                    factory.StoredProfile.HomeLongitude = lo;
                }

                if (root.TryGetProperty("preferences", out var prefs) && prefs.ValueKind == JsonValueKind.Object)
                {
                    if (prefs.TryGetProperty("homeAddress", out var ha))
                    {
                        factory.StoredProfile.Preferences.HomeAddress = ha.GetString();
                    }

                    if (prefs.TryGetProperty("workStatuses", out var ws) && ws.ValueKind == JsonValueKind.Array)
                    {
                        factory.StoredProfile.Preferences.WorkStatuses = ws.EnumerateArray()
                            .Select(e => e.GetString())
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(s => s!)
                            .ToList();
                    }
                }

                (factory.StoredProfile.FirstName, factory.StoredProfile.LastName) = CandidateNameRules.ResolveOnboardingPrefill(
                    factory.StoredProfile.FirstName,
                    factory.StoredProfile.LastName,
                    factory.StoredProfile.FullName,
                    factory.StoredProfile.Email);
            }

            private static HttpResponseMessage JsonResponse(object payload)
                => new(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload, Json), System.Text.Encoding.UTF8, "application/json"),
                };
        }
    }
}
