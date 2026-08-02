using Jobsy.Web.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests;

public class LogoutEndpointTests
{
    [Fact]
    public async Task Get_account_logout_redirects_home_instead_of_405()
    {
        await using var app = await CreateAppAsync();
        var server = app.GetTestServer();

        var context = await server.SendAsync(ctx =>
        {
            ctx.Request.Method = "GET";
            ctx.Request.Path = "/account/logout";
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
        builder.Services.AddDataProtection().SetApplicationName("Jobsy.Tests.Logout");
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
