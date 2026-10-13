using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Ops;
using Jobsy.Core.Rules;
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
    public async Task Repair_overwrites_placeholder_scores_without_resetting_preferences()
    {
        var (db, config) = CreateDb();
        var seeder = new TestAccountsSeedService(db, config);
        await seeder.SeedAsync(false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Candidate" });
        await db.SaveChangesAsync();

        var user = await db.Users.SingleAsync(u => u.Email == "test-kandidaat@lobsy.nl");
        const string preferences = """{"roles":["logistiek"],"years":8}""";
        user.PreferencesJson = preferences;

        var competency = await db.CandidateCompetencies.SingleAsync(c => c.UserId == user.Id);
        competency.SamenwerkenPercent = 1;
        competency.ResultaatgerichtheidPercent = 65;
        competency.StressbestendigheidPercent = 60;
        competency.InnovatiePercent = 55;
        competency.ExtraversiePercent = 50;

        var values = await db.CandidateValuesProfiles.SingleAsync(c => c.UserId == user.Id);
        values.AutonomyPercent = 1;
        values.ConnectionPercent = 65;
        values.AchievementPercent = 60;
        values.StabilityPercent = 55;
        values.ImpactPercent = 50;

        var career = await db.CandidateCareerInterests.SingleAsync(c => c.UserId == user.Id);
        career.Status = CandidateCompetencyStatuses.Completed;
        career.AnswersJson = CareerTestCatalog.SerializeAnswers(new Dictionary<int, int> { [1] = 3 });
        career.RealisticPercent = null;
        career.InvestigativePercent = null;
        career.ArtisticPercent = null;
        career.SocialPercent = null;
        career.EnterprisingPercent = null;
        career.ConventionalPercent = null;

        var nieuwId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = nieuwId,
            Email = "test-kandidaat-nieuw@lobsy.nl",
            FullName = "Nieuw",
            Role = UserRole.Candidate,
            IsActive = true,
            IsTestAccount = true
        });
        db.CandidateCompetencies.Add(new CandidateCompetency
        {
            Id = Guid.NewGuid(),
            UserId = nieuwId,
            Status = CandidateCompetencyStatuses.Completed,
            SamenwerkenPercent = 13,
            ResultaatgerichtheidPercent = 65,
            StressbestendigheidPercent = 60,
            InnovatiePercent = 55,
            ExtraversiePercent = 50,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        Assert.True(await seeder.RepairCompleteCandidateAssessmentsAsync());
        await db.SaveChangesAsync();

        var expectedCompetency = CompetencyTestCatalog.Score(FullLikert(CompetencyTestCatalog.QuestionCount))!;
        Assert.NotEqual(1, expectedCompetency.Samenwerken);
        competency = await db.CandidateCompetencies.SingleAsync(c => c.UserId == user.Id);
        Assert.Equal(expectedCompetency.Samenwerken, competency.SamenwerkenPercent);
        Assert.Equal(expectedCompetency.Extraversie, competency.ExtraversiePercent);
        Assert.True(CompetencyTestCatalog.IsComplete(CompetencyTestCatalog.ParseAnswersJson(competency.AnswersJson)));

        var expectedValues = SchwartzValuesCatalog.Score(FullLikert(SchwartzValuesCatalog.QuestionCount))!;
        values = await db.CandidateValuesProfiles.SingleAsync(c => c.UserId == user.Id);
        Assert.Equal(expectedValues.Autonomy, values.AutonomyPercent);
        Assert.NotEqual(1, values.AutonomyPercent);

        career = await db.CandidateCareerInterests.SingleAsync(c => c.UserId == user.Id);
        Assert.NotNull(career.RealisticPercent);
        Assert.True(CareerTestCatalog.IsComplete(CareerTestCatalog.ParseAnswersJson(career.AnswersJson)));

        Assert.Equal(preferences, (await db.Users.SingleAsync(u => u.Id == user.Id)).PreferencesJson);
        var nieuw = await db.CandidateCompetencies.SingleAsync(c => c.UserId == nieuwId);
        Assert.Equal(13, nieuw.SamenwerkenPercent);
    }

    private static Dictionary<int, int> FullLikert(int count)
    {
        var map = new Dictionary<int, int>(count);
        for (var i = 1; i <= count; i++)
        {
            map[i] = ((i * 3 + (i / 7)) % 5) + 1;
        }

        return map;
    }

    [Fact]
    public void Startup_repair_is_guarded_and_does_not_run_the_full_seed()
    {
        var root = FindRepoRoot();
        var hosted = File.ReadAllText(Path.Combine(root, "Jobsy.Api", "Jobs", "DatabaseSeedHostedService.cs"));
        Assert.Contains("TestAccountEnvironmentGuard.Evaluate", hosted, StringComparison.Ordinal);
        Assert.Contains("RepairCompleteCandidateAssessmentsAsync", hosted, StringComparison.Ordinal);
        Assert.DoesNotContain("SeedAsync", hosted, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureCompleteCandidateAsync", hosted, StringComparison.Ordinal);

        var seed = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure", "Ops", "TestAccountsSeedService.cs"));
        var start = seed.IndexOf("RepairCompleteCandidateAssessmentsAsync", StringComparison.Ordinal);
        var end = seed.IndexOf("private static bool SameAnswers", StringComparison.Ordinal);
        Assert.True(start > 0 && end > start);
        var slice = seed[start..end];
        Assert.Contains("FindByKey(\"Candidate\")", slice, StringComparison.Ordinal);
        Assert.DoesNotContain("kandidaat-nieuw", slice, StringComparison.Ordinal);
        Assert.DoesNotContain("PreferencesJson", slice, StringComparison.Ordinal);
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
    public void Failure_report_names_step_and_type_without_secrets()
    {
        const string password = "super-secret-password";
        var ex = new InvalidOperationException(
            $"Host=db.internal;Username=lobsy;Password={password};Database=lobsy",
            new ArgumentException($"inner postgres://lobsy:{password}@db.internal:5432/lobsy"));
        var text = Jobsy.Api.Ops.TestAccountFailureReport.Format(ex, "school", "Teacher");

        Assert.Contains("Failed: step=school account=Teacher", text, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException:", text, StringComparison.Ordinal);
        Assert.Contains("inner: ArgumentException:", text, StringComparison.Ordinal);
        Assert.DoesNotContain(password, text, StringComparison.Ordinal);
        Assert.DoesNotContain("db.internal", text, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", text, StringComparison.Ordinal);
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
