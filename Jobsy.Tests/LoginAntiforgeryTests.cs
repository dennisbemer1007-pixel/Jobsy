using Jobsy.Web.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

/// <summary>
/// Missing/stale antiforgery on login must redirect to /login?error=retry,
/// not throw into the generic /Error page (acceptatie UX regression).
/// </summary>
public class LoginAntiforgeryTests
{
    [Fact]
    public async Task Post_account_login_without_antiforgery_redirects_to_retry()
    {
        await using var app = await CreateAppAsync();
        var server = app.GetTestServer();

        var context = await server.SendAsync(ctx =>
        {
            ctx.Request.Method = "POST";
            ctx.Request.Path = "/account/login";
            ctx.Request.ContentType = "application/x-www-form-urlencoded";
            ctx.Request.Body = new MemoryStream(
                System.Text.Encoding.UTF8.GetBytes(
                    "email=kandidaat%40jobsy.local&password=Jobsy123%21&returnUrl=%2Fhome"));
        });

        Assert.Equal(302, context.Response.StatusCode);
        var location = context.Response.Headers.Location.ToString();
        Assert.StartsWith("/login?", location, StringComparison.Ordinal);
        Assert.Contains("error=retry", location, StringComparison.Ordinal);
        Assert.DoesNotContain("/Error", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Post_account_logout_without_antiforgery_still_signs_out()
    {
        await using var app = await CreateAppAsync();
        var server = app.GetTestServer();

        var context = await server.SendAsync(ctx =>
        {
            ctx.Request.Method = "POST";
            ctx.Request.Path = "/account/logout";
            ctx.Request.ContentType = "application/x-www-form-urlencoded";
            ctx.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(""));
        });

        Assert.Equal(302, context.Response.StatusCode);
        Assert.Equal("/", context.Response.Headers.Location.ToString());
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddDataProtection().SetApplicationName("Jobsy.Tests.LoginAntiforgery");
        builder.Services.AddJobsyAuthentication(builder.Configuration, builder.Environment);
        builder.Services.AddSingleton<Jobsy.Web.Security.ISessionTimeoutProvider>(
            new FixedSessionTimeoutProvider());
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("auth", httpContext =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 1000,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });

        var app = builder.Build();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapJobsyAuthEndpoints();
        await app.StartAsync();
        return app;
    }

    private sealed class FixedSessionTimeoutProvider : Jobsy.Web.Security.ISessionTimeoutProvider
    {
        public Task<int> GetInactivityTimeoutMinutesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(30);
    }
}
