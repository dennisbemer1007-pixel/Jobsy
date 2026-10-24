using Jobsy.Core.Enums;
using Jobsy.Core.Ops;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests.TestAccounts;

public sealed class TestAccountsSollicitatiesSeedTests
{
    private const string FakePassword = "fake-Password-For-Tests-1";

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
