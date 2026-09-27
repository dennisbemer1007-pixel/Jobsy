using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public class BlazorReconnectAndHealthzTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public void App_loads_blazor_boot_js_with_defer_after_framework()
    {
        var root = FindRepoRoot();
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/App.razor"));
        Assert.Contains("autostart=\"false\"", app, StringComparison.Ordinal);
        Assert.Contains("blazor.web.js", app, StringComparison.Ordinal);
        Assert.Contains("js/blazor-boot.js", app, StringComparison.Ordinal);
        Assert.DoesNotContain("Blazor.start(", app, StringComparison.Ordinal);

        var bootIdx = app.IndexOf("js/blazor-boot.js", StringComparison.Ordinal);
        var frameworkIdx = app.IndexOf("blazor.web.js", StringComparison.Ordinal);
        Assert.True(frameworkIdx >= 0 && bootIdx > frameworkIdx, "blazor-boot.js must load after blazor.web.js");

        var frameworkTagEnd = app.IndexOf(">", frameworkIdx, StringComparison.Ordinal);
        var frameworkTag = app.Substring(frameworkIdx, Math.Max(0, frameworkTagEnd - frameworkIdx));
        Assert.Contains("defer", frameworkTag, StringComparison.Ordinal);

        var bootTagEnd = app.IndexOf(">", bootIdx, StringComparison.Ordinal);
        var bootTag = app.Substring(bootIdx, Math.Max(0, bootTagEnd - bootIdx));
        Assert.Contains("defer", bootTag, StringComparison.Ordinal);

        var boot = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/blazor-boot.js"));
        Assert.Contains("Blazor.start(", boot, StringComparison.Ordinal);
        Assert.Contains("reconnectionOptions", boot, StringComparison.Ordinal);
        Assert.Contains("components-reconnect-rejected", boot, StringComparison.Ordinal);
        Assert.Contains("location.reload()", boot, StringComparison.Ordinal);
        Assert.Contains("Blazor.reconnect", boot, StringComparison.Ordinal);
        Assert.Contains("visibilitychange", boot, StringComparison.Ordinal);
        Assert.Contains("Blazor is not defined", boot, StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains("1.5s", css, StringComparison.Ordinal);
        Assert.Contains(".reconnect-toast.components-reconnect-show", css, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Program.cs"));
        Assert.Contains("FromMinutes(15)", program, StringComparison.Ordinal);
        Assert.Contains("DisconnectedCircuitMaxRetained = 100", program, StringComparison.Ordinal);
        Assert.Contains("/healthz", program, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Healthz_returns_ok_quickly_without_api_client()
    {
        await using var factory = new HealthzWebFactory();
        var client = factory.CreateClient();
        // Warm-up (host start) is excluded from the timing budget.
        _ = await client.GetAsync("/healthz");

        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync("/healthz");
        sw.Stop();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", (await response.Content.ReadAsStringAsync()).Trim());
        Assert.True(sw.ElapsedMilliseconds < 50, $"healthz took {sw.ElapsedMilliseconds}ms");

        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Program.cs"));
        Assert.Contains("MapGet(\"/healthz\", () => Results.Text(\"ok\"))", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/healthz\", (IJobsyApiClient", program, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(390, 844)]
    [InlineData(1280, 800)]
    public async Task Blazor_circuit_starts_and_kompas_tab_updates(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        var pageErrors = new List<string>();
        page.PageError += (_, err) => pageErrors.Add(err);

        var ws = await page.RunAndWaitForWebSocketAsync(
            async () =>
            {
                await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            },
            new()
            {
                Timeout = 5_000,
                Predicate = socket => (socket.Url ?? "").Contains("_blazor", StringComparison.OrdinalIgnoreCase)
            });
        Assert.NotNull(ws);
        Assert.Contains("_blazor", ws.Url, StringComparison.OrdinalIgnoreCase);
        Assert.True(pageErrors.Count == 0, "pageerror on /: " + string.Join(" | ", pageErrors));

        if (!await TryLoginAsync(page, baseUrl))
        {
            Assert.Fail("Candidate login failed — cannot verify Kompas interactivity.");
        }

        pageErrors.Clear();
        await page.GotoAsync(
            baseUrl + "/candidate/profile?tab=dna",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        var profileTab = page.Locator("#kompas-tab-profile");
        await profileTab.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        pageErrors.Clear();
        await profileTab.ClickAsync();
        await Assertions.Expect(profileTab).ToHaveAttributeAsync("aria-selected", new Regex("(?i)^true$"), new() { Timeout = 1_000 });

        Assert.True(pageErrors.Count == 0, "pageerror: " + string.Join(" | ", pageErrors));
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            await page.ClickAsync("button.login-submit");
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl.TrimEnd('/') + "/");
            return (int)response.StatusCode is >= 200 and < 500;
        }
        catch
        {
            return false;
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

/// <summary>Minimal web host for /healthz — counting API client construction.</summary>
file sealed class HealthzWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
    public static int ApiClientCreations;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ApiClientCreations = 0;
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
            services.RemoveAll<IVacancyMapApiForwarder>();
            services.AddSingleton<IVacancyMapApiForwarder>(_ =>
            {
                Interlocked.Increment(ref ApiClientCreations);
                return new NoopForwarder();
            });
        });
    }

    private sealed class NoopForwarder : IVacancyMapApiForwarder
    {
        public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct)
            => Task.CompletedTask;
    }
}
