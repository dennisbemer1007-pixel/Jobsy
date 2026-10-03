using System.Net;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Web.Auth;
using Jobsy.Web.Features;
using Jobsy.Web.Navigation;
using Jobsy.Web.Seo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class LandingRedirectMiddlewareTests
{
    [Fact]
    public async Task Anonymous_get_passes_through_with_variant_headers()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-cache, private", response.Headers.CacheControl?.ToString());
        Assert.Contains("Cookie", response.Headers.Vary);
        Assert.True(response.Headers.TryGetValues("X-Lobsy-Variant", out var values));
        Assert.Equal("on", values.Single());
        Assert.Equal("landing", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Anonymous_head_works()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Head, "/");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Lobsy-Variant"));
    }

    [Fact]
    public async Task Signed_in_candidate_redirects_to_passport_when_ready()
    {
        await using var app = await CreateAppAsync(user: Authed(JobsyRoles.Candidate));
        var client = app.GetTestClient();
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        // No show_candidate_how_to claim ⇒ passport-ready under passport ON default.
        Assert.Equal(FeatureRoutes.CandidatePassportPath, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Signed_in_candidate_not_ready_redirects_to_discovery()
    {
        var user = Authed(JobsyRoles.Candidate);
        ((ClaimsIdentity)user.Identity!).AddClaim(new Claim("show_candidate_how_to", "1"));
        await using var app = await CreateAppAsync(user: user);
        var client = app.GetTestClient();
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(FeatureRoutes.CandidateDiscoveryPath, response.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData(JobsyRoles.Admin)]
    [InlineData(JobsyRoles.BranchManager)]
    [InlineData(JobsyRoles.SalesManager)]
    public async Task Signed_in_staff_redirects_to_home(string role)
    {
        await using var app = await CreateAppAsync(user: Authed(role));
        var client = app.GetTestClient();
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/home", response.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("/?company=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")]
    [InlineData("/?q=zorg")]
    public async Task Map_deep_link_redirects_301_to_banenkaart(string pathAndQuery)
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();
        using var response = await client.GetAsync(pathAndQuery);
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.StartsWith(PublicRoutes.Banenkaart + "?", response.Headers.Location?.ToString());
    }

    [Fact]
    public void HomeFor_candidate_is_banenkaart()
    {
        Assert.Equal(PublicRoutes.Banenkaart, LandingRedirectMiddleware.HomeFor(Authed(JobsyRoles.Candidate)));
        Assert.Equal("/home", LandingRedirectMiddleware.HomeFor(Authed(JobsyRoles.Admin)));
    }

    private static ClaimsPrincipal Authed(string role)
    {
        var id = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "test@lobsy.local"),
            new Claim(ClaimTypes.Role, role)
        ], authenticationType: "test");
        return new ClaimsPrincipal(id);
    }

    private static async Task<WebApplication> CreateAppAsync(ClaimsPrincipal? user = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IEmployersSwitch, AlwaysOnEmployersSwitch>();
        var app = builder.Build();
        app.Use(async (ctx, next) =>
        {
            if (user is not null)
            {
                ctx.User = user;
            }

            await next();
        });
        app.UseLandingRedirect();
        app.Run(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            await ctx.Response.WriteAsync("landing");
        });
        await app.StartAsync();
        return app;
    }
}
