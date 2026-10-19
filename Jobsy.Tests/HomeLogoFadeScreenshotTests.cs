using System.Text;
using Jobsy.Core.Features;
using Jobsy.Web.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class HomeLogoFadeScreenshotTests : IAsyncLifetime
{
    private const string OutDir = "/opt/cursor/artifacts/home-logo-fade";

    private HomeLogoFadeWebFactory? _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _baseUrl = "";

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _factory = new HomeLogoFadeWebFactory();
        _baseUrl = _factory.EnsureServerAddress();
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
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task Capture_home_logo_and_illustration_fades()
    {
        Directory.CreateDirectory(OutDir);

        await CaptureFullPageAsync(1440, 900, "full-1440x900");
        await CaptureFullPageAsync(1024, 768, "full-1024x768");
        await CaptureFullPageAsync(390, 844, "full-390x844", mobile: true);

        await CaptureCropAsync(1024, 768, ".pub-header__brand", "crop-header-logo-1024");
        await CaptureCropAsync(390, 844, ".pub-header__brand", "crop-header-logo-390", mobile: true);

        await CaptureCropAsync(1024, 768, "#top.pub-landing__hero", "crop-hero-edges-1024");
        await CaptureCropAsync(1024, 768, ".pub-landing__step-art >> nth=0", "crop-step-art-edges-1024");
        await CaptureCropAsync(1024, 768, ".pub-landing__kaart-ill-art", "crop-kaart-art-edges-1024", scroll: true);
        await CaptureCropAsync(1024, 768, ".pub-landing__emp-art", "crop-emp-art-edges-1024", scroll: true);
        await CaptureCropAsync(390, 844, ".pub-landing__hero-art-band", "crop-hero-mob-art-390", mobile: true);

        var logoHeight = await MeasureLogoHeightAsync(1024, 768);
        Assert.InRange(logoHeight, 34, 42);
    }

    private async Task CaptureFullPageAsync(int w, int h, string name, bool mobile = false)
    {
        await using var context = await NewContextAsync(w, h, mobile);
        var page = await OpenHomeAsync(context);
        await page.WaitForTimeoutAsync(900);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, $"{name}.png"),
            FullPage = true
        });
    }

    private async Task CaptureCropAsync(int w, int h, string selector, string name, bool mobile = false, bool scroll = false)
    {
        await using var context = await NewContextAsync(w, h, mobile);
        var page = await OpenHomeAsync(context);
        var loc = page.Locator(selector).First;
        await loc.WaitForAsync(new() { Timeout = 60_000 });
        if (scroll)
        {
            await loc.ScrollIntoViewIfNeededAsync();
        }

        await page.WaitForTimeoutAsync(600);
        await loc.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"{name}.png") });
    }

    private async Task<double> MeasureLogoHeightAsync(int w, int h)
    {
        await using var context = await NewContextAsync(w, h);
        var page = await OpenHomeAsync(context);
        return await page.EvaluateAsync<double>(
            """
            () => {
              const el = document.querySelector('.pub-header__logo');
              if (!el) return 0;
              return el.getBoundingClientRect().height;
            }
            """);
    }

    private async Task<IPage> OpenHomeAsync(IBrowserContext context)
    {
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_baseUrl}/?lang=nl", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await page.Locator(".pub-landing--calm .pub-landing__hero--illustrated h1").WaitForAsync(new() { Timeout = 60_000 });
        return page;
    }

    private async Task<IBrowserContext> NewContextAsync(int w, int h, bool mobile = false)
    {
        return await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = w, Height = h },
            Locale = "nl-NL",
            IsMobile = mobile,
            HasTouch = mobile,
        });
    }

    private sealed class HomeLogoFadeWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
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
                    ["Landing:ForceVariant"] = "on",
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmployersSwitch>();
                services.AddSingleton<IEmployersSwitch>(new AlwaysOnEmployersSwitch());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new StubFlags());
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new StubHandler()) { BaseAddress = new Uri("http://api.test/") }));
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

        private sealed class StubFlags : IFeatureFlags
        {
            public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
                => ValueTask.FromResult(new FeatureFlagSnapshot(EmployersEnabled: false, CandidatePassportEnabled: true));

            public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(false);

            public void Invalidate()
            {
            }
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? "";
                if (path.Contains("feature", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("platform", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"employersEnabled":false,"candidatePassportEnabled":true}""", Encoding.UTF8, "application/json")
                    });
                }

                if (path.Contains("landing-stats", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"jobsWithin30Min":38,"medianCommuteMin":14}""", Encoding.UTF8, "application/json")
                    });
                }

                if (path.Contains("landing-price", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"priceEuro":9.95}""", Encoding.UTF8, "application/json")
                    });
                }

                if (path.Contains("site/legal", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"tradeName":"Jobsy","kvk":""}""", Encoding.UTF8, "application/json")
                    });
                }

                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"code":"not_found"}""", Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
