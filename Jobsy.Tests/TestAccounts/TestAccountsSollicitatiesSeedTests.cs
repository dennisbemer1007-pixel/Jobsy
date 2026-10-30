using Jobsy.Core.Enums;
using Jobsy.Core.Ops;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Jobsy.Tests.TestAccounts;

public sealed class TestAccountsSollicitatiesSeedTests
{
    private const string FakePassword = "fake-Password-For-Tests-1";

    [Fact]
    public async Task Ensure_persists_demo_vacancies_before_applications_on_fresh_db()
    {
        var (db, candidateId, branchManagerId) = await SeedUsersAsync();
        var logger = NullLogger.Instance;

        await TestAccountsSollicitatiesDemoSeeder.EnsureForCandidateAsync(
            db, candidateId, branchManagerId, logger);

        var demoVacancyIds = new[]
        {
            TestAccountsIds.SollicDemoVacancySent,
            TestAccountsIds.SollicDemoVacancySeen,
            TestAccountsIds.SollicDemoVacancyPick,
            TestAccountsIds.SollicDemoVacancyScheduled,
            TestAccountsIds.SollicDemoVacancyMessages,
            TestAccountsIds.SollicDemoVacancyDone
        };
        foreach (var vacancyId in demoVacancyIds)
        {
            Assert.True(await db.Vacancies.AnyAsync(v => v.Id == vacancyId));
        }

        await db.SaveChangesAsync();
        Assert.Equal(6, await db.Applications.CountAsync(a => a.CandidateUserId == candidateId));

        var (outcome, apps, threads) = await TestAccountsSollicitatiesDemoSeeder.BuildDemoHealthCountsAsync(
            db,
            candidateId);
        Assert.Equal(6, apps);
        Assert.True(threads >= 2, outcome);
    }

    [Fact]
    public async Task Ensure_is_idempotent_and_seeds_mockup_rows()
    {
        var (db, candidateId, branchManagerId) = await SeedUsersAsync();
        var logger = NullLogger.Instance;

        await TestAccountsSollicitatiesDemoSeeder.EnsureForCandidateAsync(
            db, candidateId, branchManagerId, logger);
        await db.SaveChangesAsync();

        Assert.Equal(6, await db.Applications.CountAsync(a => a.CandidateUserId == candidateId));
        var demoCompanyIds = new[]
        {
            TestAccountsIds.SollicDemoCompanyHoreca,
            TestAccountsIds.SollicDemoCompanyRetail,
            TestAccountsIds.SollicDemoCompanyGroen,
            TestAccountsIds.SollicDemoCompanyBlauw,
            TestAccountsIds.SollicDemoCompanyGrijs
        };
        Assert.Equal(5, await db.Companies.CountAsync(c => demoCompanyIds.Contains(c.Id)));
        var demoVacancyIds = new[]
        {
            TestAccountsIds.SollicDemoVacancySent,
            TestAccountsIds.SollicDemoVacancySeen,
            TestAccountsIds.SollicDemoVacancyPick,
            TestAccountsIds.SollicDemoVacancyScheduled,
            TestAccountsIds.SollicDemoVacancyMessages,
            TestAccountsIds.SollicDemoVacancyDone
        };
        Assert.Equal(6, await db.Vacancies.CountAsync(v => demoVacancyIds.Contains(v.Id)));
        Assert.Single(await db.CandidateExternalVacancies.Where(v => v.CandidateUserId == candidateId).ToListAsync());
        Assert.Equal(2, await db.WorkInitiatives.CountAsync(i => i.CandidateUserId == candidateId));

        var seen = await db.ApplicationStatusHistories.AnyAsync(
            h => h.ApplicationId == TestAccountsIds.SollicDemoAppSeen
                 && h.Kind == ApplicationStatusEventKind.EmployerViewed);
        Assert.True(seen);

        var pickInterview = await db.Interviews.SingleAsync(i => i.ApplicationId == TestAccountsIds.SollicDemoAppPick);
        var pickSlots = await db.InterviewSlots.CountAsync(s => s.InterviewId == pickInterview.Id);
        Assert.Equal(3, pickSlots);
        Assert.Equal(InterviewStatus.Proposing, pickInterview.Status);

        var scheduled = await db.Interviews.SingleAsync(i => i.ApplicationId == TestAccountsIds.SollicDemoAppScheduled);
        Assert.Equal(InterviewStatus.Scheduled, scheduled.Status);
        Assert.NotNull(scheduled.ChosenSlotId);

        var unread = await db.ApplicationMessages.SingleAsync(m => m.ApplicationId == TestAccountsIds.SollicDemoAppMessages);
        Assert.Null(unread.ReadAtUtc);

        var external = await db.CandidateExternalVacancies.SingleAsync(v => v.Id == TestAccountsIds.SollicDemoExternalVacancy);
        Assert.Equal(CandidateExternalVacancyStatus.Applied, external.Status);

        await TestAccountsSollicitatiesDemoSeeder.EnsureForCandidateAsync(
            db, candidateId, branchManagerId, logger);
        await db.SaveChangesAsync();

        Assert.Equal(6, await db.Applications.CountAsync(a => a.CandidateUserId == candidateId));
        Assert.Equal(2, await db.WorkInitiatives.CountAsync(i => i.CandidateUserId == candidateId));
        Assert.Equal(3, await db.InterviewSlots.CountAsync(s => s.InterviewId == pickInterview.Id));
    }

    [Fact]
    public async Task Unread_messages_demo_resets_ReadAtUtc_when_demo_seeder_runs_again()
    {
        var (db, candidateId, branchManagerId) = await SeedUsersAsync();
        var logger = NullLogger.Instance;

        await TestAccountsSollicitatiesDemoSeeder.EnsureForCandidateAsync(
            db, candidateId, branchManagerId, logger);
        await db.SaveChangesAsync();

        var grijsAppId = TestAccountsIds.SollicDemoAppMessages;
        var message = await db.ApplicationMessages.SingleAsync(m => m.ApplicationId == grijsAppId);
        message.ReadAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await TestAccountsSollicitatiesDemoSeeder.EnsureForCandidateAsync(
            db, candidateId, branchManagerId, logger);
        await db.SaveChangesAsync();

        var after = await db.ApplicationMessages.SingleAsync(m => m.ApplicationId == grijsAppId);
        Assert.Null(after.ReadAtUtc);
    }

    [Fact]
    public async Task TryEnsure_on_postgres_migrated_db_seeds_six_verified_applications()
    {
        var baseConnection = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(baseConnection))
        {
            return;
        }

        var admin = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres", Pooling = false };
        try
        {
            await using var probe = new NpgsqlConnection(admin.ConnectionString);
            await probe.OpenAsync();
        }
        catch (Exception ex) when (ex is NpgsqlException or System.Net.Sockets.SocketException or TimeoutException)
        {
            return;
        }

        var databaseName = $"lobsy_sollic_demo_{Guid.NewGuid():N}";
        var builder = new NpgsqlConnectionStringBuilder(baseConnection)
        {
            Database = databaseName,
            Pooling = false
        };
        await ExecuteAdminAsync(admin.ConnectionString, $"CREATE DATABASE \"{databaseName}\"");
        try
        {
            var services = new ServiceCollection();
            services.AddDbContext<JobsyDbContext>(o =>
                o.UseNpgsql(builder.ConnectionString, npgsql => npgsql.UseNetTopologySuite()));
            await using var sp = services.BuildServiceProvider();
            await using (var scope = sp.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
                await db.Database.MigrateAsync();
            }

            await using (var scope = sp.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
                var config = BuildConfig();
                var seeder = new TestAccountsSeedService(db, config);
                await seeder.SeedAsync(dryRun: false, onlyKeys: null);
                await db.SaveChangesAsync();

                var grok = await db.Users.SingleAsync(u => u.Email == "test-kandidaat@lobsy.nl");
                grok.Email = TestAccountsSollicitatiesDemoSeeder.AcceptatiePrimaryCandidateEmail;
                grok.IsTestAccount = true;
                await db.SaveChangesAsync();

                var branchManager = await db.Users.SingleAsync(u => u.Email == "test-werkgever@lobsy.nl");
                var applied = await TestAccountsSollicitatiesDemoSeeder.TryEnsureForAcceptatieAsync(
                    db,
                    config,
                    TestAccountEnvironmentGuard.Evaluate(ValidAcceptatieGuardInput()),
                    NullLogger.Instance);
                Assert.True(applied);
                Assert.Equal(
                    6,
                    await db.Applications.CountAsync(a => a.CandidateUserId == grok.Id && a.EmailVerifiedAt != null));
                _ = branchManager;
            }
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await ExecuteAdminAsync(admin.ConnectionString, $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)");
        }
    }

    private static async Task ExecuteAdminAsync(string connectionString, string sql)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task TryEnsure_promotes_grokbot_without_test_account_flag()
    {
        var (db, _, branchManagerId) = await SeedUsersAsync(includeGrokbotCandidate: true);
        var grok = await db.Users.SingleAsync(u => u.Email == TestAccountsSollicitatiesDemoSeeder.AcceptatiePrimaryCandidateEmail);
        grok.IsTestAccount = false;
        await db.SaveChangesAsync();

        var applied = await TestAccountsSollicitatiesDemoSeeder.TryEnsureForAcceptatieAsync(
            db,
            BuildConfig(),
            TestAccountEnvironmentGuard.Evaluate(ValidAcceptatieGuardInput()),
            NullLogger.Instance);
        Assert.True(applied);
        Assert.True((await db.Users.SingleAsync(u => u.Id == grok.Id)).IsTestAccount);
        Assert.Equal(6, await db.Applications.CountAsync(a => a.CandidateUserId == grok.Id));
        _ = branchManagerId;
    }

    [Fact]
    public void Api_startup_guard_allows_acc_api_without_public_web_host()
    {
        var input = new TestAccountGuardInput
        {
            DeploymentEnvironment = "Acceptatie",
            TestAccountsEnabled = true,
            RenderServiceName = "lobsy-acc-api",
            PublicWebHost = null,
            DatabaseName = "lobsy",
            ConnectionStringParseFailed = false,
            AllowStubPayments = false,
            HasLiveMollieKey = false,
            HostEnvironmentName = "Production",
            ExpectedDatabaseName = "lobsy",
            IsWebRuntime = true
        };

        var guard = TestAccountEnvironmentGuard.Evaluate(input);
        Assert.True(guard.Allowed, guard.CodesSummary());
    }

    [Fact]
    public void IsAcceptatieDemoCandidateEmail_includes_grokbot_kandidaat_and_catalog()
    {
        var config = BuildConfig();
        Assert.True(TestAccountsSollicitatiesDemoSeeder.IsAcceptatieDemoCandidateEmail(
            TestAccountsSollicitatiesDemoSeeder.AcceptatiePrimaryCandidateEmail,
            config));
        Assert.True(TestAccountsSollicitatiesDemoSeeder.IsAcceptatieDemoCandidateEmail(
            "kandidaat@jobsy.local",
            config));
        Assert.False(TestAccountsSollicitatiesDemoSeeder.IsAcceptatieDemoCandidateEmail(
            "random@example.com",
            config));
    }

    [Fact]
    public async Task TryEnsure_seeds_requesting_kandidaat_jobsy_local_not_grokbot()
    {
        var (db, grokId, _) = await SeedUsersAsync(includeGrokbotCandidate: true);
        await TestAccountsSollicitatiesDemoSeeder.TryEnsureForAcceptatieAsync(
            db,
            BuildConfig(),
            TestAccountEnvironmentGuard.Evaluate(ValidAcceptatieGuardInput()),
            NullLogger.Instance);
        Assert.Equal(6, await db.Applications.CountAsync(a => a.CandidateUserId == grokId));

        var kandidaatId = Guid.NewGuid();
        db.Users.Add(new Jobsy.Core.Entities.User
        {
            Id = kandidaatId,
            Email = "kandidaat@jobsy.local",
            FullName = "Sanne de Vries",
            Role = UserRole.Candidate,
            IsActive = true,
            IsTestAccount = true
        });
        await db.SaveChangesAsync();

        var applied = await TestAccountsSollicitatiesDemoSeeder.TryEnsureForAcceptatieAsync(
            db,
            BuildConfig(),
            TestAccountEnvironmentGuard.Evaluate(ValidAcceptatieGuardInput()),
            NullLogger.Instance,
            kandidaatId);
        Assert.True(applied);
        Assert.Equal(6, await db.Applications.CountAsync(a => a.CandidateUserId == kandidaatId));
        Assert.Equal(0, await db.Applications.CountAsync(a => a.CandidateUserId == grokId));
    }

    [Fact]
    public async Task Seed_reset_purge_runs_even_when_catalog_candidate_missing()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        var kandidaatId = Guid.NewGuid();
        db.Users.Add(new Jobsy.Core.Entities.User
        {
            Id = kandidaatId,
            Email = "kandidaat@jobsy.local",
            FullName = "Sanne",
            Role = UserRole.Candidate,
            IsActive = true,
            IsTestAccount = true
        });
        var staleId = Guid.NewGuid();
        db.CandidateExternalVacancies.Add(new Jobsy.Core.Entities.CandidateExternalVacancy
        {
            Id = staleId,
            CandidateUserId = kandidaatId,
            SourceUrl = "https://example.com/stale",
            SourceHost = "example.com",
            Title = "Stale",
            CompanyName = "BV",
            Status = CandidateExternalVacancyStatus.Applied,
            SavedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var seeder = new TestAccountsSeedService(db, BuildConfig());
        Assert.True(await seeder.RepairCompleteCandidateAssessmentsAsync());

        Assert.False(await db.CandidateExternalVacancies.AnyAsync(v => v.Id == staleId));
    }

    [Fact]
    public async Task Purge_stale_external_removes_linked_job_search_activity()
    {
        var (db, candidateId, _) = await SeedUsersAsync();
        var staleId = Guid.NewGuid();
        db.CandidateExternalVacancies.Add(new Jobsy.Core.Entities.CandidateExternalVacancy
        {
            Id = staleId,
            CandidateUserId = candidateId,
            SourceUrl = "https://example.com/stale",
            SourceHost = "example.com",
            Title = "Stale",
            CompanyName = "BV",
            Status = CandidateExternalVacancyStatus.Applied,
            SavedAtUtc = DateTime.UtcNow,
        });
        db.JobSearchActivities.Add(new Jobsy.Core.Entities.JobSearchActivity
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidateId,
            ActivityDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Kind = JobSearchActivityKind.Applied,
            Employer = "BV",
            FunctionTitle = "Stale",
            Source = JobSearchActivitySource.Auto,
            SourceEntityType = "ExternalVacancy",
            SourceEntityId = staleId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await TestAccountsSollicitatiesDemoSeeder.PurgeAcceptatieDemoExternalVacanciesAsync(
            db,
            BuildConfig(),
            NullLogger.Instance);

        Assert.False(await db.CandidateExternalVacancies.AnyAsync(v => v.Id == staleId));
        Assert.False(await db.JobSearchActivities.AnyAsync(a => a.SourceEntityId == staleId));
    }

    [Fact]
    public async Task TryEnsure_uses_primary_grokbot_email_when_test_account()
    {
        var (db, _, branchManagerId) = await SeedUsersAsync(includeGrokbotCandidate: true);
        var config = BuildConfig();
        var guard = TestAccountEnvironmentGuard.Evaluate(ValidAcceptatieGuardInput());

        var applied = await TestAccountsSollicitatiesDemoSeeder.TryEnsureForAcceptatieAsync(
            db,
            config,
            guard,
            NullLogger.Instance);
        Assert.True(applied);

        var grok = await db.Users.SingleAsync(u => u.Email == TestAccountsSollicitatiesDemoSeeder.AcceptatiePrimaryCandidateEmail);
        Assert.Equal(6, await db.Applications.CountAsync(a => a.CandidateUserId == grok.Id));
        _ = branchManagerId;
    }

    [Fact]
    public async Task TryEnsure_returns_false_when_guard_refused()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        var config = BuildConfig();
        var guard = TestAccountGuardResult.Refused(
            new TestAccountGuardFailure(TestAccountGuardCheck.SwitchOff, "switch_off", "off"));

        var applied = await TestAccountsSollicitatiesDemoSeeder.TryEnsureForAcceptatieAsync(
            db,
            config,
            guard,
            NullLogger.Instance);
        Assert.False(applied);
        Assert.Equal(0, await db.Applications.CountAsync());
    }

    [Fact]
    public async Task TryEnsure_uses_any_branch_manager_when_catalog_missing()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        var grokId = Guid.NewGuid();
        db.Users.Add(new Jobsy.Core.Entities.User
        {
            Id = grokId,
            Email = TestAccountsSollicitatiesDemoSeeder.AcceptatiePrimaryCandidateEmail,
            FullName = "Grok Test",
            Role = UserRole.Candidate,
            IsActive = true,
            IsTestAccount = false
        });
        var managerId = Guid.NewGuid();
        db.Users.Add(new Jobsy.Core.Entities.User
        {
            Id = managerId,
            Email = "real-manager@lobsy.nl",
            FullName = "Manager",
            Role = UserRole.BranchManager,
            IsActive = true,
            IsTestAccount = false
        });
        await db.SaveChangesAsync();

        var applied = await TestAccountsSollicitatiesDemoSeeder.TryEnsureForAcceptatieAsync(
            db,
            BuildConfig(),
            TestAccountEnvironmentGuard.Evaluate(ValidAcceptatieGuardInput()),
            NullLogger.Instance);
        Assert.True(applied);
        Assert.Equal(6, await db.Applications.CountAsync(a => a.CandidateUserId == grokId));
    }

    [Fact]
    public async Task Ensure_removes_stale_applications_before_reseeding_demo_rows()
    {
        var staleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa9999");
        var (db, candidateId, branchManagerId) = await SeedUsersAsync();
        db.Applications.Add(new Jobsy.Core.Entities.Application
        {
            Id = staleId,
            VacancyId = TestAccountsIds.SollicDemoVacancySent,
            CandidateUserId = candidateId,
            CandidateName = "Stale",
            Status = ApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            EmailVerifiedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await TestAccountsSollicitatiesDemoSeeder.EnsureForCandidateAsync(
            db, candidateId, branchManagerId, NullLogger.Instance);
        await db.SaveChangesAsync();

        Assert.Equal(6, await db.Applications.CountAsync(a => a.CandidateUserId == candidateId));
        Assert.False(await db.Applications.AnyAsync(a => a.Id == staleId));
    }

    [Fact]
    public async Task Ensure_skips_non_test_candidate()
    {
        var (db, _, branchManagerId) = await SeedUsersAsync();
        var real = new Jobsy.Core.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = "real-user@lobsy.nl",
            FullName = "Real",
            Role = UserRole.Candidate,
            IsActive = true,
            IsTestAccount = false
        };
        db.Users.Add(real);
        await db.SaveChangesAsync();

        await TestAccountsSollicitatiesDemoSeeder.EnsureForCandidateAsync(
            db, real.Id, branchManagerId, NullLogger.Instance);
        await db.SaveChangesAsync();

        Assert.Equal(0, await db.Applications.CountAsync(a => a.CandidateUserId == real.Id));
    }

    private static TestAccountGuardInput ValidAcceptatieGuardInput() => new()
    {
        DeploymentEnvironment = "Acceptatie",
        TestAccountsEnabled = true,
        RenderServiceName = "lobsy-acc-api",
        PublicWebHost = "acceptatie.lobsy.nl",
        DatabaseName = "lobsy",
        ConnectionStringParseFailed = false,
        AllowStubPayments = true,
        HasLiveMollieKey = false,
        HostEnvironmentName = "Production",
        AllowedPublicHosts = ["acceptatie.lobsy.nl"],
        ExpectedDatabaseName = "lobsy",
        IsWebRuntime = false
    };

    private static IConfiguration BuildConfig()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestAccounts:EmailDomain"] = "lobsy.nl",
                ["TestAccounts:Password:Candidate"] = FakePassword,
                ["TestAccounts:Password:BranchManager"] = FakePassword,
            })
            .Build();

    private static async Task<(JobsyDbContext Db, Guid CandidateId, Guid BranchManagerId)> SeedUsersAsync(
        bool includeGrokbotCandidate = false)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new JobsyDbContext(options);
        var config = BuildConfig();
        var seeder = new TestAccountsSeedService(db, config);
        await seeder.SeedAsync(dryRun: false, onlyKeys: null);
        await db.SaveChangesAsync();

        if (includeGrokbotCandidate)
        {
            var catalog = await db.Users.SingleAsync(u => u.Email == "test-kandidaat@lobsy.nl");
            catalog.Email = TestAccountsSollicitatiesDemoSeeder.AcceptatiePrimaryCandidateEmail;
            await db.SaveChangesAsync();
        }

        var candidate = includeGrokbotCandidate
            ? await db.Users.SingleAsync(u => u.Email == TestAccountsSollicitatiesDemoSeeder.AcceptatiePrimaryCandidateEmail)
            : await db.Users.SingleAsync(u => u.Email == "test-kandidaat@lobsy.nl");
        var branchManager = await db.Users.SingleAsync(u => u.Email == "test-werkgever@lobsy.nl");
        return (db, candidate.Id, branchManager.Id);
    }
}
