using System.Net;
using Jobsy.Web.Auth;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class BanenkaartRouteTests
{
    [Fact]
    public async Task Banen_redirects_301_to_banenkaart_preserving_query()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();

        using var response = await client.GetAsync("/banen?q=zorg&transport=Fiets");
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/banenkaart?q=zorg&transport=Fiets", response.Headers.Location?.ToString());
    }

    [Fact]
    public void BanenkaartPath_and_RoleNav_point_at_banenkaart()
    {
        Assert.Equal(PublicRoutes.Banenkaart, AuthRedirects.BanenkaartPath);
        Assert.Equal("/banenkaart", AuthRedirects.CandidatePostLoginUrl(false));

        Assert.Contains(RoleNavCatalog.Candidate, i => i.TitleKey == "Nav.Search" && i.Href == "/banenkaart");
        Assert.Contains(RoleNavCatalog.Admin, i => i.TitleKey == "Nav.JobMap" && i.Href == "/banenkaart");
        Assert.Contains(RoleNavCatalog.Branch, i => i.TitleKey == "Nav.JobMap" && i.Href == "/banenkaart");
        Assert.Contains(RoleNavCatalog.Enterprise, i => i.TitleKey == "Nav.JobMap" && i.Href == "/banenkaart");
        Assert.Contains(RoleNavCatalog.Regional, i => i.TitleKey == "Nav.JobMap" && i.Href == "/banenkaart");
        Assert.Contains(RoleNavCatalog.Intermediary, i => i.TitleKey == "Nav.JobMap" && i.Href == "/banenkaart");

        var search = RoleNavCatalog.Candidate.First(i => i.TitleKey == "Nav.Search");
        Assert.True(RoleNavCatalog.IsActive(search, "/banenkaart"));
        Assert.True(RoleNavCatalog.IsActive(search, "/"));
    }

    [Fact]
    public void Canonical_for_home_is_landing_and_banenkaart_is_map()
    {
        Assert.True(PageSeoCatalog.IsIndexable("/banenkaart"));
        Assert.True(PageSeoCatalog.IsIndexable("/"));
        Assert.Null(PageSeoCatalog.Exact["/"].CanonicalPath);
        Assert.Null(PageSeoCatalog.Exact["/banenkaart"].CanonicalPath);
        Assert.True(PageSeoCatalog.Exact["/"].Hreflang);
        Assert.Equal("Landing.Seo.Title", PageSeoCatalog.Exact["/"].TitleKey);
        Assert.Contains("/banenkaart", PageSeoCatalog.StaticIndexablePaths);

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PublicWebBaseUrl"] = "https://lobsy.nl"
        }).Build();

        Assert.Equal(
            "https://lobsy.nl/",
            PageSeoResolver.CanonicalUrl("https://lobsy.nl/", "/", config));
        Assert.Equal(
            "https://lobsy.nl/banenkaart",
            PageSeoResolver.CanonicalUrl("https://lobsy.nl/banenkaart", "/banenkaart", config));

        var entryHome = PageSeoCatalog.Resolve("/");
        var entryMap = PageSeoCatalog.Resolve("/banenkaart");
        Assert.Equal("Landing.Seo.Title", entryHome.TitleKey);
        Assert.Equal("Page.JobMapTitle", entryMap.TitleKey);
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.UseBanenRedirect();
        app.Run(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            await ctx.Response.WriteAsync("fallback");
        });
        await app.StartAsync();
        return app;
    }
}
