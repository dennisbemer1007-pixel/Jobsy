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
public sealed class FooterMobScreenshotTests : IAsyncLifetime
{
    private static string OutRoot
        => Environment.GetEnvironmentVariable("FOOTER_MOB_OUT") ?? "/opt/cursor/artifacts/footer-mob-r2";

    private static string Phase
        => Environment.GetEnvironmentVariable("FOOTER_MOB_PHASE") ?? "after";

    private FooterMobWebFactory? _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _baseUrl = "";

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _factory = new FooterMobWebFactory();
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
    public async Task Capture_footer_mob_r2_menu_and_footer()
    {
        var dir = Path.Combine(OutRoot, "after");
        Directory.CreateDirectory(dir);

        await CaptureMenuOpenAsync(dir, "360-menu-open", 360, 844);
        await CaptureMenuOpenAsync(dir, "390-menu-open", 390, 844);
        await CaptureFooterAsync(dir, "360-footer", 360, 844);
        await CaptureFooterAsync(dir, "390-footer", 390, 844);
    }

    private async Task CaptureFooterAsync(string dir, string name, int w, int h)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = w, Height = h },
            Locale = "nl-NL",
            IsMobile = true,
            HasTouch = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_baseUrl}/?lang=nl", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await page.Locator(".pub-footer").ScrollIntoViewIfNeededAsync();
        await page.WaitForTimeoutAsync(400);
        await page.Locator(".pub-footer").ScreenshotAsync(new() { Path = Path.Combine(dir, $"{name}.png") });
    }

    private async Task CaptureMenuOpenAsync(string dir, string name, int w, int h)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = w, Height = h },
            Locale = "nl-NL",
            IsMobile = true,
            HasTouch = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_baseUrl}/?lang=nl", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await page.Locator(".pub-header .pub-menu summary").First.ClickAsync();
        var sheetActions = page.Locator(".pub-header details.pub-menu[open] .pub-menu__panel");
        await sheetActions.WaitForAsync(new() { Timeout = 15_000 });
        if (Phase == "after")
        {
            await page.Locator(".pub-header .pub-menu--sheet[open] .pub-menu__actions--pair").WaitForAsync(new() { Timeout = 5_000 });
        }

        await page.WaitForTimeoutAsync(400);
        await page.ScreenshotAsync(new() { Path = Path.Combine(dir, $"{name}.png"), FullPage = false });
    }

    private sealed class FooterMobWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
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
