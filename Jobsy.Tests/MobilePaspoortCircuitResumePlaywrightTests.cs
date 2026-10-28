using System.Net;
using System.Security.Claims;
using Jobsy.Core.Features;
using Jobsy.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Mobile paspoort must survive offline/online and visibility resume without a full document reload.
/// </summary>
[Collection("PlaywrightSmoke")]
public class MobilePaspoortCircuitResumePlaywrightTests
{
    private const int DefaultIdleMs = 121_000;

    [Fact]
    public async Task Paspoort_mobile_idle_and_websocket_drop_does_not_reload()
    {
        await using var factory = new PaspoortCircuitWebFactory();
        await using var session = await OpenPaspoortMobilePageAsync(factory);
        var page = session.Page;
        var context = session.Context;
        await page.EvaluateAsync("() => { window.__lobsyNavCount = 0; }");

        var idleMs = ResolveIdleMs();
        var end = DateTime.UtcNow.AddMilliseconds(idleMs);
        while (DateTime.UtcNow < end)
        {
            var slice = (int)Math.Max(0, Math.Min(10_000, (end - DateTime.UtcNow).TotalMilliseconds));
            if (slice == 0)
            {
                break;
            }

            await page.WaitForTimeoutAsync(slice);
        }

        _ = await page.EvaluateAsync<string>(
            """
            () => {
              if (window.__lobsyBlazorWs && window.__lobsyBlazorWs.readyState === WebSocket.OPEN) {
                window.__lobsyBlazorWs.close(1006, 'test-drop');
                return 'ws-closed';
              }
              return 'no-ws';
            }
            """);

        await page.WaitForTimeoutAsync(5_000);
        await context.SetOfflineAsync(true);
        await page.WaitForTimeoutAsync(2_000);
        await context.SetOfflineAsync(false);
        await page.WaitForTimeoutAsync(10_000);

        var navCount = await page.EvaluateAsync<int>("() => window.__lobsyNavCount || 0");
        Assert.True(navCount == 0, $"Expected no unprompted document reload after idle + circuit drop, got {navCount} load events.");
    }

    [Fact]
    public async Task Paspoort_mobile_resume_does_not_trigger_document_reload()
    {
        await using var factory = new PaspoortCircuitWebFactory();
        await using var session = await OpenPaspoortMobilePageAsync(factory);
        var page = session.Page;
        var context = session.Context;
        await page.EvaluateAsync("() => { window.__lobsyNavCount = 0; }");

        await context.SetOfflineAsync(true);
        await page.WaitForTimeoutAsync(3_000);
        await page.EvaluateAsync(
            """
            () => {
              Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => 'hidden' });
              document.dispatchEvent(new Event('visibilitychange'));
            }
            """);
        await page.WaitForTimeoutAsync(2_000);
        await context.SetOfflineAsync(false);
        await page.EvaluateAsync(
            """
            () => {
              Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => 'visible' });
              document.dispatchEvent(new Event('visibilitychange'));
              window.dispatchEvent(new Event('online'));
            }
            """);
        await page.WaitForTimeoutAsync(8_000);

        var navCount = await page.EvaluateAsync<int>("() => window.__lobsyNavCount || 0");
        Assert.True(navCount == 0, $"Expected no unprompted document reload after resume, got {navCount} load events.");
    }

    [Fact]
    public void Blazor_boot_blocks_automatic_document_reload_and_wires_manual_reconnect()
    {
        var root = FindRepoRoot();
        var boot = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/blazor-boot.js"));
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/App.razor"));

        Assert.Contains("__lobsyReloadGuardInstalled", boot, StringComparison.Ordinal);
        Assert.Contains("blocked automatic document.reload", boot, StringComparison.Ordinal);
        Assert.Contains("__lobsyReloadGuardInstalled", app, StringComparison.Ordinal);
        Assert.Contains("components-reconnect-state-changed", boot, StringComparison.Ordinal);
        Assert.Contains("data-reconnect-try", app, StringComparison.Ordinal);
        Assert.Contains("data-reconnect-login", app, StringComparison.Ordinal);
        Assert.Contains("data-reconnect-reload", app, StringComparison.Ordinal);
        Assert.Contains("[data-reconnect-reload]", boot, StringComparison.Ordinal);
        Assert.DoesNotContain("onclick=\"location.reload()\"", app, StringComparison.Ordinal);
        Assert.Contains("reloadPageOnce", boot, StringComparison.Ordinal);
    }

    private static int ResolveIdleMs()
    {
        var raw = Environment.GetEnvironmentVariable("JOBSY_PASPOORT_IDLE_MS");
        if (int.TryParse(raw, out var ms) && ms is >= 15_000 and <= 300_000)
        {
            return ms;
        }

        return DefaultIdleMs;
    }

    private static async Task<MobilePaspoortBrowserSession> OpenPaspoortMobilePageAsync(
        PaspoortCircuitWebFactory factory)
    {
        _ = factory.CreateClient();
        var baseUrl = factory.ServerAddress;
        Assert.False(string.IsNullOrEmpty(baseUrl));

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Block
        });

        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync(
            """
            () => {
              window.__lobsyNavCount = 0;
              window.addEventListener("load", () => { window.__lobsyNavCount = (window.__lobsyNavCount || 0) + 1; });
              if (!window.__lobsyWsHook) {
                window.__lobsyWsHook = true;
                const Native = window.WebSocket;
                window.WebSocket = function (url, protocols) {
                  const ws = protocols === undefined ? new Native(url) : new Native(url, protocols);
                  if (url && String(url).toLowerCase().includes("blazor")) {
                    window.__lobsyBlazorWs = ws;
                  }
                  return ws;
                };
                window.WebSocket.prototype = Native.prototype;
                Object.assign(window.WebSocket, Native);
              }
            }
            """);

        var signIn = baseUrl + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/paspoort");
        using (var http = factory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri(baseUrl) }))
        {
            using var signInResponse = await http.GetAsync(
                "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/paspoort"));
            Assert.True(
                signInResponse.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found,
                "Expected test sign-in redirect.");
        }

        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await page.WaitForURLAsync("**/candidate/paspoort**", new() { Timeout = 60_000 });

        await page.WaitForFunctionAsync(
            """
            () => !!document.querySelector('[data-testid="passport-page-ssr"]')
              || document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'
            """,
            null,
            new() { Timeout = 60_000 });

        return new MobilePaspoortBrowserSession(playwright, browser, context, page);
    }

    private sealed class MobilePaspoortBrowserSession : IAsyncDisposable
    {
        private readonly IPlaywright _playwright;
        private readonly IBrowser _browser;
        private readonly IBrowserContext _context;

        public MobilePaspoortBrowserSession(IPlaywright playwright, IBrowser browser, IBrowserContext context, IPage page)
        {
            _playwright = playwright;
            _browser = browser;
            _context = context;
            Page = page;
        }

        public IBrowserContext Context => _context;
        public IPage Page { get; }

        public async ValueTask DisposeAsync()
        {
            await _context.CloseAsync();
            await _browser.CloseAsync();
            _playwright.Dispose();
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }

    private sealed class PaspoortCircuitWebFactory : WebWebApplicationFactory
    {
        public string ServerAddress { get; private set; } = "";
        private IHost? _kestrel;

        public PaspoortCircuitWebFactory()
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
                services.RemoveAll<IHostedService>();
                services.AddSingleton<IStartupFilter>(new CandidateSignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new PassportOnFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new PaspoortHttpClientFactory());
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

    private sealed class PassportOnFlags : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(EmployersEnabled: false, CandidatePassportEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class CandidateSignInFilter : IStartupFilter
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
                        new Claim(ClaimTypes.Name, "Samira Demo")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].FirstOrDefault() ?? "/candidate/paspoort";
                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class PaspoortHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler(), disposeHandler: true)
            {
                BaseAddress = new Uri("http://api.test/")
            };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var json = path.Contains("/kompas", StringComparison.OrdinalIgnoreCase)
                ? """{"profileCompletenessPercent":0}"""
                : path.Contains("/site/status", StringComparison.OrdinalIgnoreCase)
                    ? """{"maintenance":false,"expectedEndUtc":null}"""
                    : path.Contains("/applications", StringComparison.OrdinalIgnoreCase)
                        ? "[]"
                        : "{}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
