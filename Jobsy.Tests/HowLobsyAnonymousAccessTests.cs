using System.Net;
using Jobsy.Web.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobsy.Tests;

/// <summary>
/// Public help page <c>/hoe-werkt-lobsy</c> must be reachable without login.
/// </summary>
public class HowLobsyAnonymousAccessTests
{
    [Fact]
    public async Task Anonymous_get_hoe_werkt_lobsy_returns_200_without_login_redirect()
    {
        await using var factory = new HowLobsyWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/hoe-werkt-lobsy");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.DoesNotContain("/login", location, StringComparison.OrdinalIgnoreCase);

        // Document shell must not bounce anonymous visitors via meta/JS login redirect.
        var html = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("returnUrl=%2Fhoe-werkt-lobsy", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("window.location=\"/login", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void How_lobsy_page_is_allow_anonymous_with_guest_guide()
    {
        var root = FindRepoRoot();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "HowLobsyWorks.razor"));
        Assert.Contains("AllowAnonymous", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorize(Roles", page, StringComparison.Ordinal);
        Assert.Contains("HowLobsyRoleGuides.Guest", page, StringComparison.Ordinal);

        var guides = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Help", "HowLobsyRoleGuides.cs"));
        Assert.Contains("public static readonly Guide Guest", guides, StringComparison.Ordinal);
        Assert.Contains("/ontdek", guides, StringComparison.Ordinal);
        Assert.Contains("HowLobsy.Guest.PrimaryCta", guides, StringComparison.Ordinal);
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

file sealed class HowLobsyWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
{
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
            services.RemoveAll<IVacancyMapApiForwarder>();
            services.AddSingleton<IVacancyMapApiForwarder>(_ => new NoopForwarder());
        });
    }

    private sealed class NoopForwarder : IVacancyMapApiForwarder
    {
        public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct)
            => Task.CompletedTask;
    }
}
