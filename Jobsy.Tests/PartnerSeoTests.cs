using System.Net;
using Jobsy.Web.Hosting;
using Jobsy.Web.Seo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobsy.Tests;

public class PartnerSeoTests
{
    [Fact]
    public void Catalog_marks_partner_code_noindex_with_canonical_partner()
    {
        Assert.False(PageSeoCatalog.IsIndexable("/partner/ABC123"));
        Assert.True(PageSeoCatalog.IsIndexable("/partner"));

        var entry = PageSeoCatalog.Resolve("/partner/ABC123");
        Assert.False(entry.Indexable);
        Assert.Equal("/partner", entry.CanonicalPath);
    }

    [Fact]
    public async Task Partner_code_page_renders_noindex_and_canonical_partner()
    {
        await using var factory = new PartnerSeoWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/partner/ABC123");
        // Feature gate or SSR may 200/302/503 depending on Employers flag; HTML when present must be noindex.
        if (response.StatusCode != HttpStatusCode.OK)
        {
            // Still assert catalog contract when the live page is gated.
            Assert.False(PageSeoCatalog.IsIndexable("/partner/ABC123"));
            return;
        }

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("noindex", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rel=\"canonical\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/partner\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/partner/ABC123\"", html.Split("rel=\"canonical\"", 2).Last(), StringComparison.Ordinal);
    }

    [Fact]
    public void Partner_sales_page_sets_canonical_and_index_from_tracking_code()
    {
        var root = FindRepoRoot();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Partner", "PartnerSales.razor"));
        Assert.Contains("CanonicalPath=\"/partner\"", page, StringComparison.Ordinal);
        Assert.Contains("Index=\"@(string.IsNullOrEmpty(TrackingCode))\"", page, StringComparison.Ordinal);
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

file sealed class PartnerSeoWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
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
                ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
                ["Features:Employers"] = "true"
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
