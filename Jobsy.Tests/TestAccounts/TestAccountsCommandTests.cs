using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Ops;
using Jobsy.Infrastructure;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests.TestAccounts;

public class TestAccountsCommandTests
{
    private const string FakePassword = "fake-Password-For-Tests-1";

    private static (JobsyDbContext Db, IConfiguration Config) CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new JobsyDbContext(options);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestAccounts:EmailDomain"] = "lobsy.nl",
                ["TestAccounts:Password:Candidate"] = FakePassword,
                ["TestAccounts:Password:CandidateNew"] = FakePassword,
                ["TestAccounts:Password:BranchManager"] = FakePassword,
                ["TestAccounts:Password:EnterpriseManager"] = FakePassword,
                ["TestAccounts:Password:RegionalManager"] = FakePassword,
                ["TestAccounts:Password:Intermediary"] = FakePassword,
                ["TestAccounts:Password:SalesManager"] = FakePassword,
                ["TestAccounts:Password:Admin"] = FakePassword,
                ["TestAccounts:Password:Ambassadeur"] = FakePassword,
                ["TestAccounts:Password:Teacher"] = FakePassword,
                ["TestAccounts:Password:SchoolAdmin"] = FakePassword,
            })
            .Build();
        return (db, config);
    }

    [Fact]
    public async Task Seed_creates_catalog_and_second_seed_is_unchanged()
    {
        var (db, config) = CreateDb();
        var seeder = new TestAccountsSeedService(db, config);
        var first = await seeder.SeedAsync(dryRun: false, onlyKeys: null);
        await db.SaveChangesAsync();
        Assert.True(first.Created >= 9);
        Assert.False(first.AdminRealAccountConflict);

        var second = await seeder.SeedAsync(dryRun: false, onlyKeys: null);
        await db.SaveChangesAsync();
        Assert.Equal(0, second.Created);
        Assert.True(second.Unchanged + second.Updated >= 9);
        Assert.All(await db.Users.Where(u => u.IsTestAccount).ToListAsync(), u =>
        {
            Assert.False(u.AuthenticatorEnabled);
            Assert.Null(u.AuthenticatorSecret);
        });
    }

    [Fact]
    public async Task Missing_password_is_skipped()
    {
        var (db, _) = CreateDb();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestAccounts:EmailDomain"] = "lobsy.nl",
                ["TestAccounts:Password:Admin"] = FakePassword,
            })
            .Build();
        var seeder = new TestAccountsSeedService(db, config);
        var result = await seeder.SeedAsync(false, null);
        Assert.True(result.AnySkipped);
        Assert.Contains(result.Rows, r => r.AccountKey == "Candidate" && r.Action == TestAccountSeedAction.Skipped);
    }

    [Fact]
    public async Task Real_user_with_test_email_is_untouched()
    {
        var (db, config) = CreateDb();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "test-admin@lobsy.nl",
            FullName = "Real Admin",
            Role = UserRole.Admin,
            IsActive = true,
            IsTestAccount = false
        });
        await db.SaveChangesAsync();

        var seeder = new TestAccountsSeedService(db, config);
        var result = await seeder.SeedAsync(false, null);
        Assert.True(result.AdminRealAccountConflict);
        var user = await db.Users.SingleAsync(u => u.Email == "test-admin@lobsy.nl");
        Assert.False(user.IsTestAccount);
        Assert.Equal("Real Admin", user.FullName);
    }

    [Fact]
    public async Task Dry_run_writes_nothing()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        await using var db = new JobsyDbContext(options);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestAccounts:EmailDomain"] = "lobsy.nl",
                ["TestAccounts:Password:Admin"] = FakePassword,
            })
            .Build();
        var seeder = new TestAccountsSeedService(db, config);
        var plan = await seeder.SeedAsync(dryRun: true, onlyKeys: null);
        Assert.Contains(plan.Rows, r => r.Action == TestAccountSeedAction.Created);
        // No SaveChanges — a second context on the same store must stay empty (CLI rolls back).
        await using var verify = new JobsyDbContext(options);
        Assert.Equal(0, await verify.Users.CountAsync());
    }

    [Fact]
    public async Task Password_rotation_bumps_session_version()
    {
        var (db, config) = CreateDb();
        var seeder = new TestAccountsSeedService(db, config);
        await seeder.SeedAsync(false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin" });
        await db.SaveChangesAsync();
        var user = await db.Users.SingleAsync(u => u.Email == "test-admin@lobsy.nl");
        var before = user.SessionVersion;

        var rotated = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestAccounts:EmailDomain"] = "lobsy.nl",
                ["TestAccounts:Password:Admin"] = "fake-Password-For-Tests-2!",
            })
            .Build();
        var seeder2 = new TestAccountsSeedService(db, rotated);
        var result = await seeder2.SeedAsync(false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin" });
        await db.SaveChangesAsync();
        Assert.Contains(result.Rows, r => r.Action == TestAccountSeedAction.Updated);
        Assert.True(user.SessionVersion > before);
        Assert.True(JobsyPasswordHasher.Verify("fake-Password-For-Tests-2!",
            (await db.LocalAuthCredentials.SingleAsync(c => c.UserId == user.Id)).PasswordHash));
    }

    [Fact]
    public void Cli_host_resolves_no_hosted_services()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:JobsyDb"] =
                "Host=localhost;Port=5432;Database=JobsyDb;Username=postgres;Password=postgres"
        });
        builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
        builder.Services.RemoveAll<IHostedService>();
        using var host = builder.Build();
        Assert.Empty(host.Services.GetServices<IHostedService>());
    }

    [Fact]
    public void Guard_refusal_codes_do_not_leak_fake_password()
    {
        var input = new TestAccountGuardInput
        {
            DeploymentEnvironment = "Production",
            TestAccountsEnabled = false,
            RenderServiceName = "jobsy-api",
            PublicWebHost = "lobsy.nl",
            DatabaseName = "jobsy",
            AllowStubPayments = false,
            HasLiveMollieKey = true,
            HostEnvironmentName = "Production"
        };
        var blob = string.Join(' ', TestAccountEnvironmentGuard.Evaluate(input).Failures.Select(f => f.Message));
        Assert.DoesNotContain(FakePassword, blob, StringComparison.Ordinal);
    }
}
