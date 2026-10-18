using System.Net;
using System.Security.Claims;
using System.Text;
using Jobsy.Core.Features;
using Jobsy.Web.Features;
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

[Collection("PlaywrightSmoke")]
public sealed class OnboardingFinaleV2ScreenshotTests : IAsyncLifetime
{
    private const string StoreDir = "/cursor/stores/self/finale";
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public enum FinaleOptionalFlags
    {
        AllOff,
        OneOn,
        AllOn
    }

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        Directory.CreateDirectory(StoreDir);
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
    public async Task Capture_finale_v2_r10_flags_off()
    {
        using var web = new FinaleWebFactory(FinaleOptionalFlags.AllOff);
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString("/candidate/start");
        foreach (var (width, height, suffix) in new (int, int, string)[]
                 {
                     (1440, 900, "1440"),
                     (1280, 900, "1280"),
                     (1024, 768, "1024"),
                     (390, 844, "390")
                 })
        {
            await CaptureOneAsync(FinaleOptionalFlags.AllOff, "flags-off", width, height, suffix, baseUrl);
        }
    }

    [Fact]
    public async Task Capture_finale_v2_r9_flags_off()
    {
        using var web = new FinaleWebFactory(FinaleOptionalFlags.AllOff);
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString("/candidate/start");
        foreach (var (width, height, suffix) in new (int, int, string)[]
                 {
                     (1440, 900, "1440"),
                     (1280, 900, "1280"),
                     (1024, 768, "1024"),
                     (390, 844, "390")
                 })
        {
            await CaptureOneAsync(FinaleOptionalFlags.AllOff, "flags-off", width, height, suffix, baseUrl);
        }
    }

    [Fact]
    public async Task Capture_finale_v2_r8_flags_off()
    {
        using var web = new FinaleWebFactory(FinaleOptionalFlags.AllOff);
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString("/candidate/start");
        foreach (var (width, height, suffix) in new (int, int, string)[]
                 {
                     (1440, 900, "1440"),
                     (1280, 900, "1280"),
                     (1024, 768, "1024"),
                     (390, 844, "390")
                 })
        {
            await CaptureOneAsync(FinaleOptionalFlags.AllOff, "flags-off", width, height, suffix, baseUrl);
        }
    }

    [Fact]
    public async Task Capture_finale_v2_r7_flags_off()
    {
        using var web = new FinaleWebFactory(FinaleOptionalFlags.AllOff);
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString("/candidate/start");
        foreach (var (width, height, suffix) in new (int, int, string)[]
                 {
                     (1440, 900, "1440"),
                     (1280, 900, "1280"),
                     (1024, 768, "1024"),
                     (390, 844, "390")
                 })
        {
            await CaptureOneAsync(FinaleOptionalFlags.AllOff, "flags-off", width, height, suffix, baseUrl);
        }
    }

    [Fact]
    public async Task Capture_finale_v2_r6_flags_off_desktop()
    {
        using var web = new FinaleWebFactory(FinaleOptionalFlags.AllOff);
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString("/candidate/start");
        foreach (var (width, height, suffix) in new (int, int, string)[]
                 {
                     (1440, 900, "1440"),
                     (1600, 900, "1600")
                 })
        {
            await CaptureOneAsync(FinaleOptionalFlags.AllOff, "flags-off", width, height, suffix, baseUrl);
        }
    }

    [Fact]
    public async Task Capture_finale_v2_r5_flags_off()
    {
        using var web = new FinaleWebFactory(FinaleOptionalFlags.AllOff);
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString("/candidate/start");
        foreach (var (width, height, suffix) in new (int, int, string)[]
                 {
                     (1440, 900, "1440"),
                     (1280, 900, "1280"),
                     (1024, 768, "1024"),
                     (390, 844, "390")
                 })
        {
            await CaptureOneAsync(FinaleOptionalFlags.AllOff, "flags-off", width, height, suffix, baseUrl);
        }
    }

    [Theory]
    [InlineData(FinaleOptionalFlags.AllOff, "flags-off")]
    [InlineData(FinaleOptionalFlags.OneOn, "flags-one-on")]
    [InlineData(FinaleOptionalFlags.AllOn, "flags-on")]
    public async Task Capture_finale_v2_full_page(FinaleOptionalFlags flags, string tag)
    {
        using var web = new FinaleWebFactory(flags);
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString("/candidate/start");

        foreach (var (width, height, suffix) in new (int, int, string)[] { (1440, 900, "1440"), (390, 844, "390"), (360, 780, "360") })
        {
            await CaptureOneAsync(flags, tag, width, height, suffix, baseUrl);
        }
    }

    private async Task CaptureOneAsync(
        FinaleOptionalFlags flags,
        string tag,
        int width,
        int height,
        string suffix,
        string? baseUrl = null)
    {
        baseUrl ??= new FinaleWebFactory(flags).EnsureServerAddress()
                  + "/__test/sign-in?return="
                  + Uri.EscapeDataString("/candidate/start");

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL",
            IsMobile = width < 600,
            HasTouch = width < 600
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

        var loading = page.Locator(".ob-wizard__loading");
        if (await loading.CountAsync() > 0)
        {
            await loading.WaitForAsync(new()
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 90_000
            });
        }

        await page.Locator("[data-testid=onboarding-finale-v2]").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000
        });
        await page.Locator(".ob-finale h1").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
        await page.Locator(".ob-finale__end").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });

        await page.WaitForFunctionAsync(
            """
            () => {
              const body = document.body?.innerText ?? '';
              if (/Laden…|Laden\.\.\.|Loading/i.test(body)) return false;
              const h1 = document.querySelector('.ob-finale h1');
              if (!h1 || !(h1.textContent ?? '').includes('Kompas')) return false;
              const el = document.querySelector('[data-testid=onboarding-finale-match-bar]');
              if (!el) return false;
              return getComputedStyle(el).backgroundColor === 'rgb(15, 45, 92)';
            }
            """,
            null,
            new() { Timeout = 90_000 });

        await page.WaitForTimeoutAsync(400);

        var pageText = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain("Laden", pageText, StringComparison.OrdinalIgnoreCase);

        var matchBarBg = await page.Locator("[data-testid=onboarding-finale-match-bar]").EvaluateAsync<string>(
            "el => getComputedStyle(el).backgroundColor");
        Assert.True(
            matchBarBg.Equals("rgb(15, 45, 92)", StringComparison.OrdinalIgnoreCase)
            || matchBarBg.Equals("#0f2d5c", StringComparison.OrdinalIgnoreCase),
            $"Match bar background expected navy, got {matchBarBg}.");

        var scrollHeight = await page.EvaluateAsync<int>(
            """
            () => Math.max(
              document.documentElement.scrollHeight,
              document.body.scrollHeight,
              document.querySelector('.app-main')?.scrollHeight ?? 0,
              document.querySelector('.ob-finale')?.scrollHeight ?? 0)
            """);
        Assert.True(scrollHeight > height, $"Expected full-page content taller than viewport ({scrollHeight} vs {height}).");

        var file = $"finale-v2-{tag}-{suffix}.png";
        var path = Path.Combine(StoreDir, file);
        await page.ScreenshotAsync(new() { Path = path, FullPage = true });
        Assert.True(File.Exists(path));
    }

    private sealed class FinaleWebFactory(FinaleOptionalFlags flags) : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
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
                    ["Onboarding:FinaleV2Enabled"] = "true",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.Configure<HostOptions>(o =>
                    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
                services.AddSingleton<IStartupFilter>(new SignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new StubFlags(flags));
                services.RemoveAll<IReferenceVerificationFeatureGate>();
                services.AddSingleton<IReferenceVerificationFeatureGate>(new StubReferenceGate(flags));
                services.RemoveAll<IGeocodingClient>();
                services.AddSingleton<IGeocodingClient>(new StubGeocoder());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubFactory());
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(_ => new JobsyApiClient(
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

    private sealed class StubFlags(FinaleOptionalFlags flags) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
        {
            var (proactive, kandidaten) = flags switch
            {
                FinaleOptionalFlags.AllOn => (true, true),
                FinaleOptionalFlags.OneOn => (true, false),
                _ => (false, false)
            };
            return ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: true,
                CandidatePassportEnabled: true,
                ProactiveMatchingEnabled: proactive,
                KandidatenkaartEnabled: kandidaten));
        }

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class StubReferenceGate(FinaleOptionalFlags flags) : IReferenceVerificationFeatureGate
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(flags == FinaleOptionalFlags.AllOn);
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
                        new Claim(ClaimTypes.Name, "Finale E2E")
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

    private sealed class StubFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler(), disposeHandler: false) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/onboarding/nearby", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Json("""{"count":10,"maxTravelMinutes":30,"preferredTransport":"Fiets","employersFeatureOff":false,"missingHomeLocation":false,"examples":[]}"""));
            }

            if (path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Json(FinishOnboardingJson()));
            }

            if (path.Contains("/profile", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Json("""
                    {"id":"11111111-1111-1111-1111-111111111111","firstName":"Sanne","lastName":"Test","fullName":"Sanne Test","dateOfBirth":"2000-01-01","preferences":{"maxTravelMinutes":30,"homeAddress":"Zaandam"},"homeLatitude":52.44,"homeLongitude":4.83}
                    """));
            }

            if (path.Contains("competenc", StringComparison.OrdinalIgnoreCase)
                || path.Contains("career", StringComparison.OrdinalIgnoreCase)
                || path.Contains("culture", StringComparison.OrdinalIgnoreCase)
                || path.Contains("values", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Json("""{"status":"Draft","answers":{"1":4,"6":3,"11":4,"16":3,"21":4}}"""));
            }

            return Task.FromResult(Json("{}"));
        }

        private static string FinishOnboardingJson()
            => """
               {"currentStep":10,"finishReached":true,"isComplete":false,"wizardVersion":3,"shouldShow":true,"steps":[],"impression":{"label":"Eerste indruk","matchingVacancyCount":10,"strengths":[{"code":"Resultaatgerichtheid","label":"Resultaat","sentence":"Je wilt dingen afmaken en houdt van duidelijk resultaat.","percent":88}],"riasec":[{"code":"Realistic","label":"Realistisch","sentence":"Je past bij praktisch werk met je handen of machines.","percent":70}],"topValue":{"code":"Autonomy","label":"Autonomie","sentence":"Je vindt eigen regie en uitdaging belangrijk.","percent":80},"matchCards":[]}}
               """;

        private static HttpResponseMessage Json(string body)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
    }
}
