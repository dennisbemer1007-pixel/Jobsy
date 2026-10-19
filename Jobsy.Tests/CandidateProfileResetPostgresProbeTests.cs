using System.Security.Claims;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests;

/// <summary>Reproduces profile reset against real Postgres FK constraints (local/CI only).</summary>
public sealed class CandidateProfileResetPostgresProbeTests
{
    private const string Connection =
        "Host=127.0.0.1;Port=5432;Database=jobsy_reset_probe;Username=jobsy;Password=jobsy";

    [Fact]
    public async Task Reset_succeeds_with_verified_application_after_match_and_full_candidate_data()
    {
        if (!await CanConnectAsync())
        {
            return;
        }

        await EnsureDatabaseAsync();

        var candidateId = Guid.NewGuid();
        var employerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var interviewId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        const string email = "fake-reset-probe@test.local";

        await using var sp = BuildProvider();
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        await db.Database.MigrateAsync();

        db.Users.AddRange(
            new User
            {
                Id = candidateId,
                Email = email,
                FullName = "Probe Kandidaat",
                Role = UserRole.Candidate,
                IsActive = true,
                OpenForWork = true,
                PreferencesJson = """{"roles":["horeca"]}""",
                CandidateHowToCompletedAt = DateTime.UtcNow
            },
            new User
            {
                Id = employerId,
                Email = "employer-probe@test.local",
                FullName = "Werkgever",
                Role = UserRole.BranchManager,
                IsActive = true
            });
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Probe BV",
            Address = "Straat 1",
            KvkNumber = "12345678",
            Location = new GeoPoint(52, 4),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Functie",
            Description = "Beschrijving",
            HourlyWage = 15m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = VacancyStatus.Active,
            Location = new GeoPoint(52, 4),
            RequiredTransport = TransportMode.Bike
        });
        db.Applications.Add(new Application
        {
            Id = appId,
            VacancyId = vacancyId,
            CandidateUserId = candidateId,
            CandidateName = "Probe Kandidaat",
            CandidateEmail = email,
            PreferredTransport = "Bike",
            Status = ApplicationStatus.Accepted,
            EmailVerifiedAt = DateTime.UtcNow,
            Motivation = "Graag",
            MatchPercent = 88,
            CreatedAt = DateTime.UtcNow
        });
        db.ApplicationMessages.Add(new ApplicationMessage
        {
            Id = Guid.NewGuid(),
            ApplicationId = appId,
            SenderUserId = candidateId,
            Body = "Hallo",
            CreatedAtUtc = DateTime.UtcNow
        });
        db.ApplicationOutcomes.Add(new ApplicationOutcome
        {
            ApplicationId = appId,
            Outcome = AfterMatchOutcomeKind.NextRound,
            Reason = "Kennismaking",
            RecordedByUserId = employerId,
            CreatedAtUtc = DateTime.UtcNow
        });
        db.Interviews.Add(new Interview
        {
            Id = interviewId,
            ApplicationId = appId,
            CalendarUid = "uid-probe",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.InterviewSlots.Add(new InterviewSlot
        {
            Id = slotId,
            InterviewId = interviewId,
            StartUtc = DateTime.UtcNow.AddDays(2),
            EndUtc = DateTime.UtcNow.AddDays(2).AddHours(1),
            ProposedBy = InterviewSlotProposer.Employer
        });
        db.InterviewReminders.Add(new InterviewReminder
        {
            Id = Guid.NewGuid(),
            InterviewId = interviewId,
            UserId = candidateId,
            Moment = InterviewReminderMoment.DayBefore18,
            Channel = InterviewReminderChannel.Email,
            ScheduledForUtc = DateTime.UtcNow.AddDays(1)
        });
        db.AfterMatchReminderPreferences.Add(new AfterMatchReminderPreference
        {
            UserId = candidateId,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.CandidateMatchSnapshots.Add(new CandidateMatchSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            MatchesJson = "[]",
            ComputedAtUtc = DateTime.UtcNow
        });
        db.CandidateTestReflections.Add(new CandidateTestReflection
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            Kind = AssessmentKind.Competence,
            Level = TestDepthLevel.First,
            Language = "nl",
            ContentJson = """{"title":"Spiegel"}""",
            GeneratedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.DismissedVestigingSuggestions.Add(new DismissedVestigingSuggestion
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            KvkEstablishmentId = "12345678901234567890",
            HiddenUntilUtc = DateTime.UtcNow.AddDays(30),
            DismissedByUserId = candidateId
        });
        db.WorkInitiatives.Add(new WorkInitiative
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidateId,
            RootCompanyId = companyId,
            Motivation = "Initiatief",
            SnapshotRoleTitle = "Horeca",
            SnapshotRegionLabel = "Utrecht",
            SnapshotWorkDnaSummary = "Team",
            SnapshotAvailabilitySummary = "Flex",
            CandidateDisplayName = "Probe",
            CandidateEmail = email,
            ResponseDeadlineUtc = DateTime.UtcNow.AddDays(5),
            Status = WorkInitiativeStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        db.UserNotifications.Add(new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = candidateId,
            Title = "Melding",
            Body = "Tekst",
            Category = "AfterMatch",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new CandidateProfileResetService(db, new StubUserLookup(db));
        var ex = await Record.ExceptionAsync(() =>
            service.ResetAsync(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Email, email)], "test")),
                "VERWIJDER",
                "nl"));

        Assert.Null(ex);
        Assert.False(await db.CandidateMatchSnapshots.AnyAsync(m => m.UserId == candidateId));
        var app = await db.Applications.AsNoTracking().SingleAsync(a => a.Id == appId);
        Assert.Null(app.CandidateUserId);
        Assert.Equal("Verwijderde gebruiker", app.CandidateName);
    }

    private static async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var conn = new Npgsql.NpgsqlConnection(Connection);
            await conn.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task EnsureDatabaseAsync()
    {
        var admin = new Npgsql.NpgsqlConnectionStringBuilder(Connection) { Database = "postgres" };
        await using (var conn = new Npgsql.NpgsqlConnection(admin.ConnectionString))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'jobsy_reset_probe'";
            var exists = await cmd.ExecuteScalarAsync();
            if (exists is null)
            {
                await using var create = conn.CreateCommand();
                create.CommandText = "CREATE DATABASE jobsy_reset_probe OWNER jobsy";
                await create.ExecuteNonQueryAsync();
            }
        }

        await using var dbConn = new Npgsql.NpgsqlConnection(Connection);
        await dbConn.OpenAsync();
        await using (var ext = dbConn.CreateCommand())
        {
            ext.CommandText = "CREATE EXTENSION IF NOT EXISTS postgis";
            await ext.ExecuteNonQueryAsync();
        }
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddDbContext<JobsyDbContext>(o =>
            o.UseNpgsql(Connection, npgsql => npgsql.UseNetTopologySuite()));
        return services.BuildServiceProvider();
    }

    private sealed class StubUserLookup(JobsyDbContext db) : IUserLookupService
    {
        public Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
        {
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            return db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);
        }
    }
}
