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
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
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
        var http = CreateAuthedContext("/admin/settings", "admin@jobsy.local");
        SessionActivityCookie.Stamp(http, DateTimeOffset.UtcNow.AddMinutes(-45));
        // Move stamped cookie from response to request for the middleware read path.
        CopySetCookieToRequest(http);

        var authService = new FakeAuthService();
        ReplaceAuthService(http, authService);

        var middleware = new SessionInactivityMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(http, new FixedTimeoutProvider(30));

        Assert.True(authService.SignedOut);
        Assert.Equal(StatusCodes.Status302Found, http.Response.StatusCode);
        Assert.Contains("session-expired", http.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Middleware_stamps_when_last_activity_cookie_missing()
    {
        var http = CreateAuthedContext("/admin/settings", "admin@jobsy.local");
        var authService = new FakeAuthService();
        ReplaceAuthService(http, authService);

        var nextCalled = false;
        var middleware = new SessionInactivityMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        await middleware.InvokeAsync(http, new FixedTimeoutProvider(30));

        Assert.False(authService.SignedOut);
        Assert.True(nextCalled);
        Assert.Contains(
            http.Response.Headers.SetCookie,
            v => v is not null
                 && v.Contains(SessionInactivityMiddleware.LastActivityCookieName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Middleware_skips_account_login_when_stale_auth_has_no_activity_cookie()
    {
        var http = CreateAuthedContext("/account/login", "admin@jobsy.local");
        var authService = new FakeAuthService();
        ReplaceAuthService(http, authService);

        var nextCalled = false;
        var middleware = new SessionInactivityMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        await middleware.InvokeAsync(http, new FixedTimeoutProvider(30));

        Assert.False(authService.SignedOut);
        Assert.True(nextCalled);
        Assert.NotEqual(StatusCodes.Status302Found, http.Response.StatusCode);
    }

    [Fact]
    public async Task Middleware_expires_forged_plaintext_or_future_activity_cookie()
    {
        var http = CreateAuthedContext("/admin/settings", "admin@jobsy.local");
        var future = DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds();
        http.Request.Headers.Cookie = $"{SessionInactivityMiddleware.LastActivityCookieName}={future}";

        var authService = new FakeAuthService();
        ReplaceAuthService(http, authService);

        var middleware = new SessionInactivityMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(http, new FixedTimeoutProvider(30));

        Assert.True(authService.SignedOut);
        Assert.Contains("session-expired", http.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Middleware_refreshes_last_activity_when_within_timeout()
    {
        var http = CreateAuthedContext("/sales/toolkit", "sm@jobsy.local");
        SessionActivityCookie.Stamp(http, DateTimeOffset.UtcNow.AddMinutes(-5));
        CopySetCookieToRequest(http);
        ReplaceAuthService(http, new FakeAuthService());

        var nextCalled = false;
        var middleware = new SessionInactivityMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        await middleware.InvokeAsync(http, new FixedTimeoutProvider(30));

        Assert.True(nextCalled);
        Assert.Contains(
            http.Response.Headers.SetCookie,
            v => v is not null
                 && v.Contains(SessionInactivityMiddleware.LastActivityCookieName, StringComparison.Ordinal));
    }

    [Fact]
    public void Activity_cookie_is_secure_on_the_public_host()
    {
        var http = CreateAuthedContext("/home", "alice@jobsy.local");
        http.Request.Host = new HostString("lobsy.nl");
        SessionActivityCookie.Stamp(http, DateTimeOffset.UtcNow);
        Assert.Contains(
            http.Response.Headers.SetCookie,
            v => v is not null
                 && v.Contains(SessionInactivityMiddleware.LastActivityCookieName, StringComparison.Ordinal)
                 && v.Contains("secure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Activity_cookie_is_bound_to_subject()
    {
        var http = CreateAuthedContext("/home", "alice@jobsy.local");
        SessionActivityCookie.Stamp(http, DateTimeOffset.UtcNow.AddMinutes(-1));
        CopySetCookieToRequest(http);

        // Swap identity — protected payload subject no longer matches.
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "bob@jobsy.local"),
                new Claim(ClaimTypes.Email, "bob@jobsy.local")
            ],
            CookieAuthenticationDefaults.AuthenticationScheme));

        Assert.Null(SessionActivityCookie.TryRead(http));
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
    public async Task Logout_with_session_expired_reason_keeps_return_url()
    {
        await using var app = await CreateWebAppAsync(timeoutMinutes: 30);
        var server = app.GetTestServer();

        var context = await server.SendAsync(ctx =>
        {
            ctx.Request.Method = "GET";
            ctx.Request.Path = "/account/logout";
            ctx.Request.QueryString = new QueryString("?reason=session-expired&returnUrl=/employer/vacancies/123");
        });

        Assert.Equal(302, context.Response.StatusCode);
        var location = context.Response.Headers.Location.ToString();
        Assert.Contains("/login?error=session-expired", location);
        Assert.Contains("returnUrl=", location);
        Assert.Contains("employer", location);
        Assert.DoesNotContain("https://", location);
    }

    [Fact]
    public async Task Logout_session_expired_strips_query_pii_from_return_url()
    {
        await using var app = await CreateWebAppAsync(timeoutMinutes: 30);
        var server = app.GetTestServer();

        var context = await server.SendAsync(ctx =>
        {
            ctx.Request.Method = "GET";
            ctx.Request.Path = "/account/logout";
            ctx.Request.QueryString = new QueryString(
                "?reason=session-expired&returnUrl="
                + Uri.EscapeDataString("/employer/vacancies?email=secret@jobsy.local"));
        });

        Assert.Equal(302, context.Response.StatusCode);
        var location = Uri.UnescapeDataString(context.Response.Headers.Location.ToString());
        Assert.Contains("/login?error=session-expired", location);
        Assert.Contains("/employer/vacancies", location);
        Assert.DoesNotContain("secret@jobsy.local", location);
        Assert.DoesNotContain("email=", location);
    }

    [Fact]
    public async Task Middleware_idle_redirect_omits_request_query_pii()
    {
        var http = CreateAuthedContext("/employer/vacancies", "manager@jobsy.local");
        http.Request.QueryString = new QueryString("?email=secret@jobsy.local&token=abc");
        SessionActivityCookie.Stamp(http, DateTimeOffset.UtcNow.AddMinutes(-45));
        CopySetCookieToRequest(http);

        var authService = new FakeAuthService();
        ReplaceAuthService(http, authService);

        var middleware = new SessionInactivityMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(http, new FixedTimeoutProvider(30));

        Assert.True(authService.SignedOut);
        var location = Uri.UnescapeDataString(http.Response.Headers.Location.ToString());
        Assert.Contains("session-expired", location);
        Assert.Contains("/employer/vacancies", location);
        Assert.DoesNotContain("secret@jobsy.local", location);
        Assert.DoesNotContain("email=", location);
        Assert.DoesNotContain("token=abc", location);
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

    private static DefaultHttpContext CreateAuthedContext(string path, string email)
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("Jobsy.Tests.Session");
        services.AddSingleton<IAuthenticationService>(new FakeAuthService());
        var sp = services.BuildServiceProvider();

        var http = new DefaultHttpContext
        {
            RequestServices = sp
        };
        http.Request.Path = path;
        http.Response.Body = new MemoryStream();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, "Admin")
            ],
            CookieAuthenticationDefaults.AuthenticationScheme));
        return http;
    }

    private static void ReplaceAuthService(HttpContext http, FakeAuthService authService)
    {
        var existingDp = http.RequestServices.GetRequiredService<IDataProtectionProvider>();
        var services = new ServiceCollection();
        services.AddSingleton(existingDp);
        services.AddSingleton<IAuthenticationService>(authService);
        http.RequestServices = services.BuildServiceProvider();
    }

    private static void CopySetCookieToRequest(HttpContext http)
    {
        var setCookie = http.Response.Headers.SetCookie.FirstOrDefault(v =>
            v is not null && v.Contains(SessionInactivityMiddleware.LastActivityCookieName, StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(setCookie));
        var segment = setCookie!.Split(';', 2)[0];
        var eq = segment.IndexOf('=');
        Assert.True(eq > 0);
        var name = segment[..eq];
        var value = segment[(eq + 1)..];
        http.Request.Headers.Cookie = $"{name}={value}";
        http.Response.Headers.SetCookie = new Microsoft.Extensions.Primitives.StringValues();
    }

    private static async Task<WebApplication> CreateWebAppAsync(int timeoutMinutes)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddDataProtection().SetApplicationName("Jobsy.Tests.SessionWeb");
        builder.Services.AddJobsyAuthentication(builder.Configuration, builder.Environment);
        builder.Services.AddSingleton<ISessionTimeoutProvider>(new FixedTimeoutProvider(timeoutMinutes));
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
