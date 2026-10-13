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

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class ExternalVacancyPlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
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
    public async Task Flag_off_add_page_redirects_away_from_route()
    {
        using var web = new ExternalVacancyWebFactory(externalOn: false);
        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(30_000);
        await page.GotoAsync(web.EnsureServerAddress() + "/candidate/external/add", new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000
        });

        await page.WaitForTimeoutAsync(500);
        Assert.DoesNotContain("/candidate/external/add", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Flag_on_shows_paste_field_for_candidate()
    {
        using var web = new ExternalVacancyWebFactory(externalOn: true);
        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(30_000);
        await page.GotoAsync(
            web.EnsureServerAddress() + "/__test/sign-in?return=/candidate/external/add",
            new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });

        await page.Locator("[data-testid=kb-ext-url-input]").WaitForAsync();
        Assert.True(await page.Locator("[data-testid=kb-ext-add-submit]").IsVisibleAsync());
    }

    private sealed class ExternalVacancyWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        private IHost? _kestrel;
        private readonly bool _externalOn;

        public string ServerAddress { get; private set; } = "";

        public ExternalVacancyWebFactory(bool externalOn) => _externalOn = externalOn;

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
                services.AddSingleton<IStartupFilter>(new SignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new StubFlags(_externalOn));
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

    private sealed class StubFlags(bool externalOn) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: true,
                CandidatePassportEnabled: true,
                CandidateExternalVacanciesEnabled: externalOn));

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
                        new Claim(ClaimTypes.Name, "Sanne")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/external/add";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }
}
