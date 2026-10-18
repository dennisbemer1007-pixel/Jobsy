using System.Security.Claims;
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
public sealed class SensitiveExperiencePlaywrightTests : IAsyncLifetime
{
    private const string OutDir = "/opt/cursor/artifacts/gevoelig-r4";
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        Directory.CreateDirectory(OutDir);
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
    public async Task Capture_gevoelig_mockup_screens()
    {
        using var web = new GevoeligWebFactory();
        var baseUrl = web.EnsureServerAddress();
        var signIn = baseUrl + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/gevoelig-demo/werkervaring");
        Assert.NotNull(_browser);

        var screens = new (string Slug, string Path)[]
        {
            ("werkervaring", "/candidate/gevoelig-demo/werkervaring"),
            ("neutrale-omschrijving", "/candidate/gevoelig-demo/edit"),
            ("andere-periode", "/candidate/gevoelig-demo/timeline"),
            ("referenties", "/candidate/gevoelig-demo/referenties"),
            ("werkgeversweergave", "/candidate/gevoelig-demo/paspoort")
        };

        foreach (var (slug, path) in screens)
        {
            foreach (var (suffix, width, height) in new[] { ("1440", 1440, 900), ("390", 390, 844) })
            {
                await using var context = await _browser.NewContextAsync(new()
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height }
                });
                await PlaywrightCookieConsent.AcceptAsync(context);
                var page = await context.NewPageAsync();
                await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle });
                await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.NetworkIdle });
                await PlaywrightCookieConsent.AcceptOnPageAsync(page);
                await page.EvaluateAsync("""
                    () => {
                      document.documentElement.classList.add('gev-capture');
                      document.querySelectorAll('.lobsy-assistant-tab, .lobsy-assistant').forEach(el => {
                        el.style.display = 'none';
                      });
                    }
                    """);
                await page.Locator("[data-testid^='gevoelig-demo-']").WaitForAsync();
                await page.Locator("header, .app-header, .top-bar").First.WaitForAsync(new() { Timeout = 5000 });
                if (width == 1440)
                {
                    var heroBox = await page.Locator(".gev-hero__copy h1").BoundingBoxAsync();
                    var cardBox = await page.Locator(".gev-card, .gev-job").First.BoundingBoxAsync();
                    if (heroBox is not null && cardBox is not null)
                    {
                        Assert.True(Math.Abs(heroBox.X - cardBox.X) < 6,
                            $"{slug}: hero x={heroBox.X} card x={cardBox.X}");
                    }
                }

                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(OutDir, $"{slug}-{suffix}.png"),
                    FullPage = false
                });
            }
        }
    }

    private sealed class GevoeligWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
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
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.Configure<HostOptions>(o =>
                    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
                services.AddSingleton<IStartupFilter>(new SignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags, SensitiveFlags>();
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

    private sealed class StubFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler(), disposeHandler: false) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
    }

    private sealed class SensitiveFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: true,
                CandidatePassportEnabled: true,
                SensitiveExperienceEnabled: true));

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
                        new Claim(ClaimTypes.Name, "Marion Bakker")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/gevoelig-demo/werkervaring";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }
}
