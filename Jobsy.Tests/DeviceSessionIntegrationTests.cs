using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class DeviceSessionIntegrationTests
{
    [Fact]
    public async Task Local_login_with_remember_creates_device_session_and_cookie_flow()
    {
        await using var db = CreateDb();
        var user = await SeedCandidateAsync(db);
        var sut = CreateService(db);

        var created = await sut.CreateAsync(user.Id, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0");
        Assert.NotEqual(Guid.Empty, created.DeviceSessionId);

        // Simulate Web setting Lobsy.Device and later refreshing after Jobsy.Auth expired.
        var rotated = await sut.RotateAsync(created.RefreshToken, "Mozilla/5.0");
        Assert.NotNull(rotated);
        Assert.Equal(user.Email, rotated!.Email);
        Assert.False(string.IsNullOrWhiteSpace(rotated.SessionToken));
    }

    [Fact]
    public async Task Logout_revokes_device_and_push_for_that_device()
    {
        await using var db = CreateDb();
        var user = await SeedCandidateAsync(db);
        var sut = CreateService(db);
        var created = await sut.CreateAsync(user.Id, "Mozilla/5.0");

        db.WebPushSubscriptions.Add(new WebPushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DeviceSessionId = created.DeviceSessionId,
            Endpoint = "https://push.example/endpoint-1",
            P256dh = "p256",
            Auth = "auth"
        });
        await db.SaveChangesAsync();

        var ok = await sut.RevokeAsync(user.Id, created.DeviceSessionId, DeviceSessionRules.RevokeReasonLogout);
        Assert.True(ok);
        Assert.Equal(0, await db.WebPushSubscriptions.CountAsync());
        Assert.NotNull((await db.UserDeviceSessions.SingleAsync()).RevokedAtUtc);
    }

    [Fact]
    public async Task Password_change_style_revoke_all_logs_out_other_devices()
    {
        await using var db = CreateDb();
        var user = await SeedCandidateAsync(db);
        var sut = CreateService(db);
        var a = await sut.CreateAsync(user.Id, "A");
        var b = await sut.CreateAsync(user.Id, "B");

        await sut.RevokeAllAsync(user.Id, DeviceSessionRules.RevokeReasonPasswordChange, bumpSessionVersion: true);

        Assert.All(await db.UserDeviceSessions.ToListAsync(), s => Assert.NotNull(s.RevokedAtUtc));
        Assert.Equal(1, (await db.Users.SingleAsync()).SessionVersion);

        Assert.Null(await sut.RotateAsync(a.RefreshToken, null));
        Assert.Null(await sut.RotateAsync(b.RefreshToken, null));
    }

    [Fact]
    public async Task Push_subscribe_links_device_session_id()
    {
        await using var db = CreateDb();
        var user = await SeedCandidateAsync(db);
        var device = await CreateService(db).CreateAsync(user.Id, "Mozilla/5.0");
        var push = new WebPushSubscriptionService(db);

        await push.UpsertAsync(
            user.Id,
            new WebPushSubscriptionInput(
                "https://push.example/ep",
                "p256dh",
                "auth",
                "UA",
                device.DeviceSessionId));

        var row = await db.WebPushSubscriptions.SingleAsync();
        Assert.Equal(device.DeviceSessionId, row.DeviceSessionId);
        Assert.Equal(user.Id, row.UserId);
    }

    [Fact]
    public async Task Middleware_skips_idle_expire_for_candidate_with_device_claim()
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("Jobsy.Tests.DeviceSession");
        services.AddSingleton<IAuthenticationService>(new FakeAuthService());
        var sp = services.BuildServiceProvider();

        var http = new DefaultHttpContext { RequestServices = sp };
        http.Request.Path = "/candidate/profile";
        http.Response.Body = new MemoryStream();
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.Name, "kandidaat@jobsy.local"));
        identity.AddClaim(new Claim(ClaimTypes.Email, "kandidaat@jobsy.local"));
        identity.AddClaim(new Claim(ClaimTypes.Role, "Candidate"));
        identity.AddClaim(new Claim(Jobsy.Core.Authorization.JobsyClaimTypes.HasDeviceSession, "1"));
        identity.AddClaim(new Claim(Jobsy.Core.Authorization.JobsyClaimTypes.DeviceSessionId, Guid.NewGuid().ToString()));
        http.User = new ClaimsPrincipal(identity);
        SessionActivityCookie.Stamp(http, DateTimeOffset.UtcNow.AddMinutes(-120));
        CopySetCookieToRequest(http);

        var authService = new FakeAuthService();
        ReplaceAuthService(http, authService);

        var nextCalled = false;
        var middleware = new SessionInactivityMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        await middleware.InvokeAsync(http, new FixedTimeoutProvider(30));

        Assert.True(nextCalled);
        Assert.False(authService.SignedOut);
    }

    [Fact]
    public void Offline_fallback_page_exists_and_sw_does_not_cache_navigations_as_root()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web/wwwroot/offline.html")));
        var sw = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/service-worker.published.js"));
        Assert.Contains("offline.html", sw);
        Assert.DoesNotContain("cache.put(\"/\", copy)", sw);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static DeviceSessionService CreateService(JobsyDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:LocalSessionSigningKey"] = "test-local-session-signing-key-32chars!!"
            })
            .Build();
        return new DeviceSessionService(
            db,
            config,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<DeviceSessionService>.Instance);
    }

    private static async Task<User> SeedCandidateAsync(JobsyDbContext db)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "kandidaat@jobsy.local",
            FullName = "Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static void CopySetCookieToRequest(HttpContext http)
    {
        if (!http.Response.Headers.TryGetValue("Set-Cookie", out var values))
        {
            return;
        }

        var jar = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var existing in http.Request.Cookies)
        {
            jar[existing.Key] = existing.Value;
        }

        foreach (var header in values)
        {
            if (string.IsNullOrEmpty(header))
            {
                continue;
            }

            var part = header.Split(';', 2)[0];
            var eq = part.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            jar[part[..eq]] = part[(eq + 1)..];
        }

        http.Request.Headers.Cookie = string.Join("; ", jar.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    private static void ReplaceAuthService(HttpContext http, FakeAuthService auth)
    {
        var existingDp = http.RequestServices.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
        var services = new ServiceCollection();
        services.AddSingleton(existingDp);
        services.AddSingleton<IAuthenticationService>(auth);
        http.RequestServices = services.BuildServiceProvider();
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
