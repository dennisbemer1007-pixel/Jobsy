using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
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
using System.Net;
using System.Text;
using Jobsy.Tests.TestSupport;

namespace Jobsy.Tests;

/// <summary>K1: empty vacancy state (werkgever) and kandidatenkaart-instellingen without JsonException.</summary>
[Collection("BanenkaartPlaywright")]
public sealed class KandidatenkaartK1PlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private KandidatenkaartK1WebFactory? _web;
    private string _baseUrl = "";

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new KandidatenkaartK1WebFactory();
        _baseUrl = _web.ServerAddress.TrimEnd('/');
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        if (_web is not null)
        {
            await _web.DisposeAsync();
        }
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Employer_kandidatenkaart_shows_vacancy_empty_state(int width, int height)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            _baseUrl + "/__test/sign-in?role=employer&return=" + Uri.EscapeDataString("/werkgever/kandidatenkaart"),
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForURLAsync("**/werkgever/kandidatenkaart**", new() { Timeout = 60_000 });
        await page.WaitForSelectorAsync("[data-kk-no-vacancy=\"1\"]", new() { Timeout = 60_000 });
        var cta = page.Locator("[data-kk-no-vacancy=\"1\"] a[href*=\"/werkgever/vacatures/nieuw\"]");
        Assert.True(await cta.CountAsync() > 0);
        Assert.Equal(0, await page.Locator("text=Even iets misgegaan").CountAsync());
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Candidate_kandidatenkaart_settings_loads_invite_preference(int width, int height)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            _baseUrl + "/__test/sign-in?role=candidate&return="
                      + Uri.EscapeDataString("/candidate/kandidatenkaart-instellingen"),
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForURLAsync("**/candidate/kandidatenkaart-instellingen**", new() { Timeout = 60_000 });
        await page.WaitForSelectorAsync("button.is-on", new() { Timeout = 60_000 });
        Assert.Equal(0, await page.Locator("text=Even iets misgegaan").CountAsync());
    }
}

internal sealed class KandidatenkaartK1WebFactory : WebWebApplicationFactory
{
    public string ServerAddress { get; private set; } = "";
    private IHost? _kestrel;

    public KandidatenkaartK1WebFactory()
    {
        _ = CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiBaseUrl"] = "http://api.test/",
                ["Landing:ForceVariant"] = "on",
                ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
                ["Jobsy:CandidateCompactHeroEnabled"] = "true"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.AddSingleton<IStartupFilter>(new KkSignInStartupFilter());
            services.RemoveAll<IFeatureFlags>();
            services.AddSingleton<IFeatureFlags>(new KkOnFlags());
            services.RemoveAll<IHttpClientFactory>();
            services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory());
            services.RemoveAll<JobsyApiClient>();
            services.AddScoped(_ => new JobsyApiClient(
                new HttpClient(new KkStubApiHandler(), disposeHandler: false)
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

    private sealed class KkOnFlags : IFeatureFlags
    {
        private readonly FeatureFlagSnapshot _snap = new(
            EmployersEnabled: true,
            CandidatePassportEnabled: true,
            KandidatenkaartEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(_snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(_snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new KkStubApiHandler(), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class KkSignInStartupFilter : IStartupFilter
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

                    var roleParam = ctx.Request.Query["role"].ToString();
                    var (role, displayName) = roleParam.ToLowerInvariant() switch
                    {
                        "employer" or "werkgever" => ("BranchManager", "Ondernemer Demo"),
                        _ => ("Candidate", "Sanne de Vries")
                    };

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new(ClaimTypes.Role, role),
                        new(ClaimTypes.Name, displayName),
                        new(JobsyClaimTypes.MfaVerified, "1")
                    };

                    if (role == "BranchManager")
                    {
                        claims.Add(new Claim(JobsyClaimTypes.CompanyIds, IllustratedPagesApiStub.SampleCompanyId.ToString("D")));
                    }

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = role == "BranchManager" ? "/werkgever/sollicitaties" : "/candidate/paspoort";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class KkStubApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var pathAndQuery = path + (request.RequestUri?.Query ?? "");

            if (path.Contains("/api/employer/kandidatenkaart/vacancies", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(Json("[]"));
            }

            if (path.Contains("/api/me/kandidatenkaart/settings", StringComparison.OrdinalIgnoreCase)
                && request.Method == HttpMethod.Get)
            {
                return Task.FromResult(Json("""{"invitePreference":"Normal","showReAvailabilityPrompt":false}"""));
            }

            var json = IllustratedPagesApiStub.BuildJson(pathAndQuery, emptyAppsAndLikes: false, minimalPassportProfile: false);
            return Task.FromResult(Json(json));
        }

        private static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK)
            => new(code)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
    }
}
