using System.Diagnostics;
using System.Net;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobsy.Tests;

public class BlazorReconnectAndHealthzTests
{
    [Fact]
    public void App_razor_starts_blazor_with_reconnect_options_and_rejected_reload()
    {
        var root = FindRepoRoot();
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/App.razor"));
        Assert.Contains("autostart=\"false\"", app, StringComparison.Ordinal);
        Assert.Contains("Blazor.start(", app, StringComparison.Ordinal);
        Assert.Contains("reconnectionOptions", app, StringComparison.Ordinal);
        Assert.Contains("components-reconnect-rejected", app, StringComparison.Ordinal);
        Assert.Contains("location.reload()", app, StringComparison.Ordinal);
        Assert.Contains("Blazor.reconnect", app, StringComparison.Ordinal);
        Assert.Contains("visibilitychange", app, StringComparison.Ordinal);

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
