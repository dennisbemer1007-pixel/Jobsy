using System.Net;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Web.Hosting;
using Jobsy.Web.Navigation;
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

/// <summary>
/// Regression: <c>/admin</c> must load Beheer CSS bundles; admin inline icons need intrinsic SVG size.
/// </summary>
public class AdminDashboardStylesTests
{
    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/users")]
    public void Admin_routes_resolve_beheer_stylesheets(string path)
    {
        var areas = PageAssetCatalog.ResolveAreas(path);
        Assert.True((areas & PageAssetCatalog.Area.Beheer) != 0, $"Expected Beheer area for {path}");

        var sheets = PageAssetCatalog.StylesheetsFor(areas, path);
        Assert.Contains(
            WwwrootAssetCatalog.Url("css/features/admin.css"),
            sheets,
            StringComparer.Ordinal);
    }

    [Fact]
    public void NavIconSvg_intrinsic_size_adds_width_and_height_when_missing()
    {
        const string bare = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\"></svg>";
        var sized = NavIconSvg.WithIntrinsicSize(bare);
        Assert.Contains("width=\"24\"", sized, StringComparison.Ordinal);
        Assert.Contains("height=\"24\"", sized, StringComparison.Ordinal);
        Assert.StartsWith("<svg width=\"24\" height=\"24\"", sized, StringComparison.Ordinal);
    }

    [Fact]
    public void NavIconSvg_intrinsic_size_is_idempotent_when_width_present()
    {
        const string alreadySized = "<svg width=\"18\" height=\"18\" viewBox=\"0 0 24 24\"></svg>";
        Assert.Equal(alreadySized, NavIconSvg.WithIntrinsicSize(alreadySized));
    }

    [Fact]
    public void App_razor_binds_request_path_to_page_stylesheets()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/App.razor"));
        Assert.Contains("PageAssetCatalog.ResolveAreas(RequestPath)", app, StringComparison.Ordinal);
        Assert.Contains("PageAssetCatalog.StylesheetsFor(PageAreas, RequestPath)", app, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_shell_markup_uses_intrinsic_svg_fallbacks()
    {
        var root = FindRepoRoot();
        var topBar = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Admin/Shell/AdminTopBar.razor"));
        var kpi = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Admin/Ui/AdminKpiCard.razor"));
        Assert.Contains("NavIconSvg.WithIntrinsicSize", topBar, StringComparison.Ordinal);
        Assert.Contains("width=\"64\" height=\"20\"", kpi, StringComparison.Ordinal);
    }

    /// <summary>Run with ADMIN_DASHBOARD_OUT_DIR=/opt/cursor/artifacts to capture PR evidence.</summary>
    [Fact]
    public async Task Capture_admin_dashboard_when_out_dir_set()
    {
        var outDir = Environment.GetEnvironmentVariable("ADMIN_DASHBOARD_OUT_DIR");
        if (string.IsNullOrWhiteSpace(outDir))
        {
            return;
        }

        Directory.CreateDirectory(outDir);
        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));
        await using var web = new AdminDashboardCaptureFactory();
        var baseUrl = web.EnsureServerAddress();
        var signIn = baseUrl + "/__test/sign-in?role=Admin&return=" + Uri.EscapeDataString("/admin");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL"
        });
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync(baseUrl + "/admin", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        await page.WaitForSelectorAsync(".admin-shell", new() { Timeout = 60_000 });
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, "admin-dashboard-after-1440.png"),
            FullPage = true
        });
    }

    private sealed class AdminDashboardCaptureFactory : WebWebApplicationFactory
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
                services.AddSingleton<IStartupFilter>(new AdminDashboardSignInFilter());
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(_ => new JobsyApiClient(
                    new HttpClient(new StubApiHandler()) { BaseAddress = new Uri("http://api.test/") },
                    null!));
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
                _kestrel?.StopAsync().GetAwaiter().GetResult();
                _kestrel?.Dispose();
            }

            base.Dispose(disposing);
        }

        private sealed class StubApiHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
                });
        }

        private sealed class AdminDashboardSignInFilter : IStartupFilter
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
}
