using System.Globalization;
using System.Security.Claims;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class SessionSecurityTests
{
    [Theory]
    [InlineData(0, 30)]
    [InlineData(-5, 30)]
    [InlineData(4, 5)]
    [InlineData(30, 30)]
    [InlineData(120, 120)]
    [InlineData(999, 480)]
    public void ClampTimeoutMinutes_enforces_bounds(int input, int expected)
        => Assert.Equal(expected, SessionSecurityRules.ClampTimeoutMinutes(input));

    [Fact]
    public async Task Platform_features_persist_session_inactivity_timeout()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "http://localhost:5201"
            })
            .Build();
        var sut = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            config);

        var updated = await sut.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: true,
            AuthenticatorEnabled: false,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201",
            InactiveCompanyDays: 120,
            SessionInactivityTimeoutMinutes: 45));

        Assert.Equal(45, updated.SessionInactivityTimeoutMinutes);
        var loaded = await sut.GetAsync();
        Assert.Equal(45, loaded.SessionInactivityTimeoutMinutes);
        Assert.Equal(45, (await db.PlatformFeatureSettings.SingleAsync()).SessionInactivityTimeoutMinutes);
    }

    [Fact]
    public async Task Platform_features_default_session_timeout_is_30_minutes()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);
        var sut = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());

        var snap = await sut.GetAsync();
        Assert.Equal(SessionSecurityRules.DefaultInactivityTimeoutMinutes, snap.SessionInactivityTimeoutMinutes);
    }

    [Fact]
    public async Task Middleware_expires_idle_authenticated_session()
    {
        var expiredUnix = DateTimeOffset.UtcNow.AddMinutes(-45).ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);

        var timeout = new FixedTimeoutProvider(30);
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "admin@jobsy.local"), new Claim(ClaimTypes.Role, "Admin")],
            CookieAuthenticationDefaults.AuthenticationScheme));
        http.Request.Path = "/admin/settings";
        http.Request.Headers.Cookie = $"{SessionInactivityMiddleware.LastActivityCookieName}={expiredUnix}";
        http.Response.Body = new MemoryStream();

        var authService = new FakeAuthService();
        http.RequestServices = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(authService)
            .BuildServiceProvider();

        var middleware = new SessionInactivityMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(http, timeout);

        Assert.True(authService.SignedOut);
        Assert.Equal(StatusCodes.Status302Found, http.Response.StatusCode);
        Assert.Contains("session-expired", http.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Middleware_refreshes_last_activity_when_within_timeout()
    {
        var timeout = new FixedTimeoutProvider(30);
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "sm@jobsy.local")],
            CookieAuthenticationDefaults.AuthenticationScheme));
        http.Request.Path = "/sales/toolkit";
        var recent = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);
        http.Request.Headers.Cookie = $"{SessionInactivityMiddleware.LastActivityCookieName}={recent}";
        http.Response.Body = new MemoryStream();
        http.RequestServices = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(new FakeAuthService())
            .BuildServiceProvider();

        var nextCalled = false;
        var middleware = new SessionInactivityMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        await middleware.InvokeAsync(http, timeout);

        Assert.True(nextCalled);
        Assert.Contains(
            http.Response.Headers.SetCookie,
            v => v is not null
                 && v.Contains(SessionInactivityMiddleware.LastActivityCookieName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Logout_with_session_expired_reason_redirects_to_login_notice()
    {
        await using var app = await CreateWebAppAsync(timeoutMinutes: 30);
        var server = app.GetTestServer();

        var context = await server.SendAsync(ctx =>
        {
            ctx.Request.Method = "GET";
            ctx.Request.Path = "/account/logout";
            ctx.Request.QueryString = new QueryString("?reason=session-expired");
        });

        Assert.Equal(302, context.Response.StatusCode);
        Assert.Contains("/login?error=session-expired", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Session_security_endpoint_returns_configured_timeout()
    {
        await using var app = await CreateWebAppAsync(timeoutMinutes: 42);
        var server = app.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/account/session-security");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("42", json);
        Assert.Contains("inactivityTimeoutMinutes", json, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<WebApplication> CreateWebAppAsync(int timeoutMinutes)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddJobsyAuthentication(builder.Configuration, builder.Environment);
        builder.Services.AddSingleton<ISessionTimeoutProvider>(new FixedTimeoutProvider(timeoutMinutes));

        var app = builder.Build();
        app.UseAuthentication();
        app.UseSessionInactivity();
        app.MapJobsyAuthEndpoints();
        await app.StartAsync();
        return app;
    }

    private sealed class FixedTimeoutProvider(int minutes) : ISessionTimeoutProvider
    {
        public Task<int> GetInactivityTimeoutMinutesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(minutes);
    }

    private sealed class FakeAuthService : IAuthenticationService
    {
        public bool SignedOut { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignedOut = true;
            return Task.CompletedTask;
        }
    }
}
