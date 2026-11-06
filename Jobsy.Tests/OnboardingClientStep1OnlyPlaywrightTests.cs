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

/// <summary>Step 1 only: returning candidate, client island, volgende + reload (acc regression).</summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingClientStep1OnlyPlaywrightTests : IAsyncLifetime
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
    public Task Step1_real_user_volgende_reload(bool mobile, string label)
        => RunStep1UserFlowAsync(_browser!, mobile, capture: false, label);

    [Theory]
    [InlineData(false, "desktop-1280")]
    [InlineData(true, "mobile-390")]
    public Task Capture_step1_screenshots(bool mobile, string label)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_STEP1_ONLY") ?? "").Trim() != "1")
        {
            return Task.CompletedTask;
        }

        return RunStep1UserFlowAsync(_browser!, mobile, capture: true, label);
    }

    private static async Task RunStep1UserFlowAsync(IBrowser browser, bool mobile, bool capture, string label)
    {
        using var web = new Step1WebFactory();
        var root = web.EnsureServerAddress();
        var suggestGets = 0;
        var browserApiHostFetches = 0;
        var step1BffPuts = 0;
        var step1AdvancePuts = 0;
        const string accLikeApiHost = "lobsy-acc-api.test";

        await using var context = await browser.NewContextAsync(Step1WebFactory.ContextOptions(mobile));
        await context.RouteAsync(
            url => url.Contains("/_blazor", StringComparison.OrdinalIgnoreCase),
            route => route.AbortAsync());
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        page.Request += (_, req) =>
        {
            if (req.Url.Contains(accLikeApiHost, StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref browserApiHostFetches);
            }

            if (req.Method == "GET" && req.Url.Contains("/api/onboarding-client/address-suggest", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref suggestGets);
            }

            if (req.Method == "PUT" && req.Url.Contains("/api/onboarding-client/step1", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref step1BffPuts);
            }
        };

        page.RequestFinished += async (_, req) =>
        {
            if (req.Method != "PUT" || !req.Url.Contains("/api/onboarding-client/step1", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var post = req.PostData;
            if (!string.IsNullOrWhiteSpace(post)
                && post.Contains("\"advance\":true", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref step1AdvancePuts);
            }
        };

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await WaitForStep1MountedAsync(page);

        await AssertClientIslandAsync(page);
        if (!mobile)
        {
            await page.Locator(".ob-wizard__sidebar").WaitForAsync(new() { Timeout = 60_000 });
        }

        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_STEP1_ARTIFACTS") ?? "/opt/cursor/artifacts";
        if (capture)
        {
            Directory.CreateDirectory(artifactDir);
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, $"step1-{label}-empty.png"), FullPage = true });
        }

        var first = page.Locator("[data-ob-client=firstName]");
        var last = page.Locator("[data-ob-client=lastName]");
        await Assertions.Expect(first).ToHaveValueAsync("Test");
        await Assertions.Expect(last).ToHaveValueAsync("Kandidaat");

        var chips = page.Locator(".work-status-chips.lobsy-choice-grid .lobsy-choice-tile");
        await chips.First.WaitForAsync(new() { Timeout = 15_000 });
        Assert.True(await chips.CountAsync() >= 2);

        var chipA = chips.Nth(0);
        var chipB = chips.Nth(1);
        await chipA.ScrollIntoViewIfNeededAsync();
        for (var i = 0; i < 10; i++)
        {
            await (mobile ? chipA.TapAsync(new() { Force = true }) : chipA.ClickAsync(new() { Force = true }));
            await page.WaitForTimeoutAsync(50);
            if (await chipA.GetAttributeAsync("aria-pressed") == "true")
            {
                break;
            }
        }

        await Assertions.Expect(chipA).ToHaveAttributeAsync("aria-pressed", "true", new() { Timeout = 5_000 });
        await (mobile ? chipA.TapAsync(new() { Force = true }) : chipA.ClickAsync(new() { Force = true }));
        await Assertions.Expect(chipA).ToHaveAttributeAsync("aria-pressed", "false");
        await chipB.ScrollIntoViewIfNeededAsync();
        for (var i = 0; i < 10; i++)
        {
            await (mobile ? chipB.TapAsync(new() { Force = true }) : chipB.ClickAsync(new() { Force = true }));
            await page.WaitForTimeoutAsync(50);
            if (await chipB.GetAttributeAsync("aria-pressed") == "true")
            {
                break;
            }
        }

        await Assertions.Expect(chipB).ToHaveAttributeAsync("aria-pressed", "true");

        var birth = page.Locator("[data-ob-client=birthDate]");
        await birth.FillAsync("");
        await birth.PressSequentiallyAsync("12031995", new() { Delay = mobile ? 45 : 60 });
        await Assertions.Expect(birth).ToHaveValueAsync("12-03-1995");

        var address = page.Locator("#onboarding-home-address");
        await address.FillAsync("");
        await address.PressSequentiallyAsync("Kal", new() { Delay = 60 });
        await page.WaitForTimeoutAsync(700);
        await address.PressSequentiallyAsync("verstraat 1 Amsterdam", new() { Delay = 40 });
        await Assertions.Expect(address).ToHaveValueAsync("Kalverstraat 1 Amsterdam");

        var suggestBaseline = suggestGets;
        await address.FillAsync("ab");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(suggestBaseline, suggestGets);
        await address.PressSequentiallyAsync("cdef", new() { Delay = 40 });
        await page.WaitForTimeoutAsync(500);
        Assert.True(suggestGets >= suggestBaseline + 1);
        await page.Locator(".address-suggestions__item").First.WaitForAsync(new() { Timeout = 15_000 });

        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, $"step1-{label}-filled-suggest-open.png"), FullPage = true });
        }

        var suggestTop = await page.Locator(".ob-client-suggest").EvaluateAsync<double>("el => el.getBoundingClientRect().top");
        var chipTop = await chipB.EvaluateAsync<double>("el => el.getBoundingClientRect().top");
        Assert.True(suggestTop < chipTop, "Address suggest list should render above status tiles.");
        var suggestItems = page.Locator(".address-suggestions__item");
        if (await suggestItems.CountAsync() >= 4)
        {
            var fourthBottom = await suggestItems.Nth(3).EvaluateAsync<double>("el => el.getBoundingClientRect().bottom");
            Assert.True(fourthBottom <= chipTop + 2, "Fourth suggestion should not sit behind status tiles.");
        }

        await page.Locator(".address-suggestions__item").First.ClickAsync(new() { Timeout = 10_000 });
        Assert.False(string.IsNullOrWhiteSpace(await address.InputValueAsync()));

        await first.FillAsync("Test");
        await last.FillAsync("Kandidaat");

        await page.WaitForTimeoutAsync(700);
        Assert.True(web.ProfilePutCount >= 1, "Debounced step-1 saves should persist profile before Volgende.");
        Assert.True(step1BffPuts >= 1);

        await page.Locator("[data-ob-step-next]").ClickAsync(new() { Timeout = 15_000 });
        await page.WaitForURLAsync("**/candidate/start**", new() { Timeout = 90_000 });
        await page.Locator("[data-ob-client-step=\"2\"]").WaitForAsync(new() { Timeout = 90_000 });
        Assert.True(web.ProfilePutCount >= 1);
        Assert.Equal(1, step1AdvancePuts);
        Assert.Equal(2, web.SavedStep);
        Assert.Equal(0, browserApiHostFetches);

        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, $"step1-{label}-after-volgende-step2.png"), FullPage = true });
        }

        await page.ReloadAsync();
        await page.Locator("[data-ob-client-step=\"2\"]").WaitForAsync(new() { Timeout = 90_000 });

        web.SavedStep = 1;
        await page.GotoAsync(root + "/candidate/start");
        await WaitForStep1MountedAsync(page);

        birth = page.Locator("[data-ob-client=birthDate]");
        address = page.Locator("#onboarding-home-address");
        chipB = page.Locator(".work-status-chips.lobsy-choice-grid .lobsy-choice-tile").Nth(1);

        await Assertions.Expect(birth).ToHaveValueAsync("12-03-1995");
        Assert.False(string.IsNullOrWhiteSpace(await address.InputValueAsync()));
        await Assertions.Expect(chipB).ToHaveAttributeAsync("aria-pressed", "true");

        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, $"step1-{label}-after-reload.png"), FullPage = true });
        }
    }

    private static async Task WaitForStep1MountedAsync(IPage page)
    {
        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync(new() { Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            """() => document.querySelector('[data-ob-client-step="1"]')?.getAttribute('data-ob-client-mounted') === '1'""",
            null,
            new() { Timeout = 90_000 });
    }

    private static async Task AssertClientIslandAsync(IPage page)
    {
        await WaitForStep1MountedAsync(page);
        var hasBlazorBirth = await page.Locator("[data-testid=onboarding-birth-date] .jobsy-date-field__text").CountAsync();
        Assert.Equal(0, hasBlazorBirth);
    }

    private sealed class Step1WebFactory : WebWebApplicationFactory
    {
        private IHost? _kestrel;
        internal int SavedStep { get; set; } = 1;
        internal int ProfilePutCount;
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
            public async Task<IReadOnlyList<AddressSuggestion>> SuggestAsync(string query, CancellationToken cancellationToken = default)
            {
                await Task.Delay(60, cancellationToken);
                return [new AddressSuggestion($"{query} (test)", 52.1, 4.2)];
            }

            public Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
                => Task.FromResult<string?>("2671GB Den Haag");
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
                            new Claim(ClaimTypes.Name, "Step1 E2E"),
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

        private sealed class StubHttpFactory(Step1WebFactory factory) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) =>
                new(new StubApiHandler(factory), disposeHandler: false) { BaseAddress = new Uri("https://lobsy-acc-api.test/") };
        }

        private sealed class StubApiHandler(Step1WebFactory factory) : HttpMessageHandler
        {
            private static readonly JsonSerializerOptions Json = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(50, cancellationToken);
                var path = request.RequestUri?.AbsolutePath ?? "";

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
                    Interlocked.Increment(ref factory.ProfilePutCount);
                    await MergeProfilePutAsync(request, cancellationToken);
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

            private async Task MergeProfilePutAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
                var root = doc.RootElement;
                if (root.TryGetProperty("firstName", out var fn))
                {
                    factory.StoredProfile.FirstName = fn.GetString();
                }

                if (root.TryGetProperty("lastName", out var ln))
                {
                    factory.StoredProfile.LastName = ln.GetString();
                }

                if (root.TryGetProperty("phoneNumber", out var phone))
                {
                    factory.StoredProfile.PhoneNumber = phone.GetString();
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
