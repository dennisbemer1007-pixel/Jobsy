using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services.EmployerPhase2;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

[Trait("Suite", "EmployerPhase2")]
public class MaqqieHoursServiceTests
{
    [Fact]
    public async Task Employer_return_then_candidate_can_resubmit()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var weekId = Guid.NewGuid();
        SeedCompany(db, companyId);
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Test",
            Description = "x",
            HourlyWage = 10,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Status = VacancyStatus.Active,
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Horeca,
            Location = new GeoPoint(52.0, 4.3)
        });
        db.Applications.Add(new Application
        {
            Id = appId,
            VacancyId = vacancyId,
            CandidateUserId = candidateId,
            CandidateName = "Jan",
            Status = ApplicationStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        });
        db.ApplicationPlacements.Add(new ApplicationPlacement
        {
            ApplicationId = appId,
            BillingCompanyId = companyId,
            EmploymentMode = PlacementEmploymentMode.Maqqie
        });
        db.MaqqieHoursWeeks.Add(new MaqqieHoursWeek
        {
            Id = weekId,
            ApplicationId = appId,
            WeekStart = new DateOnly(2026, 10, 5),
            Status = MaqqieHoursWeekStatus.Submitted,
            DailyHoursJson = "{\"1\":8}",
            TotalHours = 8,
            SubmittedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var returned = await sut.EmployerReturnWeekAsync(appId, weekId, Guid.NewGuid(), "Check maandag nog eens.", default);
        Assert.True(returned);

        var week = await db.MaqqieHoursWeeks.SingleAsync();
        Assert.Equal(MaqqieHoursWeekStatus.ReturnedToCandidate, week.Status);
        Assert.Equal("Check maandag nog eens.", week.EmployerReturnNote);

        var updated = await sut.UpsertDraftWeekAsync(candidateId, week.WeekStart, new Dictionary<int, decimal> { [1] = 7 }, default);
        Assert.NotNull(updated);
        Assert.Equal(MaqqieHoursWeekStatus.Draft, updated!.Status);

        var submitted = await sut.SubmitWeekAsync(candidateId, weekId, default);
        Assert.True(submitted);
        Assert.Equal(MaqqieHoursWeekStatus.Submitted, (await db.MaqqieHoursWeeks.SingleAsync()).Status);
    }

    [Fact]
    public async Task Employer_overview_lists_submitted_maqqie_weeks_only()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        SeedCompany(db, companyId);
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Baan",
            Description = "x",
            HourlyWage = 10,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Status = VacancyStatus.Active,
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Horeca,
            Location = new GeoPoint(52.0, 4.3)
        });
        db.Applications.Add(new Application
        {
            Id = appId,
            VacancyId = vacancyId,
            CandidateUserId = Guid.NewGuid(),
            CandidateName = "Piet",
            Status = ApplicationStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        });
        db.ApplicationPlacements.Add(new ApplicationPlacement
        {
            ApplicationId = appId,
            BillingCompanyId = companyId,
            EmploymentMode = PlacementEmploymentMode.Maqqie
        });
        db.MaqqieHoursWeeks.Add(new MaqqieHoursWeek
        {
            Id = Guid.NewGuid(),
            ApplicationId = appId,
            WeekStart = new DateOnly(2026, 10, 5),
            Status = MaqqieHoursWeekStatus.Submitted,
            TotalHours = 32
        });
        db.MaqqieHoursWeeks.Add(new MaqqieHoursWeek
        {
            Id = Guid.NewGuid(),
            ApplicationId = appId,
            WeekStart = new DateOnly(2026, 9, 29),
            Status = MaqqieHoursWeekStatus.Draft,
            TotalHours = 0
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var overview = await sut.GetEmployerOverviewAsync(Guid.NewGuid(), new HashSet<Guid> { companyId }, default);
        Assert.NotNull(overview);
        Assert.Single(overview!.Weeks);
        Assert.Equal("Piet", overview.Weeks[0].CandidateName);
    }

    private static MaqqieHoursService CreateSut(JobsyDbContext db)
        => new(
            db,
            new Phase2OnEmployerPhase2Service(db),
            new NoopExporter(),
            Microsoft.Extensions.Options.Options.Create(new MaqqieHoursOptions()));

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static void SeedCompany(JobsyDbContext db, Guid companyId)
    {
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Co",
            KvkNumber = "12345678",
            Address = "Test",
            Location = new GeoPoint(52.0, 4.3),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
    }

    private sealed class Phase2OnEmployerPhase2Service : IEmployerPhase2Service
    {
        public Phase2OnEmployerPhase2Service(JobsyDbContext _) { }

        public Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<EmployerPhase2AcceptResult> AcceptApplicationAsync(
            Guid applicationId,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<EmployerPhase2PlacementResult> ChooseEmploymentModeAsync(
            Guid applicationId,
            PlacementEmploymentMode mode,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<EmployerPhase2WalletDto> GetWalletAsync(
            Guid companyId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task TryCreditMaqqieWeekOneAsync(Guid applicationId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoopExporter : IMaqqieHoursExporter
    {
        public Task ExportWeekAsync(MaqqieHoursWeek week, Application application, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
