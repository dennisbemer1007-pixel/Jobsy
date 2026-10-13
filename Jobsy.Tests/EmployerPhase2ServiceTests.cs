using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.EmployerPhase2;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

[Trait("Suite", "EmployerPhase2")]
public class EmployerPhase2ServiceTests
{
    [Fact]
    public async Task TryCreditMaqqieWeekOne_grants_accept_cost_once()
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
            CandidateUserId = Guid.NewGuid(),
            CandidateName = "Test",
            Status = ApplicationStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        });
        db.ApplicationPlacements.Add(new ApplicationPlacement
        {
            ApplicationId = appId,
            BillingCompanyId = companyId,
            AcceptCostTokens = 0.5m,
            EmploymentMode = PlacementEmploymentMode.Maqqie
        });
        db.TokenSpendCosts.Add(new TokenSpendCost
        {
            Id = Guid.NewGuid(),
            Reason = TokenSpendReason.AcceptCandidate,
            CostTokens = 0.5m,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var flags = new Phase2OnFlags();
        var sut = new EmployerPhase2Service(
            db,
            flags,
            new TokenLedgerService(db),
            new FlexCommercialService(db),
            new ApplicationStatusRecorder(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<ApplicationStatusRecorder>.Instance),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<EmployerPhase2Service>.Instance);

        await sut.TryCreditMaqqieWeekOneAsync(appId);

        var grant = await db.TokenTransactions.SingleAsync(t => t.Kind == TokenTransactionKind.Grant);
        Assert.Equal(0.5m, grant.Amount);
        var placement = await db.ApplicationPlacements.SingleAsync();
        Assert.NotNull(placement.MaqqieCreditGrantedAtUtc);

        await sut.TryCreditMaqqieWeekOneAsync(appId);
        Assert.Equal(1, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Grant));
    }

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

    private sealed class Phase2OnFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(true, true, EmployerPhase2Enabled: true));

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(feature == PlatformFeature.EmployerPhase2);

        public void Invalidate()
        {
        }
    }
}
