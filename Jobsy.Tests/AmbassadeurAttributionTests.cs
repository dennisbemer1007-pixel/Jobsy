using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class AmbassadeurAttributionTests
{
    [Fact]
    public async Task Attribute_Candidate_And_Company_Via_TrackingCode()
    {
        await using var db = CreateDb();
        var ambassadeurId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.Users.Add(new User
        {
            Id = ambassadeurId,
            Email = "am@test.local",
            FullName = "AM",
            Role = UserRole.Ambassadeur,
            IsActive = true
        });
        db.AmbassadeurProfiles.Add(new AmbassadeurProfile
        {
            Id = Guid.NewGuid(),
            UserId = ambassadeurId,
            TrackingCode = "AM-TEST01",
            BaseCommissionPercentage = 5m,
            AgreementSignedAt = now,
            AgreementVersion = AmbassadeurCommissionRules.CurrentAgreementVersion,
            OnboardingCompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CompanyName = "AM BV",
            KvkNumber = "12345678",
            VatNumber = "NL123456789B01",
            Address = "Straat 1",
            PostalCode = "1234AB",
            City = "Delft"
        });
        db.AmbassadeurSettings.Add(new AmbassadeurSettings
        {
            Id = Guid.NewGuid(),
            CandidateThreshold = 50,
            PercentPerThreshold = 1m,
            MaxCommissionPercentage = 15m,
            UpdatedAtUtc = now
        });
        await db.SaveChangesAsync();

        var settings = new AmbassadeurSettingsService(db);
        var attribution = new AmbassadeurAttributionService(
            db, settings, NullLogger<AmbassadeurAttributionService>.Instance);

        var candidateId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = candidateId,
            Email = "cand@test.local",
            FullName = "Cand",
            Role = UserRole.Candidate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        Assert.True(await attribution.TryAttributeCandidateAsync(candidateId, "AM-TEST01"));
        var candidate = await db.Users.SingleAsync(u => u.Id == candidateId);
        Assert.Equal(ambassadeurId, candidate.ReferredByAmbassadeurUserId);
        Assert.Equal("AM-TEST01", candidate.ReferredByAmbassadeurTrackingCode);

        // Idempotent
        Assert.False(await attribution.TryAttributeCandidateAsync(candidateId, "AM-TEST01"));

        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Co",
            KvkNumber = "87654321",
            Address = "X",
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52.0, 4.3),
            Type = CompanyType.Employer
        });
        await db.SaveChangesAsync();

        Assert.True(await attribution.TryAttributeCompanyAsync(companyId, "AM-TEST01"));
        var company = await db.Companies.SingleAsync(c => c.Id == companyId);
        Assert.Equal(ambassadeurId, company.ReferredByAmbassadeurUserId);
        Assert.True(company.PendingStartHighlightBonus);
        Assert.Equal(0.05m, company.CommissionAmbassadeurRateSnapshot);
    }

    [Fact]
    public async Task Dashboard_Counts_Candidates_And_Applications()
    {
        await using var db = CreateDb();
        var ambassadeurId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.Users.Add(new User
        {
            Id = ambassadeurId,
            Email = "am2@test.local",
            FullName = "AM2",
            Role = UserRole.Ambassadeur,
            IsActive = true
        });
        db.AmbassadeurProfiles.Add(new AmbassadeurProfile
        {
            Id = Guid.NewGuid(),
            UserId = ambassadeurId,
            TrackingCode = "AM-TEST02",
            BaseCommissionPercentage = 5m,
            AgreementSignedAt = now,
            OnboardingCompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CompanyName = "AM2",
            KvkNumber = "12345678",
            VatNumber = "NL123456789B01",
            Address = "A",
            PostalCode = "1234AB",
            City = "Delft"
        });
        db.AmbassadeurSettings.Add(new AmbassadeurSettings
        {
            Id = Guid.NewGuid(),
            CandidateThreshold = 50,
            PercentPerThreshold = 1m,
            MaxCommissionPercentage = 15m,
            UpdatedAtUtc = now
        });

        var c1 = Guid.NewGuid();
        var c2 = Guid.NewGuid();
        db.Users.AddRange(
            new User
            {
                Id = c1, Email = "c1@t.local", FullName = "C1", Role = UserRole.Candidate,
                IsActive = true, ReferredByAmbassadeurUserId = ambassadeurId
            },
            new User
            {
                Id = c2, Email = "c2@t.local", FullName = "C2", Role = UserRole.Candidate,
                IsActive = true, ReferredByAmbassadeurUserId = ambassadeurId
            });

        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Co",
            KvkNumber = "11111111",
            Address = "A",
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52.0, 4.3)
        });
        var vacancyId = Guid.NewGuid();
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "T",
            Description = "D",
            Status = VacancyStatus.Active,
            StartDate = DateOnly.FromDateTime(now),
            EndDate = DateOnly.FromDateTime(now.AddDays(30)),
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52.0, 4.3),
            RequiredTransport = TransportMode.Bike
        });
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            VacancyId = vacancyId,
            CandidateUserId = c1,
            CandidateName = "C1",
            CandidateEmail = "c1@t.local",
            PreferredTransport = "bike",
            CreatedAt = now
        });
        await db.SaveChangesAsync();

        var ledger = new CommissionLedgerService(db);
        var settings = new AmbassadeurSettingsService(db);
        var dashboard = new AmbassadeurDashboardService(db, ledger, settings);
        var dto = await dashboard.GetDashboardAsync(ambassadeurId);

        Assert.NotNull(dto);
        Assert.Equal(2, dto!.RegisteredCandidates);
        Assert.Equal(1, dto.CandidateApplications);
        Assert.Equal(5.0m, dto.CurrentCommissionPercentage);
        Assert.Equal("AM-TEST02", dto.TrackingCode);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
