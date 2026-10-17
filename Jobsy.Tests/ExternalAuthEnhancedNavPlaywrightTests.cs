using System.Net;
using Jobsy.Web.Auth;
using Jobsy.Web.Features;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.Http;
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

/// <summary>
/// Blazor enhanced navigation must not intercept OAuth provider links (CSP blocks fetch to IdP).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class ExternalAuthEnhancedNavPlaywrightTests : IAsyncLifetime
{
    private ExternalAuthNavWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new ExternalAuthNavWebFactory();
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Fact]
    public async Task Microsoft_provider_link_performs_document_navigation_not_fetch()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(30_000);

        var failedFetches = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Text.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase))
            {
                failedFetches.Add(msg.Text);
            }
        };

        var enhancedNavFetches = 0;
        page.Request += (_, req) =>
        {
            if (!req.Url.Contains("/account/external/entra", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!string.Equals(req.ResourceType, "document", StringComparison.OrdinalIgnoreCase))
            {
                enhancedNavFetches++;
            }
        };

        await page.RouteAsync("**/account/external/entra**", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = (int)HttpStatusCode.Redirect,
                Headers = new Dictionary<string, string>
                {
                    ["Location"] = $"{baseUrl}/__test/external-oauth-landed"
                }
            });
        });

        await page.GotoAsync(baseUrl + "/login", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });

        var microsoft = page.Locator("a.au-provider[href*='/account/external/entra']").First;
        await Assertions.Expect(microsoft).ToBeVisibleAsync();
        var href = await microsoft.GetAttributeAsync("href");
        Assert.False(string.IsNullOrWhiteSpace(href));
        Assert.Contains("data-enhance-nav=\"false\"", await microsoft.EvaluateAsync<string>("el => el.outerHTML"));

        await microsoft.ClickAsync();
        await page.WaitForURLAsync("**/__test/external-oauth-landed**", new() { Timeout = 15_000 });

        Assert.Equal(0, enhancedNavFetches);
        Assert.Empty(failedFetches);
        Assert.Contains("external-oauth-landed", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ExternalAuthNavWebFactory : WebWebApplicationFactory
    {
        private IHost? _kestrel;

        public string ServerAddress { get; private set; } = "";

        public ExternalAuthNavWebFactory()
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
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new TestRoutesFilter());
                services.RemoveAll<IExternalAuthCredentialSource>();
                services.AddSingleton<IExternalAuthCredentialSource>(new FakeExternalAuth(entra: true, google: true));
                services.RemoveAll<IEmployersSwitch>();
                services.AddSingleton<IEmployersSwitch>(new FixedEmployers(on: true));
                services.RemoveAll<Jobsy.Core.Features.IFeatureFlags>();
                services.AddSingleton<Jobsy.Core.Features.IFeatureFlags>(new FixedFlags(employers: true));
                services.RemoveAll<IVacancyMapApiForwarder>();
                services.AddSingleton<IVacancyMapApiForwarder>(_ => new NoopForwarder());
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

    private sealed class TestRoutesFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (ctx, nxt) =>
                {
                    if (ctx.Request.Path.Equals("/__test/external-oauth-landed", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                        await ctx.Response.WriteAsync("external-oauth-landed");
                        return;
                    }

                    await nxt();
                });
                next(app);
            };
    }

    private sealed class NoopForwarder : IVacancyMapApiForwarder
    {
        public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class FakeExternalAuth(bool entra, bool google) : IExternalAuthCredentialSource
    {
        public Task<bool> IsEntraConfiguredAsync(CancellationToken ct = default) => Task.FromResult(entra);
        public Task<bool> IsGoogleConfiguredAsync(CancellationToken ct = default) => Task.FromResult(google);
        public Task<ExternalOAuthCredentials?> GetEntraAsync(CancellationToken ct = default)
            => Task.FromResult<ExternalOAuthCredentials?>(null);
        public Task<ExternalOAuthCredentials?> GetGoogleAsync(CancellationToken ct = default)
            => Task.FromResult<ExternalOAuthCredentials?>(null);
    }

    private sealed class FixedEmployers(bool on) : IEmployersSwitch
    {
        public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default) => ValueTask.FromResult(on);
        public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
            => ValueTask.FromResult(on ? LandingVariant.On : LandingVariant.Zw);
    }

    private sealed class FixedFlags(bool employers) : Jobsy.Core.Features.IFeatureFlags
    {
        public ValueTask<Jobsy.Core.Features.FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new Jobsy.Core.Features.FeatureFlagSnapshot(employers, false));

        public async ValueTask<bool> IsEnabledAsync(
            Jobsy.Core.Features.PlatformFeature feature,
            CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }
}
