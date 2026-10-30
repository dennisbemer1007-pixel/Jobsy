using Jobsy.Api.Controllers;
using Jobsy.Api.Models;
using Jobsy.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Jobsy.Core.Admin;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Ops;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Jobsy.Infrastructure.Security;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class SystemAdminAccountTests
{
    [Fact]
    public void SystemAdmin_password_rules_require_16_chars()
    {
        Assert.Throws<ArgumentException>(() => SystemAdminPasswordRules.Validate("short-but-valid12!"));
        SystemAdminPasswordRules.Validate("long-enough-password-1!");
    }

    [Fact]
    public async Task Provision_creates_admin_with_hashed_password()
    {
        await using var db = CreateDb();
        var config = Config(enabled: true, password: "BreakGlass-Password-99!");
        var services = Services(db);

        await SystemAdminAccountProvisioner.ProvisionAsync(
            services, config, NullLogger.Instance, CancellationToken.None);

        var user = await db.Users.SingleAsync(u => u.IsSystemAdminAccount);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.True(user.IsActive);
        var cred = await db.LocalAuthCredentials.SingleAsync(c => c.UserId == user.Id);
        Assert.True(JobsyPasswordHasher.Verify("BreakGlass-Password-99!", cred.PasswordHash));
    }

    [Fact]
    public async Task Provision_updates_password_when_env_changes()
    {
        await using var db = CreateDb();
        var services = Services(db);
        await SystemAdminAccountProvisioner.ProvisionAsync(
            services,
            Config(enabled: true, password: "BreakGlass-Password-99!"),
            NullLogger.Instance,
            CancellationToken.None);
        var before = await db.Users.SingleAsync(u => u.IsSystemAdminAccount);

        await SystemAdminAccountProvisioner.ProvisionAsync(
            services,
            Config(enabled: true, password: "BreakGlass-Password-88!"),
            NullLogger.Instance,
            CancellationToken.None);

        var cred = await db.LocalAuthCredentials.SingleAsync(c => c.UserId == before.Id);
        Assert.True(JobsyPasswordHasher.Verify("BreakGlass-Password-88!", cred.PasswordHash));
    }

    [Fact]
    public async Task Provision_disabled_locks_account_without_deleting()
    {
        await using var db = CreateDb();
        var services = Services(db);
        await SystemAdminAccountProvisioner.ProvisionAsync(
            services,
            Config(enabled: true, password: "BreakGlass-Password-99!"),
            NullLogger.Instance,
            CancellationToken.None);

        await SystemAdminAccountProvisioner.ProvisionAsync(
            services,
            Config(enabled: false),
            NullLogger.Instance,
            CancellationToken.None);

        var user = await db.Users.SingleAsync(u => u.IsSystemAdminAccount);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task Provision_enabled_with_weak_password_throws()
    {
        await using var db = CreateDb();
        var services = Services(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SystemAdminAccountProvisioner.ProvisionAsync(
                services,
                Config(enabled: true, password: "short"),
                NullLogger.Instance,
                CancellationToken.None));
    }

    [Fact]
    public void MfaPolicy_system_admin_skips_mfa_when_not_required()
    {
        Assert.False(MfaPolicy.IsLocalPasswordMfaStepRequired(
            isSystemAdminAccount: true,
            UserRole.Admin,
            isTestAccount: false,
            testAccountsActive: false,
            authenticatorEnabled: true,
            systemAdminRequireTwoFactor: false));
    }

    [Fact]
    public void MfaPolicy_system_admin_requires_mfa_when_flag_on()
    {
        Assert.True(MfaPolicy.IsLocalPasswordMfaStepRequired(
            isSystemAdminAccount: true,
            UserRole.Admin,
            isTestAccount: false,
            testAccountsActive: false,
            authenticatorEnabled: false,
            systemAdminRequireTwoFactor: true));
    }

    [Fact]
    public async Task Login_writes_audit_for_system_admin()
    {
        await using var db = CreateDb();
        var audit = new RecordingAdminAudit();
        var user = await SeedSystemAdminAsync(db, enrolled: false);
        await SeedPasswordAsync(db, user, "BreakGlass-Password-99!");

        var auth = CreateAuthController(db, audit, requireTwoFactor: false);
        var login = await auth.LocalLogin(
            new LocalLoginRequest(user.Email, "BreakGlass-Password-99!"),
            CancellationToken.None);
        Assert.IsType<OkObjectResult>(login.Result);
        Assert.Contains(audit.Entries, e => e.Action == AdminAuditKeys.AuthSystemAdminLogin);
    }

    [Fact]
    public async Task Login_requires_mfa_when_system_admin_2fa_required()
    {
        await using var db = CreateDb();
        var user = await SeedSystemAdminAsync(db, enrolled: false);
        await SeedPasswordAsync(db, user, "BreakGlass-Password-99!");

        var auth = CreateAuthController(db, new RecordingAdminAudit(), requireTwoFactor: true);
        var login = await auth.LocalLogin(
            new LocalLoginRequest(user.Email, "BreakGlass-Password-99!"),
            CancellationToken.None);
        var body = Assert.IsType<LocalLoginResponse>(Assert.IsType<OkObjectResult>(login.Result).Value);
        Assert.True(body.RequiresMfa);
        Assert.False(body.MfaEnrolled);
    }

    private static async Task<User> SeedSystemAdminAsync(JobsyDbContext db, bool enrolled)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "sysadmin@lobsy.nl",
            FullName = "System",
            Role = UserRole.Admin,
            IsActive = true,
            IsSystemAdminAccount = true,
            AuthenticatorEnabled = enrolled,
            TermsAcceptedAt = DateTime.UtcNow
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
            Email = user.Email,
            PasswordHash = JobsyPasswordHasher.Hash(password)
        });
        await db.SaveChangesAsync();
    }

    private static AuthController CreateAuthController(
        JobsyDbContext db,
        IAdminAuditLog audit,
        bool requireTwoFactor)
    {
        var config = new ConfigurationBuilder().Build();
        var notifier = new SystemAdminLoginNotifier(
            new StubSystemAdminSettings { Email = "sysadmin@lobsy.nl", RequireTwoFactor = requireTwoFactor },
            audit,
            mailer: null,
            new AlwaysOnFeatures(),
            NullLogger<SystemAdminLoginNotifier>.Instance);

        return new AuthController(
            db,
            config,
            new IntegrationCredentialService(db, new PassthroughSecretProtector()),
            new AmbassadeurAttributionService(db, new AmbassadeurSettingsService(db), NullLogger<AmbassadeurAttributionService>.Instance, new AlwaysOnFeatures()),
            new StubHostEnvironment(),
            new DeviceSessionService(db, config, new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()), NullLogger<DeviceSessionService>.Instance),
            new MfaTrustedDeviceService(db),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            new MfaChallengeService(new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions())),
            new AlwaysOnFeatures(),
            new UnknownAccountLockoutTracker("test"),
            new AccountLockoutPolicy(config),
            new StubTestAccountsRuntime(),
            new StubSystemAdminSettings { RequireTwoFactor = requireTwoFactor },
            notifier,
            NullLogger<AuthController>.Instance,
            Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.AuthOptions()))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static IConfiguration Config(bool enabled, string? password = null, string email = "breakglass@lobsy.nl")
    {
        var dict = new Dictionary<string, string?>
        {
            ["SystemAdmin:Enabled"] = enabled.ToString(),
            ["SystemAdmin:Email"] = email,
            ["SystemAdmin:RequireTwoFactor"] = "false"
        };
        if (password is not null)
        {
            dict["SystemAdmin:Password"] = password;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static IServiceProvider Services(JobsyDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton<JobsyDbContext>(sp => sp.GetRequiredService<JobsyDbContext>());
        return services.BuildServiceProvider();
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("SystemAdmin-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class RecordingAdminAudit : IAdminAuditLog
    {
        public List<AdminAuditEntry> Entries { get; } = [];

        public Task WriteAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public void Stage(AdminAuditEntry entry) => Entries.Add(entry);
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
