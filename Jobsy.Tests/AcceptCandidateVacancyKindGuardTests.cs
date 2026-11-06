using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Jobsy.Infrastructure.Services.EmployerPhase2;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

/// <summary>
/// Owner rule: stage/vrijwilliger = free accept, no Maqqie. Paid jobs = ½ token pilot + Zelf/Maqqie.
/// </summary>
[Trait("Suite", "EmployerPhase2")]
public class AcceptCandidateVacancyKindGuardTests
{
    [Theory]
    [InlineData(VacancyKind.Internship)]
    [InlineData(VacancyKind.Volunteer)]
    public void Unpaid_kinds_never_charge_or_offer_Maqqie(VacancyKind kind)
    {
        Assert.False(AcceptCandidateVacancyRules.ChargesTokensOnAccept(kind));
        Assert.False(AcceptCandidateVacancyRules.SupportsEmploymentModeChoice(kind));
        Assert.Equal(0m, AcceptCandidateVacancyRules.ResolveAcceptCostTokens(
            kind,
            new FlexCommercialSettings
            {
                AcceptCandidatePilotCostTokens = 0.5m,
                AcceptCandidatePilotEndsOn = DateOnly.MaxValue,
                AcceptCandidateStandardCostTokens = 1m
            },
            DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public void Regular_vacancy_uses_pilot_accept_cost()
    {
        var cost = AcceptCandidateVacancyRules.ResolveAcceptCostTokens(
            VacancyKind.Regular,
            new FlexCommercialSettings
            {
                AcceptCandidatePilotCostTokens = 0.5m,
                AcceptCandidatePilotEndsOn = DateOnly.MaxValue,
                AcceptCandidateStandardCostTokens = 1m
            },
            DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(0.5m, cost);
        Assert.True(AcceptCandidateVacancyRules.SupportsEmploymentModeChoice(VacancyKind.Regular));
    }

    [Theory]
    [InlineData(VacancyKind.Internship)]
    [InlineData(VacancyKind.Volunteer)]
    public async Task Accept_at_zero_balance_is_free_without_spend(VacancyKind kind)
    {
        await using var db = CreateDb();
        var (companyId, appId, actorId) = await SeedPendingApplicationAsync(db, kind, tokenBalance: 0m);
        var sut = CreatePhase2Service(db);

        var result = await sut.AcceptApplicationAsync(appId, actorId);

        Assert.True(result.Succeeded, result.UserMessage);
        Assert.Equal(0, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend));
        var placement = await db.ApplicationPlacements.SingleAsync(p => p.ApplicationId == appId);
        Assert.Equal(0m, placement.AcceptCostTokens);
        Assert.Equal(ApplicationStatus.Accepted, await db.Applications.Where(a => a.Id == appId).Select(a => a.Status).SingleAsync());
    }

    [Fact]
    public async Task Accept_regular_at_zero_balance_fails_insufficient_tokens()
    {
        await using var db = CreateDb();
        var (_, appId, actorId) = await SeedPendingApplicationAsync(db, VacancyKind.Regular, tokenBalance: 0m);
        SeedAcceptCost(db);
        var sut = CreatePhase2Service(db);

        var result = await sut.AcceptApplicationAsync(appId, actorId);

        Assert.False(result.Succeeded);
        Assert.Equal("insufficient_tokens", result.ErrorCode);
        Assert.Equal(0, await db.ApplicationPlacements.CountAsync());
    }

    [Fact]
    public async Task Accept_regular_first_placement_free_when_commercial_flag_on()
    {
        await using var db = CreateDb();
        var (_, appId, actorId) = await SeedPendingApplicationAsync(db, VacancyKind.Regular, tokenBalance: 0m);
        var settings = await db.FlexCommercialSettings.SingleAsync();
        settings.FirstEmployerAcceptanceFreeEnabled = true;
        await db.SaveChangesAsync();
        var sut = CreatePhase2Service(db);

        var result = await sut.AcceptApplicationAsync(appId, actorId);

        Assert.True(result.Succeeded, result.UserMessage);
        Assert.Equal(0, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend));
        var placement = await db.ApplicationPlacements.SingleAsync(p => p.ApplicationId == appId);
        Assert.Equal(0m, placement.AcceptCostTokens);
    }

    [Theory]
    [InlineData(VacancyKind.Internship)]
    [InlineData(VacancyKind.Volunteer)]
    public async Task Employment_mode_choice_rejected_for_unpaid_kinds(VacancyKind kind)
    {
        await using var db = CreateDb();
        var (_, appId, actorId) = await SeedPendingApplicationAsync(db, kind, tokenBalance: 0m);
        var sut = CreatePhase2Service(db);
        Assert.True((await sut.AcceptApplicationAsync(appId, actorId)).Succeeded);

        var choice = await sut.ChooseEmploymentModeAsync(appId, PlacementEmploymentMode.Maqqie, actorId);
        Assert.False(choice.Succeeded);
        Assert.Equal("not_applicable", choice.ErrorCode);
    }

    [Fact]
    public async Task Maqqie_week_one_credit_skipped_for_volunteer_placement()
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
            Title = "Vrijwilliger",
            Description = "x",
            HourlyWage = 0,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Status = VacancyStatus.Active,
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Horeca,
            Location = new GeoPoint(52.0, 4.3),
            Kind = VacancyKind.Volunteer
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
            AcceptCostTokens = 0m,
            EmploymentMode = PlacementEmploymentMode.Maqqie
        });
        await db.SaveChangesAsync();

        var sut = CreatePhase2Service(db);
        await sut.TryCreditMaqqieWeekOneAsync(appId);

        Assert.Equal(0, await db.TokenTransactions.CountAsync());
    }

    [Fact]
    public async Task Candidate_Maqqie_hours_inactive_for_internship_placement()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        SeedCompany(db, companyId);
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Stage",
            Description = "x",
            HourlyWage = 0,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Status = VacancyStatus.Active,
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Horeca,
            Location = new GeoPoint(52.0, 4.3),
            Kind = VacancyKind.Internship
        });
        db.Applications.Add(new Application
        {
            Id = appId,
            VacancyId = vacancyId,
            CandidateUserId = candidateId,
            CandidateName = "Stagiair",
            Status = ApplicationStatus.Accepted,
            CreatedAt = DateTime.UtcNow
        });
        db.ApplicationPlacements.Add(new ApplicationPlacement
        {
            ApplicationId = appId,
            BillingCompanyId = companyId,
            AcceptCostTokens = 0m,
            EmploymentMode = PlacementEmploymentMode.Maqqie,
            EmploymentModeChosenAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var hours = new MaqqieHoursService(
            db,
            CreatePhase2Service(db),
            new NoopMaqqieExporter(),
            Microsoft.Extensions.Options.Options.Create(new MaqqieHoursOptions()));

        Assert.False(await hours.CandidateHasActiveMaqqieContractAsync(candidateId));
    }

    private static void SeedAcceptCost(JobsyDbContext db)
    {
        db.TokenSpendCosts.Add(new TokenSpendCost
        {
            Id = Guid.NewGuid(),
            Reason = TokenSpendReason.AcceptCandidate,
            CostTokens = 0.5m,
            IsActive = true
        });
        db.SaveChanges();
    }

    private static async Task<(Guid CompanyId, Guid ApplicationId, Guid ActorId)> SeedPendingApplicationAsync(
        JobsyDbContext db,
        VacancyKind kind,
        decimal tokenBalance)
    {
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        SeedCompany(db, companyId);
        db.Users.Add(new User
        {
            Id = actorId,
            Email = "employer@jobsy.local",
            FullName = "Employer",
            Role = UserRole.EnterpriseManager,
            IsActive = true
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Vacature",
            Description = "x",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Status = VacancyStatus.Active,
            RequiredTransport = TransportMode.Bike,
            WorkTypes = WorkType.Horeca,
            Location = new GeoPoint(52.0, 4.3),
            Kind = kind
        });
        db.Applications.Add(new Application
        {
            Id = appId,
            VacancyId = vacancyId,
            CandidateUserId = Guid.NewGuid(),
            CandidateName = "Kandidaat",
            Status = ApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        if (tokenBalance > 0)
        {
            db.TokenTransactions.Add(new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Amount = tokenBalance,
                Kind = TokenTransactionKind.Grant,
                OldBalance = 0,
                NewBalance = tokenBalance,
                CreatedAt = DateTime.UtcNow
            });
        }

        db.FlexCommercialSettings.Add(new FlexCommercialSettings
        {
            Id = FlexCommercialService.SettingsSingletonId,
            AcceptCandidatePilotCostTokens = 0.5m,
            AcceptCandidatePilotEndsOn = DateOnly.MaxValue,
            AcceptCandidateStandardCostTokens = 1m,
            FirstEmployerAcceptanceFreeEnabled = false,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return (companyId, appId, actorId);
    }

    private static EmployerPhase2Service CreatePhase2Service(JobsyDbContext db)
        => new(
            db,
            new Phase2OnFlags(),
            new TokenLedgerService(db),
            new FlexCommercialService(db),
            new ApplicationStatusRecorder(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<ApplicationStatusRecorder>.Instance),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<EmployerPhase2Service>.Instance);

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

    private sealed class NoopMaqqieExporter : IMaqqieHoursExporter
    {
        public Task ExportWeekAsync(MaqqieHoursWeek week, Application application, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
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
