using System.Net;
using System.Security.Claims;
using Jobsy.Core.Authorization;
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
using Microsoft.Extensions.Http;
using Microsoft.Playwright;
using Jobsy.Tests.TestSupport;

namespace Jobsy.Tests;

/// <summary>
/// PR evidence for admin UI polish — run with <c>ADMIN_UI_OUT_DIR=/opt/cursor/artifacts/admin-ui-after</c>.
/// Same Kestrel + <c>/__test/sign-in</c> pattern as <see cref="AdminDashboardStylesTests"/>.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class AdminUiPolishPlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        var outDir = Environment.GetEnvironmentVariable("ADMIN_UI_OUT_DIR");
        if (string.IsNullOrWhiteSpace(outDir))
        {
            return;
        }

        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        Directory.CreateDirectory(outDir);
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
    public async Task Capture_admin_ui_polish_pages_when_out_dir_set()
    {
        var outDir = Environment.GetEnvironmentVariable("ADMIN_UI_OUT_DIR");
        if (string.IsNullOrWhiteSpace(outDir))
        {
            return;
        }

        Assert.NotNull(_browser);
        using var web = new AdminUiPolishWebFactory();
        var baseUrl = web.EnsureServerAddress();

        (string route, string file, string waitSelector)[] pages =
        [
            ("/admin/scholen", "admin-ui-after-scholen-list-1440.png", ".admin-page"),
            ("/admin/scholen/rapportage", "admin-ui-after-scholen-rapportage-1440.png", ".sch-kpi-row, .admin-page"),
            ("/admin/organisaties/regios", "admin-ui-after-regios-1440.png", ".cname-admin, .admin-page"),
            ("/admin/research", "admin-ui-after-research-1440.png", ".admin-page"),
            ("/admin/vacatures/ats", "admin-ui-after-ats-import-1440.png", ".admin-page"),
            ("/admin/werkgeververificatie", "admin-ui-after-werkgeververificatie-1440.png", ".filter-bar"),
            ("/admin/vacatures/categorieen", "admin-ui-after-categorien-1440.png", ".admin-page"),
            ("/admin/instellingen/algemeen", "admin-ui-after-instellingen-algemeen-1440.png", ".admin-settings-layout"),
            ("/admin/instellingen", "admin-ui-after-instellingen-functies-1440.png", ".admin-settings-layout"),
        ];

        var browser = _browser ?? throw new InvalidOperationException("Browser not initialized.");
        var captured = 0;
        foreach (var (route, file, waitSelector) in pages)
        {
            if (await TryCaptureRouteAsync(browser, baseUrl, outDir, route, file, waitSelector))
            {
                captured++;
            }
        }

        if (await TryCaptureSchoolDrawerAsync(browser, baseUrl, outDir))
        {
            captured++;
        }

        if (await TryCaptureCnameDrawerAsync(browser, baseUrl, outDir))
        {
            captured++;
        }

        Assert.True(captured >= 5, $"Expected at least 5 captures in {outDir}, got {captured}.");
    }

    private static async Task<bool> TryCaptureRouteAsync(IBrowser browser, string baseUrl, string outDir, string route, string file, string waitSelector)
    {
        try
        {
            await CaptureRouteAsync(browser, baseUrl, route, outDir, file, waitSelector);
            return true;
        }
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
        {
            return false;
        }
    }

    private static async Task CaptureRouteAsync(IBrowser browser, string baseUrl, string route, string outDir, string file, string waitSelector)
    {
        var signIn = baseUrl + "/__test/sign-in?role=Admin&return=" + Uri.EscapeDataString(route);
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync(baseUrl + route, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForSelectorAsync(waitSelector, new() { Timeout = 60_000 });
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, file),
            FullPage = true
        });
    }

    private static async Task<bool> TryCaptureSchoolDrawerAsync(IBrowser browser, string baseUrl, string outDir)
    {
        try
        {
            await CaptureSchoolDrawerAsync(browser, baseUrl, outDir);
            return true;
        }
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
        {
            return false;
        }
    }

    private static async Task CaptureSchoolDrawerAsync(IBrowser browser, string baseUrl, string outDir)
    {
        var route = "/admin/scholen";
        var signIn = baseUrl + "/__test/sign-in?role=Admin&return=" + Uri.EscapeDataString(route);
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync(baseUrl + route, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        var addBtn = page.Locator(".sch-admin-toolbar button, .admin-page button").First;
        await addBtn.ClickAsync(new() { Timeout = 30_000 });
        await page.WaitForSelectorAsync(".ent-drawer.is-open, .ent-drawer:not([hidden])", new() { Timeout = 30_000 });
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, "admin-ui-after-school-modal-1440.png"),
            FullPage = false
        });
    }

    private static async Task<bool> TryCaptureCnameDrawerAsync(IBrowser browser, string baseUrl, string outDir)
    {
        try
        {
            await CaptureCnameDrawerAsync(browser, baseUrl, outDir);
            return true;
        }
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException)
        {
            return false;
        }
    }

    private static async Task CaptureCnameDrawerAsync(IBrowser browser, string baseUrl, string outDir)
    {
        var route = "/admin/organisaties/regios";
        var signIn = baseUrl + "/__test/sign-in?role=Admin&return=" + Uri.EscapeDataString(route);
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync(baseUrl + route, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        var createBtn = page.Locator(".cname-admin .btn-compact--primary, .inline-actions .btn-compact--primary").First;
        await createBtn.ClickAsync(new() { Timeout = 30_000 });
        await page.WaitForSelectorAsync(".admin-drawer.is-open", new() { Timeout = 30_000 });
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, "admin-ui-after-regio-host-modal-1440.png"),
            FullPage = false
        });
    }

    private sealed class AdminUiPolishWebFactory : WebWebApplicationFactory
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
                    ["Lobsy:DeploymentEnvironment"] = "Acceptatie"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new AdminUiPolishSignInFilter());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(this));
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(_ => new JobsyApiClient(CreateApiClient()));
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

        internal HttpClient CreateApiClient()
            => new(new PermissiveStubApiHandler(), disposeHandler: false)
            {
                BaseAddress = new Uri("http://api.test/")
            };

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _kestrel?.StopAsync().GetAwaiter().GetResult();
                _kestrel?.Dispose();
            }

            base.Dispose(disposing);
        }

        private sealed class StubHttpClientFactory(AdminUiPolishWebFactory factory) : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => factory.CreateApiClient();
        }

        private sealed class PermissiveStubApiHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? "";
                if (path.Contains("platform-features", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok("""{"vacancyContentModerationEnabled":true,"employersEnabled":true,"candidatePassportEnabled":true,"researchConsentEnabled":false,"schoolsEnabled":true,"candidateCoachTonePreferencesEnabled":true,"updatedAtUtc":"2026-01-01T00:00:00Z"}""");
                }

                if (path.Contains("session-security", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok("""{"inactivityTimeoutMinutes":30}""");
                }

                if (path.Contains("site/status", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok("""{"maintenanceEnabled":false,"maintenanceExpectedEndUtc":null}""");
                }

                if (path.Contains("site/legal", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok("""{"companyName":"Lobsy","addressLine1":"Test","postalCode":"1234AB","city":"Amsterdam","countryCode":"NL","email":"info@lobsy.nl","phone":""}""");
                }

                if (path.Contains("admin/research/settings", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok("""{"researchConsentEnabled":false}""");
                }

                if (path.Contains("admin/research/dashboard", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok("""{"rows":[]}""");
                }

                if (path.Contains("admin/research/pilots", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok("[]");
                }

                if (request.Method == HttpMethod.Get)
                {
                    return Ok("[]");
                }

                return Ok("{}");
            }

            private static Task<HttpResponseMessage> Ok(string json)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                });
        }

        private sealed class AdminUiPolishSignInFilter : IStartupFilter
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
                            new Claim(ClaimTypes.Role, JobsyRoles.Admin),
                            new Claim(ClaimTypes.Email, "admin@jobsy.local"),
                            new Claim(ClaimTypes.Name, "Demo Admin"),
                            new Claim(JobsyClaimTypes.MfaVerified, "1")
                        ],
                        CookieAuthenticationDefaults.AuthenticationScheme);
                        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                        var ret = ctx.Request.Query["return"].ToString();
                        ctx.Response.Redirect(string.IsNullOrWhiteSpace(ret) ? "/admin" : ret);
                    });
                    next(app);
                };
        }
    }
}
