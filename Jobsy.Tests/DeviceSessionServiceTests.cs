using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace Jobsy.Tests;

public class DeviceSessionServiceTests
{
    private static (JobsyDbContext Db, DeviceSessionService Sut) CreateSut()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new JobsyDbContext(options);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:LocalSessionSigningKey"] = "test-local-session-signing-key-32chars!!"
            })
            .Build();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new DeviceSessionService(db, config, cache, NullLogger<DeviceSessionService>.Instance);
        return (db, sut);
    }

    private static async Task<User> SeedUserAsync(JobsyDbContext db, string email = "kandidaat@jobsy.local")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = "Test Kandidaat",
            Role = UserRole.Candidate,
            IsActive = true,
            SessionVersion = 0
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Create_and_rotate_issues_new_token_same_family()
    {
        var (db, sut) = CreateSut();
        var user = await SeedUserAsync(db);

        var created = await sut.CreateAsync(user.Id, "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Safari/604.1");
        Assert.False(string.IsNullOrWhiteSpace(created.RefreshToken));
        Assert.Contains("iPhone", created.DeviceName, StringComparison.OrdinalIgnoreCase);

        var rotated = await sut.RotateAsync(created.RefreshToken, null);
        Assert.NotNull(rotated);
        Assert.Equal(user.Id, rotated!.UserId);
        Assert.Equal(created.DeviceSessionId, rotated.DeviceSessionId);
        Assert.NotEqual(created.RefreshToken, rotated.RefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(rotated.SessionToken));

        var row = await db.UserDeviceSessions.SingleAsync();
        Assert.Equal(created.FamilyId, row.FamilyId);
        Assert.NotNull(row.PreviousRefreshTokenHash);
        Assert.True(row.PreviousTokenGraceUntilUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Reuse_of_old_token_outside_grace_revokes_family()
    {
        var (db, sut) = CreateSut();
        var user = await SeedUserAsync(db);
        var created = await sut.CreateAsync(user.Id, "Mozilla/5.0");
        var rotated = await sut.RotateAsync(created.RefreshToken, null);
        Assert.NotNull(rotated);

        // Expire the grace window artificially.
        var row = await db.UserDeviceSessions.SingleAsync();
        row.PreviousTokenGraceUntilUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var reuse = await sut.RotateAsync(created.RefreshToken, null);
        Assert.Null(reuse);

        row = await db.UserDeviceSessions.SingleAsync();
        Assert.NotNull(row.RevokedAtUtc);
        Assert.Equal(DeviceSessionRules.RevokeReasonTokenReuse, row.RevokedReason);
    }

    [Fact]
    public async Task Grace_window_allows_previous_token_without_theft_detection()
    {
        var (db, sut) = CreateSut();
        var user = await SeedUserAsync(db);
        var created = await sut.CreateAsync(user.Id, "Mozilla/5.0");
        var first = await sut.RotateAsync(created.RefreshToken, null);
        Assert.NotNull(first);

        var second = await sut.RotateAsync(created.RefreshToken, null);
        Assert.NotNull(second);
        Assert.Equal(first!.RefreshToken, second!.RefreshToken);

        var row = await db.UserDeviceSessions.SingleAsync();
        Assert.Null(row.RevokedAtUtc);
    }

    [Fact]
    public async Task Expired_session_cannot_rotate()
    {
        var (db, sut) = CreateSut();
        var user = await SeedUserAsync(db);
        var created = await sut.CreateAsync(user.Id, null);
        var row = await db.UserDeviceSessions.SingleAsync();
        row.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var rotated = await sut.RotateAsync(created.RefreshToken, null);
        Assert.Null(rotated);
        row = await db.UserDeviceSessions.SingleAsync();
        Assert.Equal(DeviceSessionRules.RevokeReasonExpired, row.RevokedReason);
    }

    [Fact]
    public async Task SessionVersion_bump_invalidates_device_via_revoke_all()
    {
        var (db, sut) = CreateSut();
        var user = await SeedUserAsync(db);
        await sut.CreateAsync(user.Id, "A");
        await sut.CreateAsync(user.Id, "B");

        await sut.RevokeAllAsync(user.Id, DeviceSessionRules.RevokeReasonPasswordChange);

        Assert.Equal(2, await db.UserDeviceSessions.CountAsync(s => s.RevokedAtUtc != null));
        Assert.Equal(1, (await db.Users.SingleAsync()).SessionVersion);
    }

    [Fact]
    public async Task MinimumSessionVersion_blocks_rotate()
    {
        var (db, sut) = CreateSut();
        var user = await SeedUserAsync(db);
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            MinimumSessionVersion = 5
        });
        await db.SaveChangesAsync();

        var created = await sut.CreateAsync(user.Id, null);
        var rotated = await sut.RotateAsync(created.RefreshToken, null);
        Assert.Null(rotated);
    }

    [Fact]
    public void DeviceNameFormatter_iphone_safari()
    {
        var name = DeviceNameFormatter.FromUserAgent(
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1");
        Assert.Equal("iPhone – Safari", name);
    }

    [Fact]
    public void RefreshToken_hash_is_stable_and_not_raw()
    {
        var raw = DeviceRefreshToken.Generate();
        var hash = DeviceRefreshToken.Hash(raw);
        Assert.Equal(64, hash.Length);
        Assert.DoesNotContain(raw, hash, StringComparison.Ordinal);
        Assert.Equal(hash, DeviceRefreshToken.Hash(raw));
    }
}

public class DeviceSessionIdleExemptionTests
{
    [Theory]
    [InlineData("Candidate", true, true)]
    [InlineData("Ambassadeur", true, true)]
    [InlineData("Admin", true, false)]
    [InlineData("BranchManager", true, false)]
    [InlineData("Candidate", false, false)]
    public void Idle_exempt_only_for_candidate_like_roles_with_device_session(
        string role,
        bool hasDevice,
        bool expectedExempt)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role)
        };
        if (hasDevice)
        {
            claims.Add(new Claim(Jobsy.Core.Authorization.JobsyClaimTypes.HasDeviceSession, "1"));
            claims.Add(new Claim(Jobsy.Core.Authorization.JobsyClaimTypes.DeviceSessionId, Guid.NewGuid().ToString()));
        }

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var method = typeof(SessionInactivityMiddleware)
            .GetMethod("IsIdleExempt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        var result = (bool)method!.Invoke(null, [user])!;
        Assert.Equal(expectedExempt, result);
    }
}
