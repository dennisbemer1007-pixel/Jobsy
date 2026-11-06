using System.Security.Claims;
using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Api.Security;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Options;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class MfaSeedAccountSkipTests
{
    [Theory]
    [InlineData("admin@jobsy.local", true)]
    [InlineData(" Admin@jobsy.local ", true)]
    [InlineData("admin@test.local", false)]
    [InlineData("not-an-email", false)]
    [InlineData(null, false)]
    public void IsJobsyLocalSeedEmail_recognizes_domain(string? email, bool expected)
        => Assert.Equal(expected, MfaPolicy.IsJobsyLocalSeedEmail(email));

    [Fact]
    public void Skip_flag_off_keeps_seed_accounts_in_mfa_flow()
    {
        Assert.False(MfaPolicy.ShouldSkipMandatoryMfaForSeedAccount("admin@jobsy.local", skipMfaForSeedAccounts: false));
    }

    [Fact]
    public async Task Local_login_skips_mfa_for_jobsy_local_when_flag_on()
    {
        await using var db = CreateDb();
        var user = await SeedPrivilegedUserAsync(db, "admin@jobsy.local", enrolled: false);
        await SeedPasswordAsync(db, user, "SecretPass1!");

        var auth = CreateAuthController(db, skipMfaForSeedAccounts: true);
        var login = await auth.LocalLogin(new LocalLoginRequest(user.Email, "SecretPass1!"), CancellationToken.None);
        var body = Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(login.Result).Value);
        Assert.False(body.RequiresMfa);
        Assert.False(string.IsNullOrWhiteSpace(body.SessionToken));
    }

    [Fact]
    public async Task Local_login_still_requires_mfa_for_non_seed_when_flag_on()
    {
        await using var db = CreateDb();
        var user = await SeedPrivilegedUserAsync(db, "admin-other@test.local", enrolled: false);
        await SeedPasswordAsync(db, user, "SecretPass1!");

        var auth = CreateAuthController(db, skipMfaForSeedAccounts: true);
        var login = await auth.LocalLogin(new LocalLoginRequest(user.Email, "SecretPass1!"), CancellationToken.None);
        var body = Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(login.Result).Value);
        Assert.True(body.RequiresMfa);
    }

    [Fact]
    public async Task Local_login_still_requires_mfa_for_jobsy_local_when_flag_off()
    {
        await using var db = CreateDb();
        var user = await SeedPrivilegedUserAsync(db, "regio@jobsy.local", enrolled: false);
        await SeedPasswordAsync(db, user, "SecretPass1!");

        var auth = CreateAuthController(db, skipMfaForSeedAccounts: false);
        var login = await auth.LocalLogin(new LocalLoginRequest(user.Email, "SecretPass1!"), CancellationToken.None);
        var body = Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(login.Result).Value);
        Assert.True(body.RequiresMfa);
    }

    [Fact]
    public async Task Middleware_allows_privileged_seed_without_mfa_claim_when_flag_on()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/admin";
        context.RequestServices = new ServiceCollection()
            .AddSingleton(Options.Create(new AuthOptions { SkipMfaForSeedAccounts = true }))
            .BuildServiceProvider();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Email, "admin@jobsy.local"),
            new Claim("auth_method", "password")
        ], "test"));

        var reached = false;
        await new MfaEnforcementMiddleware(_ =>
        {
            reached = true;
            return Task.CompletedTask;
        }).InvokeAsync(context);

        Assert.True(reached);
    }

    [Fact]
    public void SkipMfaForSeedAccounts_is_only_enabled_in_acceptatie_appsettings()
    {
        var root = FindRepoRoot();
        foreach (var path in new[]
                 {
                     Path.Combine(root, "Jobsy.Web", "appsettings.json"),
                     Path.Combine(root, "Jobsy.Api", "appsettings.json"),
                     Path.Combine(root, "Jobsy.Web", "appsettings.Development.json"),
                     Path.Combine(root, "Jobsy.Api", "appsettings.Development.json")
                 })
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("SkipMfaForSeedAccounts", text, StringComparison.Ordinal);
        }

        foreach (var path in new[]
                 {
                     Path.Combine(root, "Jobsy.Web", "appsettings.Acceptatie.json"),
                     Path.Combine(root, "Jobsy.Api", "appsettings.Acceptatie.json")
                 })
        {
            var text = File.ReadAllText(path);
            Assert.Contains("\"SkipMfaForSeedAccounts\": true", text, StringComparison.Ordinal);
        }
    }

    private static AuthController CreateAuthController(JobsyDbContext db, bool skipMfaForSeedAccounts)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JobsyAuth:DevelopmentAuthSecret"] = "test-secret",
                ["Auth:SkipMfaForSeedAccounts"] = skipMfaForSeedAccounts ? "true" : "false"
            })
            .Build();
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        return new AuthController(
            db,
            config,
            credentials,
            new AmbassadeurAttributionService(db, new AmbassadeurSettingsService(db), NullLogger<AmbassadeurAttributionService>.Instance, new AlwaysOnFeatures()),
            new StubHostEnvironment(),
            new DeviceSessionService(db, config, new MemoryCache(new MemoryCacheOptions()), NullLogger<DeviceSessionService>.Instance),
            new MfaTrustedDeviceService(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new MfaChallengeService(new MemoryCache(new MemoryCacheOptions())),
            new AlwaysOnFeatures(),
            new UnknownAccountLockoutTracker("test-lockout-key"),
            new AccountLockoutPolicy(config),
            new StubTestAccountsRuntime(),
            new StubSystemAdminSettings(),
            SystemAdminTestDoubles.LoginNotifier(),
            NullLogger<AuthController>.Instance,
            Options.Create(new AuthOptions { SkipMfaForSeedAccounts = skipMfaForSeedAccounts }))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static async Task<User> SeedPrivilegedUserAsync(JobsyDbContext db, string email, bool enrolled)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = "Seed",
            Role = UserRole.Admin,
            IsActive = true,
            AuthenticatorEnabled = enrolled
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task SeedPasswordAsync(JobsyDbContext db, User user, string password)
    {
        db.LocalAuthCredentials.Add(new LocalAuthCredential
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = user.Email.ToLowerInvariant(),
            PasswordHash = JobsyPasswordHasher.Hash(password)
        });
        await db.SaveChangesAsync();
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("MfaSeedSkip-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
