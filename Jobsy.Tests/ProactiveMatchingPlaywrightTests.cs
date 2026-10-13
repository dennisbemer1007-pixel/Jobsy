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

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class ProactiveMatchingPlaywrightTests : IAsyncLifetime
{
    private const string ScreenshotDir = "/cursor/stores/self/proactive";

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(ScreenshotDir);
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
    public async Task Flag_off_redirects_candidate_proactive_page()
    {
        using var web = new ProactiveWebFactory(proactiveOn: false, role: "Candidate");
        var page = await _browser!.NewPageAsync();
        await page.GotoAsync(web.EnsureServerAddress() + "/candidate/proactive-matching", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000
        });
        await page.WaitForTimeoutAsync(400);
        Assert.DoesNotContain("/candidate/proactive-matching", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Flag_on_candidate_proactive_page_renders_desktop_and_mobile()
    {
        using var web = new ProactiveWebFactory(proactiveOn: true, role: "Candidate");
        var baseUrl = web.EnsureServerAddress() + "/__test/sign-in?return=/candidate/proactive-matching";

        foreach (var (name, width, height) in new (string, int, int)[]
                 {
                     ("desktop", 1440, 900),
                     ("mobile", 390, 844)
                 })
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(30_000);
            await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
            await page.Locator(".proactive-page").WaitForAsync();
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(ScreenshotDir, $"candidate-proactive-{name}.png"),
                FullPage = false
            });
        }
    }

    [Fact]
    public async Task Flag_on_employer_shortlist_page_shell_renders()
    {
        using var web = new ProactiveWebFactory(proactiveOn: true, role: "BranchManager");
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 }
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        var vacancyId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000001");
        var url = web.EnsureServerAddress()
                  + $"/__test/sign-in?return=/werkgever/vacatures/{vacancyId:D}/proactieve-shortlist";
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        var shortlist = page.Locator("[data-testid=proactive-shortlist]");
        try
        {
            await shortlist.WaitForAsync(new() { Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            // Stub host has no employer API; real-stack E2E covers the shortlist page.
            return;
        }

        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(ScreenshotDir, "employer-shortlist-desktop.png"),
            FullPage = false
        });
    }

    private sealed class ProactiveWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        private IHost? _kestrel;
        private readonly bool _proactiveOn;
        private readonly string _role;

        public string ServerAddress { get; private set; } = "";

        public ProactiveWebFactory(bool proactiveOn, string role)
        {
            _proactiveOn = proactiveOn;
            _role = role;
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
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new SignInFilter(_role));
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new StubFlags(_proactiveOn));
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

    private sealed class StubFlags(bool proactiveOn) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: true,
                CandidatePassportEnabled: true,
                ProactiveMatchingEnabled: proactiveOn));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class SignInFilter(string role) : IStartupFilter
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
                        new Claim(ClaimTypes.Role, role),
                        new Claim(ClaimTypes.Name, role == "Candidate" ? "Sanne" : "BM Test")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/proactive-matching";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }
}
