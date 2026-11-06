using System.Net;
using System.Security.Claims;
using System.Text.Json;
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
using Microsoft.Playwright;
using Jobsy.Tests.TestSupport;

namespace Jobsy.Tests;

/// <summary>Styled admin Functies page captures (admin-flags-r5) via real Web host + CSS.</summary>
[Collection("PlaywrightSmoke")]
public sealed class AdminFlagsPlaywrightTests : IAsyncLifetime
{
    private static readonly string OutDir =
        Environment.GetEnvironmentVariable("ADMIN_FLAGS_OUT_DIR")
        ?? "/opt/cursor/artifacts/admin-flags-r5";
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
    public async Task Capture_admin_instellingen_desktop_mobile_and_sensitive_toggle()
    {
        Assert.NotNull(_browser);
        using var web = new AdminFlagsWebFactory();
        var baseUrl = web.EnsureServerAddress();
        var signIn = baseUrl
                     + "/__test/sign-in?role=Admin&email=admin@jobsy.local&return="
                     + Uri.EscapeDataString("/admin/instellingen");

        await CaptureAsync(signIn, baseUrl + "/admin/instellingen", 1440, "admin-settings-desktop-1440.png");
        var mobileHeight = await CaptureAsync(signIn, baseUrl + "/admin/instellingen", 390, "admin-settings-mobile-390.png");
        Assert.True(mobileHeight < 6700, $"Mobile page height {mobileHeight}px should be under 6700px (r2 mobile was ~15400px).");

        await using var ctx = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync(baseUrl + "/admin/instellingen", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForSelectorAsync("button.admin-switch", new() { Timeout = 45000, State = WaitForSelectorState.Visible });
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, "flag-before-sensitive-off-1440.png"),
            FullPage = true
        });

        var sensitiveRow = page.Locator(".admin-toggle-row").Filter(new()
        {
            HasText = "Gevoelige werkervaring"
        }).First;
        var toggle = sensitiveRow.Locator("button.admin-switch").First;
        await toggle.ClickAsync();
        await page.WaitForTimeoutAsync(400);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, "flag-after-sensitive-on-1440.png"),
            FullPage = true
        });

        var companyMapRow = page.Locator(".admin-toggle-row").Filter(new()
        {
            HasText = "Bedrijvenkaart (kandidaat)"
        }).First;
        await companyMapRow.ScrollIntoViewIfNeededAsync();
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, "flag-before-company-map-1440.png"),
            FullPage = true
        });
        var companyMapToggle = companyMapRow.Locator("button.admin-switch").First;
        await companyMapToggle.ClickAsync();
        await page.WaitForTimeoutAsync(400);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, "flag-after-company-map-off-1440.png"),
            FullPage = true
        });
        await companyMapToggle.ClickAsync();
        await page.WaitForTimeoutAsync(400);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, "flag-after-company-map-on-1440.png"),
            FullPage = true
        });

        var switchCount = await page.Locator("button.admin-switch").CountAsync();
        Assert.True(switchCount >= 5, $"Expected real toggles, found {switchCount}.");
        var rawKey = await page.Locator("text=AdminSettings.").CountAsync();
        Assert.Equal(0, rawKey);
    }

    private async Task<int> CaptureAsync(string signInUrl, string pageUrl, int width, string fileName)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = 900 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(signInUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync(pageUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForSelectorAsync("button.admin-switch", new() { Timeout = 45000, State = WaitForSelectorState.Visible });
        var height = await page.EvaluateAsync<int>("() => document.documentElement.scrollHeight");
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, fileName),
            FullPage = true
        });
        return height;
    }

    private sealed class AdminFlagsWebFactory : WebWebApplicationFactory
    {
        private IHost? _kestrel;

        public string ServerAddress { get; private set; } = "";

        public AdminFlagsWebFactory()
        {
            _ = CreateClient();
        }

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
                    ["Lobsy:DeploymentEnvironment"] = "Lokaal"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new AdminSignInFilter());
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
            => new(new StubApiHandler(SampleFeatures()), disposeHandler: false)
            {
                BaseAddress = new Uri("http://api.test/")
            };

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _kestrel?.Dispose();
            }

            base.Dispose(disposing);
        }

        private static PlatformFeatureItem SampleFeatures() => new()
        {
            VacancyContentModerationEnabled = true,
            EmployersEnabled = true,
            CandidatePassportEnabled = true,
            SensitiveExperienceEnabled = false,
            TrainingPartnersEnabled = true,
            KandidatenkaartEnabled = false,
            EmployerFitQuestionnairesEnabled = true,
            ResearchConsentEnabled = false,
            TestEditModeEnabled = false,
            SamplePdfEnabled = true,
            PublicWebBaseUrl = "http://localhost:5201",
            UpdatedAtUtc = DateTime.UtcNow,
            AdminOverriddenFlagKeys = ["TrainingPartnersEnabled", "SamplePdfEnabled"]
        };
    }

    private sealed class AdminSignInFilter : IStartupFilter
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

                    var role = ctx.Request.Query["role"].ToString();
                    if (string.IsNullOrWhiteSpace(role))
                    {
                        role = "Admin";
                    }

                    var email = ctx.Request.Query["email"].ToString();
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        email = "admin@jobsy.local";
                    }

                    var identity = new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, role),
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Name, "Demo Admin"),
                        new Claim(JobsyClaimTypes.MfaVerified, "1")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    ctx.Response.Redirect(string.IsNullOrWhiteSpace(ret) ? "/admin/instellingen" : ret);
                });
                next(app);
            };
    }

    private sealed class StubHttpClientFactory(AdminFlagsWebFactory factory) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => factory.CreateApiClient();
    }

    private sealed class StubApiHandler(PlatformFeatureItem features) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("platform-features", StringComparison.OrdinalIgnoreCase))
            {
                var json = JsonSerializer.Serialize(features);
                return Ok(json);
            }

            if (path.Contains("admin/audit", StringComparison.OrdinalIgnoreCase))
            {
                return Ok("""{"items":[],"totalCount":0}""");
            }

            if (path.Contains("session-security", StringComparison.OrdinalIgnoreCase))
            {
                return Ok("""{"inactivityTimeoutMinutes":30}""");
            }

            if (path.Contains("feature-flags", StringComparison.OrdinalIgnoreCase))
            {
                return Ok("""{"employersEnabled":true,"candidatePassportEnabled":true}""");
            }

            if (path.Contains("site/legal", StringComparison.OrdinalIgnoreCase))
            {
                return Ok("""{"companyName":"Lobsy","addressLine1":"Test","postalCode":"1234AB","city":"Amsterdam","countryCode":"NL","email":"info@lobsy.nl","phone":""}""");
            }

            if (path.Contains("site/status", StringComparison.OrdinalIgnoreCase))
            {
                return Ok("""{"maintenanceEnabled":false,"maintenanceExpectedEndUtc":null}""");
            }

            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                return Ok("{}");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static Task<HttpResponseMessage> Ok(string json)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
    }
}
