using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.CandidateExternalVacancies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class ExternalVacancyEmployerOnboardingTests
{
    [Fact]
    public async Task CompleteRegistration_links_vacancy_and_application()
    {
        await using var db = CreateDb();
        var candidateId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var outboundId = Guid.NewGuid();
        var extVacId = Guid.NewGuid();

        db.Users.Add(new User
        {
            Id = candidateId,
            Email = "kandidaat@test.nl",
            FullName = "Sanne Test",
            Role = UserRole.Candidate,
            IsActive = true,
            HomeLocation = new GeoPoint(52.07, 4.30)
        });
        var tableId = Guid.NewGuid();
        SeedCompanyWithTable(db, companyId, tableId);
        await db.SaveChangesAsync();

        var links = new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance);
        var createdLink = await links.CreateAsync(
            OneTimeLinkPurpose.ExternalVacancyEmployerInvite,
            userId: null,
            companyId: null,
            "werkgever@test.nl",
            OneTimeLinkRules.ExternalVacancyEmployerInviteLifetime,
            candidateId);

        db.CandidateExternalVacancies.Add(new CandidateExternalVacancy
        {
            Id = extVacId,
            CandidateUserId = candidateId,
            SourceUrl = "https://example.com/job",
            SourceHost = "example.com",
            Title = "Hulp in de winkel",
            CompanyName = "Demo BV",
            Place = "Den Haag",
            Status = CandidateExternalVacancyStatus.Applied
        });
        db.CandidateExternalVacancyOutbounds.Add(new CandidateExternalVacancyOutbound
        {
            Id = outboundId,
            ExternalVacancyId = extVacId,
            EmployerEmailNormalized = "werkgever@test.nl",
            Motivation = "Ik wil graag werken.",
            InitialSentAtUtc = DateTime.UtcNow,
            OneTimeLinkId = createdLink.Id
        });
        await db.SaveChangesAsync();

        var drafts = new VacancyDraftCreationService(
            db,
            new AlwaysOkSalary(),
            new NoOpModeration());
        var products = new StubVacancyProducts();
        var recorder = new ApplicationStatusRecorder(db, NullLogger<ApplicationStatusRecorder>.Instance);
        var sut = new ExternalVacancyEmployerOnboardingService(
            db,
            links,
            drafts,
            products,
            recorder,
            NullLogger<ExternalVacancyEmployerOnboardingService>.Instance);

        var employerId = Guid.NewGuid();
        var result = await sut.CompleteRegistrationAsync(outboundId, companyId, employerId);

        Assert.True(result.Succeeded);
        Assert.Contains("/werkgever/sollicitaties", result.PostActivationWebPath, StringComparison.Ordinal);

        var ext = await db.CandidateExternalVacancies.FirstAsync(v => v.Id == extVacId);
        Assert.NotNull(ext.LinkedVacancyId);
        Assert.NotNull(ext.ApplicationId);
        var app = await db.Applications.FirstAsync(a => a.Id == ext.ApplicationId!.Value);
        Assert.Equal(candidateId, app.CandidateUserId);
        Assert.Equal("Ik wil graag werken.", app.Motivation);
    }

    [Fact]
    public async Task TryResolveOutboundId_requires_matching_email()
    {
        await using var db = CreateDb();
        var outboundId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var links = new OneTimeLinkService(db, NullLogger<OneTimeLinkService>.Instance);
        var createdLink = await links.CreateAsync(
            OneTimeLinkPurpose.ExternalVacancyEmployerInvite,
            userId: null,
            companyId: null,
            "werkgever@test.nl",
            OneTimeLinkRules.ExternalVacancyEmployerInviteLifetime,
            candidateId);
        db.CandidateExternalVacancies.Add(new CandidateExternalVacancy
        {
            Id = Guid.NewGuid(),
            CandidateUserId = candidateId,
            SourceUrl = "https://x.nl",
            SourceHost = "x.nl",
            Title = "T",
            CompanyName = "C",
            Place = "P"
        });
        await db.SaveChangesAsync();
        var extId = await db.CandidateExternalVacancies.Select(v => v.Id).FirstAsync();
        db.CandidateExternalVacancyOutbounds.Add(new CandidateExternalVacancyOutbound
        {
            Id = outboundId,
            ExternalVacancyId = extId,
            EmployerEmailNormalized = "werkgever@test.nl",
            Motivation = "x",
            InitialSentAtUtc = DateTime.UtcNow,
            OneTimeLinkId = createdLink.Id
        });
        await db.SaveChangesAsync();

        var sut = new ExternalVacancyEmployerOnboardingService(
            db,
            links,
            new VacancyDraftCreationService(db, new AlwaysOkSalary(), new NoOpModeration()),
            new StubVacancyProducts(),
            new ApplicationStatusRecorder(db, NullLogger<ApplicationStatusRecorder>.Instance),
            NullLogger<ExternalVacancyEmployerOnboardingService>.Instance);

        var ok = await sut.TryResolveOutboundIdAsync(createdLink.Token, "werkgever@test.nl");
        var bad = await sut.TryResolveOutboundIdAsync(createdLink.Token, "other@test.nl");
        Assert.Equal(outboundId, ok);
        Assert.Null(bad);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static void SeedCompanyWithTable(JobsyDbContext db, Guid companyId, Guid tableId)
    {
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Demo BV",
            Address = "Straat 1",
            KvkNumber = "12345678",
            KvkEstablishmentId = "123456780001",
            Location = new GeoPoint(52.08, 4.31),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        var table = new CompanySalaryTable
        {
            Id = tableId,
            CompanyId = companyId,
            Name = "WML",
            IsActive = true,
            IsSystemWml = true
        };
        table.Rates.Add(new CompanySalaryRate
        {
            Id = Guid.NewGuid(),
            SalaryTableId = tableId,
            AgeYears = 21,
            HourlyRate = 14.50m
        });
        db.CompanySalaryTables.Add(table);
    }

    private sealed class AlwaysOkSalary : ISalaryService
    {
        public bool MeetsMinimumWage(decimal hourlyWage, int ageYears) => hourlyWage > 0;
        public decimal GetMinimumHourlyWage(int ageYears) => 14m;
    }

    private sealed class NoOpModeration : IVacancyContentModerationService
    {
        public Task<VacancyContentModerationResult> CheckAsync(
            string title,
            string description,
            CancellationToken cancellationToken = default)
            => Task.FromResult(VacancyContentModerationResult.Allowed());
    }

    private sealed class StubVacancyProducts : IVacancyProductService
    {
        public Task<VacancyProductOutcome> PublishAsync(
            Vacancy vacancy,
            VacancyPublishOptions options,
            Guid? actorUserId,
            bool allowPendingApproval = true,
            CancellationToken cancellationToken = default)
        {
            vacancy.Status = VacancyStatus.Active;
            vacancy.PublishedAtUtc = DateTime.UtcNow;
            return Task.FromResult(new VacancyProductOutcome(true, null, vacancy));
        }

        public Task<VacancyProductOutcome> ApprovePublishAsync(
            Vacancy vacancy,
            Guid? actorUserId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PushBomPreview> PreviewPushBomAsync(
            Vacancy vacancy,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<VacancyProductOutcome> DeactivateAsync(
            Vacancy vacancy,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<VacancyProductOutcome> HighlightAsync(
            Vacancy vacancy,
            Guid? actorUserId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<VacancyProductOutcome> PushBomAsync(
            Vacancy vacancy,
            Guid? actorUserId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<VacancyProductOutcome> ExtendAsync(
            Vacancy vacancy,
            Guid? actorUserId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
