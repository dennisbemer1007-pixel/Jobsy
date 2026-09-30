using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Api.Security;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class AuthHardeningHotfixTests
{
    [Fact]
    public void LoginLockoutRules_escalates_per_window()
    {
        Assert.Equal(TimeSpan.FromMinutes(15), LoginLockoutRules.LockoutDuration(1));
        Assert.Equal(TimeSpan.FromMinutes(30), LoginLockoutRules.LockoutDuration(2));
        Assert.Equal(TimeSpan.FromMinutes(60), LoginLockoutRules.LockoutDuration(3));
        Assert.Equal(TimeSpan.FromMinutes(120), LoginLockoutRules.LockoutDuration(4));
        Assert.Equal(TimeSpan.FromMinutes(240), LoginLockoutRules.LockoutDuration(5));
        Assert.Equal(TimeSpan.FromMinutes(240), LoginLockoutRules.LockoutDuration(9));
    }

    [Fact]
    public void TrustedClientIp_ignores_raw_cf_header()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["CF-Connecting-IP"] = "203.0.113.44";
        http.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        Assert.Equal("10.0.0.1", TrustedClientIp.Resolve(http));
    }

    [Fact]
    public void AuthApiClientGuard_no_new_http_client_in_auth_pages()
    {
        var root = FindRepoRoot();
        foreach (var rel in new[]
                 {
                     Path.Combine("Jobsy.Web", "Auth"),
                     Path.Combine("Jobsy.Web", "Components", "Pages", "Account")
                 })
        {
            var dir = Path.Combine(root, rel);
            foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                         .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                                     || f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain("new HttpClient(", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void AuthPagesCspGuard_no_inline_script_or_handlers()
    {
        var root = FindRepoRoot();
        var paths = Directory.EnumerateFiles(
                Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Account"),
                "*.razor",
                SearchOption.TopDirectoryOnly)
            .Append(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Login.razor"))
            .Append(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "RegisterActivate.razor"));

        foreach (var file in paths)
        {
            if (!File.Exists(file))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("<script", StringComparison.OrdinalIgnoreCase)
                    && !trimmed.StartsWith("@*", StringComparison.Ordinal)
                    && !trimmed.StartsWith("//", StringComparison.Ordinal))
                {
                    Assert.Contains("src=", trimmed, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("nonce=", trimmed, StringComparison.OrdinalIgnoreCase);
                }

                Assert.DoesNotMatch(@"\son[a-zA-Z]+\s*=", trimmed);
                Assert.DoesNotContain("javascript:", trimmed, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void LoginRememberDefault_checkbox_not_checked()
    {
        var login = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Login.razor"));
        Assert.Contains("name=\"rememberDevice\"", login, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"rememberDevice\" value=\"true\" checked", login, StringComparison.Ordinal);
        Assert.Contains("Login.RememberDeviceHint", login, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LocalLogin_unknown_then_lockout_same_shape()
    {
        await using var db = NewDb();
        var controller = NewAuth(db);
        for (var i = 0; i < 4; i++)
        {
            var result = await controller.LocalLogin(
                new LocalLoginRequest($"unknown{i}@example.nl", "WrongPass1!"),
                CancellationToken.None);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var body = JsonSerializer.Serialize(unauthorized.Value);
            Assert.Contains("invalid_credentials", body, StringComparison.Ordinal);
        }

        // Same unknown e-mail five times → lockout
        for (var i = 0; i < 4; i++)
        {
            await controller.LocalLogin(
                new LocalLoginRequest("ghost@example.nl", "WrongPass1!"),
                CancellationToken.None);
        }

        var locked = await controller.LocalLogin(
            new LocalLoginRequest("ghost@example.nl", "WrongPass1!"),
            CancellationToken.None);
        var forbidden = Assert.IsType<ObjectResult>(locked.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        var lockedBody = JsonSerializer.Serialize(forbidden.Value);
        Assert.Contains("locked_out", lockedBody, StringComparison.Ordinal);
        Assert.Contains("retryAtUtc", lockedBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LocalLogin_five_wrong_locks_and_resets_after_pause()
    {
        await using var db = NewDb();
        var user = SeedLocalUser(db, "lock@example.nl", "CorrectHorseBattery!");
        var controller = NewAuth(db);

        for (var i = 0; i < 4; i++)
        {
            var r = await controller.LocalLogin(
                new LocalLoginRequest("lock@example.nl", "WrongPass1!"),
                CancellationToken.None);
            Assert.IsType<UnauthorizedObjectResult>(r.Result);
        }

        var fifth = await controller.LocalLogin(
            new LocalLoginRequest("lock@example.nl", "WrongPass1!"),
            CancellationToken.None);
        var forbidden = Assert.IsType<ObjectResult>(fifth.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);

        var cred = await db.LocalAuthCredentials.SingleAsync(c => c.Email == "lock@example.nl");
        Assert.NotNull(cred.LockoutUntil);
        Assert.Equal(1, cred.LockoutCount);

        // Simulate pause ended
        cred.LockoutUntil = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var after = await controller.LocalLogin(
            new LocalLoginRequest("lock@example.nl", "WrongPass1!"),
            CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(after.Result);
        await db.Entry(cred).ReloadAsync();
        Assert.Equal(1, cred.FailedLoginCount);
        Assert.Null(cred.LockoutUntil);
    }

    [Fact]
    public void AuthController_unknown_and_paused_paths_use_dummy_hash()
    {
        var src = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "Jobsy.Api", "Controllers", "AuthController.cs"));
        Assert.Contains("VerifyAgainstDummy", src, StringComparison.Ordinal);
        Assert.Contains("UnknownAccountLockoutTracker", src, StringComparison.Ordinal);
        Assert.Contains("VerifyAgainstDummy", File.ReadAllText(Path.Combine(
            FindRepoRoot(), "Jobsy.Infrastructure", "Security", "JobsyPasswordHasher.cs")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Mfa_five_wrong_expire_challenge_and_recovery_normalizes()
    {
        await using var db = NewDb();
        var secret = TotpAuthenticator.GenerateSecret();
        var user = SeedLocalUser(db, "mfa@example.nl", "CorrectHorseBattery!", enrollMfa: true, secret: secret);
        var challenges = new MfaChallengeService(new MemoryCache(new MemoryCacheOptions()));
        var token = challenges.Create(user, rememberDevice: false, localPassword: true);
        var mfa = NewMfa(db, challenges);

        for (var i = 0; i < 4; i++)
        {
            var r = await mfa.Verify(new MfaVerifyRequest(token, "000000"), CancellationToken.None);
            Assert.IsType<UnauthorizedObjectResult>(r.Result);
        }

        var fifth = await mfa.Verify(new MfaVerifyRequest(token, "000000"), CancellationToken.None);
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(fifth.Result);
        Assert.Contains("challenge_expired", JsonSerializer.Serialize(unauthorized.Value), StringComparison.Ordinal);

        // Fresh challenge + recovery code with dashes/spaces
        var codes = Enumerable.Range(0, 10)
            .Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(8)))
            .ToArray();
        user.RecoveryCodesHash = JsonSerializer.Serialize(
            codes.Select(c => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(c)))));
        await db.SaveChangesAsync();
        var token2 = challenges.Create(user, false, true);
        var spaced = codes[0].Insert(4, "-").ToLowerInvariant();
        var ok = await mfa.Verify(new MfaVerifyRequest(token2, RecoveryCode: spaced), CancellationToken.None);
        var success = Assert.IsType<OkObjectResult>(ok.Result);
        var profile = Assert.IsType<LocalLoginResponse>(success.Value);
        Assert.True(profile.UsedRecoveryCode);
        Assert.Equal(9, profile.RecoveryCodesLeft);
    }

    private static JobsyDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("auth-hardening-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new JobsyDbContext(options);
    }

    private static User SeedLocalUser(
        JobsyDbContext db,
        string email,
        string password,
        bool enrollMfa = false,
        string? secret = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = "Test User",
            Role = UserRole.Admin,
            IsActive = true,
            AuthenticatorEnabled = enrollMfa,
            AuthenticatorSecret = enrollMfa ? secret : null
        };
        db.Users.Add(user);
        db.LocalAuthCredentials.Add(new LocalAuthCredential
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = email,
            PasswordHash = JobsyPasswordHasher.Hash(password)
        });
        db.SaveChanges();
        return user;
    }

    private static AuthController NewAuth(JobsyDbContext db)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JobsyAuth:LocalSessionSigningKey"] = "test-signing-key-32chars-minimum!!",
            ["JobsyAuth:DevelopmentAuthSecret"] = "dev"
        }).Build();
        return new AuthController(
            db,
            config,
            new IntegrationCredentialService(db, new PassthroughSecretProtector()),
            new AmbassadeurAttributionService(
                db,
                new AmbassadeurSettingsService(db),
                NullLogger<AmbassadeurAttributionService>.Instance,
                new AlwaysOnFeatures()),
            new StubHostEnvironment(),
            new DeviceSessionService(
                db,
                config,
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<DeviceSessionService>.Instance),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new MfaChallengeService(new MemoryCache(new MemoryCacheOptions())),
            new AlwaysOnFeatures(),
            new UnknownAccountLockoutTracker("test-lockout-key"),
            NullLogger<AuthController>.Instance);
    }

    private static MfaController NewMfa(JobsyDbContext db, MfaChallengeService challenges)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JobsyAuth:LocalSessionSigningKey"] = "test-signing-key-32chars-minimum!!"
        }).Build();
        var secrets = new PassthroughSecretProtector();
        return new MfaController(
            db,
            secrets,
            new DeviceSessionService(
                db,
                config,
                new MemoryCache(new MemoryCacheOptions()),
                NullLogger<DeviceSessionService>.Instance),
            config,
            challenges,
            new TotpVerifier(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new AlwaysOnFeatures(),
            NullLogger<MfaController>.Instance);
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

        throw new InvalidOperationException("Repo root not found.");
    }

    private sealed class StubHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
