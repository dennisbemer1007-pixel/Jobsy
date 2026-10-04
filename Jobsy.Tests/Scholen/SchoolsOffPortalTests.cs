using System.Net;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests.Scholen;

public class SchoolsOffPortalTests
{
    [Fact]
    public async Task Browser_request_redirects_to_the_friendly_page_and_api_stays_json()
    {
        var middleware = new SchoolsFeatureMiddleware(_ => throw new InvalidOperationException("should not continue"));
        var services = new ServiceCollection()
            .AddSingleton<IFeatureFlags>(new FixedFlags(schools: false))
            .BuildServiceProvider();

        var page = new DefaultHttpContext { RequestServices = services };
        page.Request.Path = "/school/klassen";
        await middleware.InvokeAsync(page);
        Assert.Equal(StatusCodes.Status302Found, page.Response.StatusCode);
        Assert.Equal(FeatureRoutes.SchoolsOffAccessDeniedPath, page.Response.Headers.Location.ToString());

        var api = new DefaultHttpContext { RequestServices = services };
        api.Request.Path = "/api/school/dashboard";
        api.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(api);
        Assert.Equal(StatusCodes.Status404NotFound, api.Response.StatusCode);
        api.Response.Body.Position = 0;
        var body = await new StreamReader(api.Response.Body).ReadToEndAsync();
        Assert.Contains("feature_disabled", body, StringComparison.Ordinal);

        var open = new SchoolsFeatureMiddleware(_ => Task.CompletedTask);
        var allowed = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IFeatureFlags>(new FixedFlags(schools: true))
                .BuildServiceProvider()
        };
        allowed.Request.Path = "/leraar";
        await open.InvokeAsync(allowed);
        Assert.Equal(StatusCodes.Status200OK, allowed.Response.StatusCode);
    }

    [Fact]
    public async Task Missing_flags_fail_closed_for_the_portal()
    {
        var middleware = new SchoolsFeatureMiddleware(_ => throw new InvalidOperationException("opened"));
        var page = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        page.Request.Path = "/leerling";
        await middleware.InvokeAsync(page);
        Assert.Equal(FeatureRoutes.SchoolsOffAccessDeniedPath, page.Response.Headers.Location.ToString());
    }

    [Theory]
    [InlineData(JobsyRoles.SchoolAdmin, false, "/access-denied?reason=schools-off")]
    [InlineData(JobsyRoles.Teacher, false, "/access-denied?reason=schools-off")]
    [InlineData(JobsyRoles.SchoolAdmin, true, "/school")]
    [InlineData(JobsyRoles.Teacher, true, "/leraar")]
    public void Role_home_does_not_enter_the_portal_when_schools_are_off(string role, bool schools, string expected)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, role)],
            authenticationType: "test"));
        var flags = new FeatureFlagSnapshot(EmployersEnabled: true, CandidatePassportEnabled: true, SchoolsEnabled: schools);
        Assert.Equal(expected, FeatureRoutes.HomeFor(user, flags));
    }

    [Fact]
    public void Schools_paused_login_is_its_own_state()
    {
        Assert.Equal(LoginState.SchoolsPaused, LoginStateMapping.FromQuery("schools-paused", false));
    }

    [Fact]
    public async Task School_and_teacher_login_redirects_are_not_stored()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => options.LoginPath = "/login");
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IFeatureFlags>(new FixedFlags(schools: true));

        var app = builder.Build();
        app.UseAuthentication();
        app.UseMiddleware<SchoolsFeatureMiddleware>();
        app.UseAuthorization();
        app.MapGet("/school", () => Results.Text("school")).RequireAuthorization();
        app.MapGet("/leraar", () => Results.Text("leraar")).RequireAuthorization();
        app.MapGet("/scholen", () => Results.Text("public"));
        await app.StartAsync();
        try
        {
            var client = app.GetTestClient();
            var school = await client.GetAsync("/school");
            Assert.Equal(HttpStatusCode.Redirect, school.StatusCode);
            Assert.Contains("/login", school.Headers.Location?.OriginalString ?? "", StringComparison.Ordinal);
            Assert.Equal("no-store", school.Headers.CacheControl?.ToString());

            var teacher = await client.GetAsync("/leraar");
            Assert.Equal(HttpStatusCode.Redirect, teacher.StatusCode);
            Assert.Equal("no-store", teacher.Headers.CacheControl?.ToString());

            var marketing = await client.GetAsync("/scholen");
            Assert.Equal(HttpStatusCode.OK, marketing.StatusCode);
            Assert.Null(marketing.Headers.CacheControl);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private sealed class FixedFlags(bool schools) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: false,
                CandidatePassportEnabled: true,
                SchoolsEnabled: schools));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature == PlatformFeature.Schools && schools);

        public void Invalidate()
        {
        }
    }
}
