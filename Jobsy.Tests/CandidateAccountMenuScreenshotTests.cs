using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Jobsy.Core.Features;
using Jobsy.Tests.E2e;
using Jobsy.Tests.TestSupport;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
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

/// <summary>Evidence for slim candidate account menu (local fake identity only).</summary>
public class CandidateAccountMenuScreenshotTests
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public async Task Capture_slim_candidate_account_menu_and_settings()
    {
        var artifacts = "/opt/cursor/artifacts";
        Directory.CreateDirectory(artifacts);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var web = new MenuCaptureWebFactory();
        var baseUrl = web.EnsureServerAddress();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await CaptureMenuAsync(browser, baseUrl, artifacts, 390, 844, "candidate-account-menu-390.png");
        await CaptureMenuAsync(browser, baseUrl, artifacts, 1440, 900, "candidate-account-menu-1440.png");
        await CaptureSettingsAsync(browser, baseUrl, artifacts, 390, 844, "candidate-settings-390.png");
    }

    private static async Task CaptureMenuAsync(
        IBrowser browser,
        string baseUrl,
        string artifacts,
        int width,
        int height,
        string fileName)
    {
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        var signIn = baseUrl + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start");
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page, "app.min.css");
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        await page.EvaluateAsync(
            """
            () => {
              const menu = document.querySelector('.account-menu');
              const btn = document.querySelector('.account-menu__toggle');
              if (btn) {
                btn.click();
              } else if (menu) {
                menu.classList.add('is-open');
                document.documentElement.setAttribute('data-header-menu', 'account');
              }
            }
            """);
        await page.Locator(".account-menu.is-open .account-menu__panel").WaitForAsync(new()
        {
            State = WaitForSelectorState.Attached,
            Timeout = 15_000
        });
        await page.WaitForTimeoutAsync(300);

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifacts, fileName),
            FullPage = false
        });
    }

    private static async Task CaptureSettingsAsync(
        IBrowser browser,
        string baseUrl,
        string artifacts,
        int width,
        int height,
        string fileName)
    {
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        var signIn = baseUrl + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/settings?pane=mail");
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page, "app.min.css");
        await page.Locator(".cand-settings").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(400);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifacts, fileName),
            FullPage = true
        });
    }

    private sealed class MenuCaptureWebFactory : WebWebApplicationFactory
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
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
                    ["Jobsy:MaintenancePollSeconds"] = "0"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.AddSingleton<IStartupFilter>(new FakeCandidateSignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new StubFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubFactory());
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
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

    private sealed class FakeCandidateSignInFilter : IStartupFilter
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
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
                        new Claim(ClaimTypes.Role, "Candidate"),
                        new Claim(ClaimTypes.GivenName, "Demo"),
                        new Claim(ClaimTypes.Surname, "Kandidaat"),
                        new Claim(ClaimTypes.Email, "demo.kandidaat@jobsy.local"),
                        new Claim(ClaimTypes.Name, "Demo Kandidaat")
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

    private sealed class StubFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: false,
                CandidatePassportEnabled: true));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class StubFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler(), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            object payload = path.Contains("/profile", StringComparison.OrdinalIgnoreCase)
                ? new { firstName = "Demo", lastName = "Kandidaat", email = "demo.kandidaat@jobsy.local" }
                : path.Contains("/site/status", StringComparison.OrdinalIgnoreCase)
                    ? new { maintenance = false }
                    : path.Contains("/notifications", StringComparison.OrdinalIgnoreCase)
                        ? Array.Empty<object>()
                        : new { ok = true };

            var json = JsonSerializer.Serialize(payload, Json);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
