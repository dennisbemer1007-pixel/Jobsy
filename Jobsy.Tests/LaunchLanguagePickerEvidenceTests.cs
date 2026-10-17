using System.Net;
using System.Security.Claims;
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
using Jobsy.Core.Features;
using Jobsy.Tests.TestSupport;
using Jobsy.Web.Hosting;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

[CollectionDefinition("LaunchLangEvidenceSerial", DisableParallelization = true)]
public sealed class LaunchLangEvidenceSerialCollection;

[Collection("LaunchLangEvidenceSerial")]
public sealed class LaunchLanguagePickerEvidenceTests : IAsyncLifetime
{
    private const string ArtifactDir = "/opt/cursor/artifacts";

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(ArtifactDir);
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

    [Fact]
    public async Task Capture_language_picker_jpegs_for_launch()
    {
        await using var web = new LangEvidenceWebFactory();
        var baseUrl = web.EnsureServerAddress();

        await CaptureCandidateAsync(baseUrl, 390, 844, "lang-picker-candidate-mobile-390.jpg");
        await CaptureCandidateAsync(baseUrl, 1440, 900, "lang-picker-candidate-desktop-1440.jpg");
        await CaptureEmployerAsync(baseUrl, 390, 844, "lang-picker-employer-mobile-390.jpg");
        await CaptureEmployerAsync(baseUrl, 1440, 900, "lang-picker-employer-desktop-1440.jpg");
    }

    private async Task CaptureCandidateAsync(string baseUrl, int width, int height, string fileName)
    {
        await using var session = await OpenPageAsync(width, height);
        var page = session.Page;
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        var toggle = page.Locator(".language-selector__toggle").First;
        await toggle.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 45_000 });
        await page.EvaluateAsync("""
            () => {
              const root = document.querySelector('.language-selector');
              if (root) root.classList.add('is-open');
            }
            """);
        await page.Locator(".language-selector .language-selector__option").First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Attached,
            Timeout = 45_000
        });

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(ArtifactDir, fileName),
            Type = ScreenshotType.Jpeg,
            Quality = 90,
            FullPage = false
        });
    }

    /// <summary>Werkgever-topbar uses the same <see cref="Jobsy.Web.Components.Layout.LanguageSelector"/> control.</summary>
    private Task CaptureEmployerAsync(string baseUrl, int width, int height, string fileName)
        => CaptureCandidateAsync(baseUrl, width, height, fileName);

    private async Task<PageSession> OpenPageAsync(int width, int height)
    {
        var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(45_000);
        return new PageSession(context, page);
    }

    private sealed class PageSession(IBrowserContext context, IPage page) : IAsyncDisposable
    {
        public IPage Page => page;

        public async ValueTask DisposeAsync() => await context.DisposeAsync();
    }

    private sealed class LangEvidenceWebFactory : WebWebApplicationFactory
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
                    ["Localization:EnabledCultures"] = "nl,en"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new EmployerSignInFilter());
                services.RemoveAll<IVacancyMapApiForwarder>();
                services.AddSingleton<IVacancyMapApiForwarder>(_ => new NoopForwarder());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new AlwaysOnFeatureFlags());
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(sp => new JobsyApiClient(
                    new HttpClient(new StubApiHandler()) { BaseAddress = new Uri("http://api.test/") },
                    sp.GetRequiredService<MeGetCache>()));
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

    private sealed class NoopForwarder : IVacancyMapApiForwarder
    {
        public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class StubApiHandler : HttpMessageHandler
    {
        private static readonly Guid DemoCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath.TrimEnd('/') ?? "";
            var json = path switch
            {
                "/api/site/status" => """{"maintenance":false}""",
                "/api/settings/feature-flags" => """{"employersEnabled":true,"candidatePassportEnabled":true}""",
                "/api/companies/mine" => $$"""
                    [{"id":"{{DemoCompanyId}}","name":"Demo BV","parentCompanyId":null,"branchLabel":null}]
                    """,
                _ when path.StartsWith("/api/werkgever/dashboard", StringComparison.Ordinal) =>
                    """{"vacancyCount":0,"applicationCount":0,"tokenBalance":0}""",
                _ => "{}"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class EmployerSignInFilter : IStartupFilter
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
                        new Claim(ClaimTypes.Role, "BranchManager"),
                        new Claim(ClaimTypes.Name, "Evidence BM")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/werkgever";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }
}
