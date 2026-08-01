using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class Sprint4TokenProductsTests
{
    [Fact]
    public async Task Publish_while_PendingApproval_is_rejected()
    {
        await using var db = CreateDb();
        var (_, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 5);
        SeedSpendCosts(db);
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        vacancy.Status = VacancyStatus.PendingApproval;
        vacancy.RequestedHighlight = true;
        await db.SaveChangesAsync();

        var sut = CreateProducts(db);
        var result = await sut.PublishAsync(vacancy, new VacancyPublishOptions(), actorUserId: null);

        Assert.False(result.Succeeded);
        Assert.Equal(VacancyStatus.PendingApproval, vacancy.Status);
        Assert.Contains("goedkeuring", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApprovePublish_applies_requested_highlight_and_extend()
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0);
        SeedSpendCosts(db);
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        vacancy.Status = VacancyStatus.PendingApproval;
        vacancy.RequestedHighlight = true;
        vacancy.RequestedExtend = true;
        var originalEnd = vacancy.EndDate;
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Amount = 5m,
            Kind = TokenTransactionKind.Grant,
            OldBalance = 0,
            NewBalance = 5m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateProducts(db);
        var result = await sut.ApprovePublishAsync(vacancy, actorUserId: null);

        Assert.True(result.Succeeded);
        Assert.Equal(VacancyStatus.Active, vacancy.Status);
        Assert.True(vacancy.IsHighlighted);
        Assert.NotNull(vacancy.HighlightedUntil);
        Assert.True(vacancy.HighlightedUntil > DateTime.UtcNow);
        Assert.Equal(1, vacancy.ExtensionCount);
        Assert.Equal(originalEnd.AddDays(VacancyProductRules.ExtendDays), vacancy.EndDate);
        Assert.False(vacancy.RequestedHighlight);
        Assert.False(vacancy.RequestedExtend);
        // Publish 1 + Highlight 1 + Extend 1 = 3 → balance 2
        Assert.Equal(2m, await db.TokenTransactions.Where(t => t.CompanyId == companyId).SumAsync(t => t.Amount));
    }

    [Fact]
    public async Task PushBom_with_zero_candidates_does_not_spend()
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 10);
        SeedSpendCosts(db);
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        vacancy.Status = VacancyStatus.Active;
        await db.SaveChangesAsync();

        var sut = CreateProducts(db);
        var result = await sut.PushBomAsync(vacancy, actorUserId: null);

        Assert.False(result.Succeeded);
        Assert.Equal(10m, await db.TokenTransactions.Where(t => t.CompanyId == companyId).SumAsync(t => t.Amount));
        Assert.Empty(db.PlatformLogs.Where(l => l.Category == "PushBom"));
    }

    [Fact]
    public async Task Publish_without_tokens_persists_requested_options()
    {
        await using var db = CreateDb();
        var (_, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0);
        SeedSpendCosts(db);
        await db.SaveChangesAsync();

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        var sut = CreateProducts(db);

        var result = await sut.PublishAsync(
            vacancy,
            new VacancyPublishOptions(Highlight: true, Extend: true),
            actorUserId: null);

        Assert.True(result.Succeeded);
        Assert.True(result.PendingApproval);
        Assert.True(vacancy.RequestedHighlight);
        Assert.True(vacancy.RequestedExtend);
        Assert.False(vacancy.RequestedPushBom);
    }

    [Fact]
    public async Task Publish_without_tokens_sets_PendingApproval()
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0);
        SeedSpendCosts(db);
        await db.SaveChangesAsync();

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        var sut = CreateProducts(db);

        var result = await sut.PublishAsync(vacancy, new VacancyPublishOptions(), actorUserId: null);

        Assert.True(result.Succeeded);
        Assert.True(result.PendingApproval);
        Assert.Equal(VacancyStatus.PendingApproval, vacancy.Status);
        Assert.Equal(0, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend));
        Assert.Contains(db.PlatformLogs, l => l.Category == "PendingApproval");
    }

    [Fact]
    public async Task Publish_with_highlight_debits_both_and_activates()
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 5);
        SeedSpendCosts(db);
        await db.SaveChangesAsync();

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        var sut = CreateProducts(db);

        var result = await sut.PublishAsync(
            vacancy,
            new VacancyPublishOptions(Highlight: true),
            actorUserId: null);

        Assert.True(result.Succeeded);
        Assert.False(result.PendingApproval);
        Assert.Equal(VacancyStatus.Active, vacancy.Status);
        Assert.True(vacancy.IsHighlighted);
        Assert.NotNull(vacancy.HighlightedUntil);
        Assert.True(vacancy.HighlightedUntil > DateTime.UtcNow);
        Assert.Equal(2, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend));
        // Grant/seed 5 − Publish 1 − Highlight 1 = 3
        Assert.Equal(3m, await db.TokenTransactions.Where(t => t.CompanyId == companyId).SumAsync(t => t.Amount));
    }

    [Fact]
    public async Task Highlight_rejects_while_active_window_is_open()
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 5);
        SeedSpendCosts(db);
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        vacancy.Status = VacancyStatus.Active;
        vacancy.IsHighlighted = true;
        vacancy.HighlightedUntil = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        var sut = CreateProducts(db);
        var result = await sut.HighlightAsync(vacancy, actorUserId: null);

        Assert.False(result.Succeeded);
        Assert.Contains("al gehighlight", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(5m, await db.TokenTransactions.Where(t => t.CompanyId == companyId).SumAsync(t => t.Amount));
    }

    [Fact]
    public async Task Highlight_renewal_after_expiry_extends_window_and_spends()
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 5);
        SeedSpendCosts(db);
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        vacancy.Status = VacancyStatus.Active;
        vacancy.IsHighlighted = true;
        vacancy.HighlightedUntil = DateTime.UtcNow.AddMinutes(-5);
        await db.SaveChangesAsync();

        var sut = CreateProducts(db);
        var before = DateTime.UtcNow;
        var result = await sut.HighlightAsync(vacancy, actorUserId: null);

        Assert.True(result.Succeeded);
        Assert.True(vacancy.IsHighlighted);
        Assert.NotNull(vacancy.HighlightedUntil);
        Assert.True(vacancy.HighlightedUntil > before.AddDays(VacancyProductRules.HighlightDays - 1));
        Assert.Equal(1, await db.TokenTransactions.CountAsync(t =>
            t.Kind == TokenTransactionKind.Spend && t.Reason == TokenSpendReason.Highlight));
        Assert.Equal(4m, await db.TokenTransactions.Where(t => t.CompanyId == companyId).SumAsync(t => t.Amount));
    }

    [Fact]
    public async Task PushBom_notifies_only_OpenForWork_within_10km()
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 10);
        SeedSpendCosts(db);

        db.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                Email = "near@jobsy.local",
                FullName = "Near",
                Role = UserRole.Candidate,
                OpenForWork = true,
                HomeLocation = new GeoPoint(51.9820, 4.2240),
                PreferencesJson = """{"roles":["horeca"],"maxTravelMinutes":30}""",
                IsActive = true
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "far@jobsy.local",
                FullName = "Far",
                Role = UserRole.Candidate,
                OpenForWork = true,
                HomeLocation = new GeoPoint(52.3700, 4.8950),
                PreferencesJson = """{"roles":["horeca"],"maxTravelMinutes":90}""",
                IsActive = true
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "closed@jobsy.local",
                FullName = "Closed",
                Role = UserRole.Candidate,
                OpenForWork = false,
                HomeLocation = new GeoPoint(51.9820, 4.2240),
                IsActive = true
            });

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        vacancy.Status = VacancyStatus.Active;
        await db.SaveChangesAsync();

        var sut = CreateProducts(db);
        var result = await sut.PushBomAsync(vacancy, actorUserId: null);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.PushBomRecipientCount);
        Assert.Single(db.PlatformLogs.Where(l => l.Category == "PushBom"));
        Assert.Contains(db.PlatformLogs, l => l.Message.Contains("n***@jobsy.local"));
        // Tier 1–9 → 1 token (not flat 3)
        Assert.Equal(9m, await db.TokenTransactions.Where(t => t.CompanyId == companyId).SumAsync(t => t.Amount));
    }

    [Fact]
    public async Task PreviewPushBom_returns_reach_and_tier_cost()
    {
        await using var db = CreateDb();
        var (_, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 10);
        SeedSpendCosts(db);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "near@jobsy.local",
            FullName = "Near",
            Role = UserRole.Candidate,
            OpenForWork = true,
            HomeLocation = new GeoPoint(51.9820, 4.2240),
            PreferencesJson = """{"maxTravelMinutes":30}""",
            IsActive = true
        });
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        vacancy.Status = VacancyStatus.Active;
        await db.SaveChangesAsync();

        var sut = CreateProducts(db);
        var preview = await sut.PreviewPushBomAsync(vacancy);

        Assert.Equal(1, preview.CandidateCount);
        Assert.Equal(1m, preview.CostTokens);
        Assert.True(preview.HasPricing);
        Assert.Equal(10, preview.RadiusKm);
        Assert.Equal(30, preview.MaxTravelMinutes);
    }

    [Fact]
    public void PushBomPricingRules_resolves_tier_by_candidate_count()
    {
        var tiers = new[]
        {
            new PushBomPricingTier { MinCandidates = 1, MaxCandidates = 9, CostTokens = 1m, IsActive = true },
            new PushBomPricingTier { MinCandidates = 10, MaxCandidates = 25, CostTokens = 2m, IsActive = true },
            new PushBomPricingTier { MinCandidates = 26, MaxCandidates = null, CostTokens = 4m, IsActive = true }
        };

        Assert.Null(PushBomPricingRules.ResolveCost(tiers, 0));
        Assert.Equal(1m, PushBomPricingRules.ResolveCost(tiers, 5));
        Assert.Equal(2m, PushBomPricingRules.ResolveCost(tiers, 10));
        Assert.Equal(2m, PushBomPricingRules.ResolveCost(tiers, 25));
        Assert.Equal(4m, PushBomPricingRules.ResolveCost(tiers, 100));
    }

    [Fact]
    public async Task Extend_adds_days_and_increments_count()
    {
        await using var db = CreateDb();
        var (_, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 5);
        SeedSpendCosts(db);
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        vacancy.Status = VacancyStatus.Active;
        var originalEnd = vacancy.EndDate;
        await db.SaveChangesAsync();

        var sut = CreateProducts(db);
        var result = await sut.ExtendAsync(vacancy, actorUserId: null);

        Assert.True(result.Succeeded);
        Assert.Equal(1, vacancy.ExtensionCount);
        Assert.Equal(originalEnd.AddDays(VacancyProductRules.ExtendDays), vacancy.EndDate);
    }

    [Fact]
    public async Task Deactivate_archives_active_vacancy()
    {
        await using var db = CreateDb();
        var (_, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0);
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        vacancy.Status = VacancyStatus.Active;
        await db.SaveChangesAsync();

        var sut = CreateProducts(db);
        var result = await sut.DeactivateAsync(vacancy);

        Assert.True(result.Succeeded);
        Assert.Equal(VacancyStatus.Archived, vacancy.Status);
        Assert.False(VacancyVisibilityRules.IsPubliclyVisible(vacancy, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public async Task ApprovePublish_spends_and_activates_pending()
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0);
        SeedSpendCosts(db);
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        vacancy.Status = VacancyStatus.PendingApproval;
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Amount = 2m,
            Kind = TokenTransactionKind.Grant,
            OldBalance = 0,
            NewBalance = 2m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = CreateProducts(db);
        var result = await sut.ApprovePublishAsync(vacancy, actorUserId: null);

        Assert.True(result.Succeeded);
        Assert.Equal(VacancyStatus.Active, vacancy.Status);
        Assert.Equal(1m, await db.TokenTransactions.Where(t => t.CompanyId == companyId).SumAsync(t => t.Amount));
    }

    [Fact]
    public void GeoDistance_within_10km_matches_westland_neighbour()
    {
        var westland = new GeoPoint(51.9812, 4.2235);
        var near = new GeoPoint(51.9850, 4.2300);
        var amsterdam = new GeoPoint(52.3700, 4.8950);

        Assert.True(GeoDistance.IsWithinKm(westland, near, 10));
        Assert.False(GeoDistance.IsWithinKm(westland, amsterdam, 10));
    }

    private static IVacancyProductService CreateProducts(JobsyDbContext db)
    {
        var features = new PlatformFeatureService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());

        return new VacancyProductService(
            db,
            new TokenLedgerService(db),
            new PushNotificationServiceStub(db, NullLogger<PushNotificationServiceStub>.Instance),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            features,
            new MockRoutingService(),
            NullLogger<VacancyProductService>.Instance);
    }

    private static void SeedSpendCosts(JobsyDbContext db)
    {
        db.TokenSpendCosts.AddRange(
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Publish, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Highlight, CostTokens = VacancyProductRules.DefaultHighlightCostTokens, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.PushBom, CostTokens = 3m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Extend, CostTokens = 1m, IsActive = true });

        if (!db.PushBomSettings.Any())
        {
            db.PushBomSettings.Add(new PushBomSettings
            {
                Id = Guid.NewGuid(),
                RadiusKm = VacancyProductRules.PushBomRadiusKm,
                MaxTravelMinutes = VacancyProductRules.PushBomMaxTravelMinutes
            });
        }

        if (!db.PushBomPricingTiers.Any())
        {
            db.PushBomPricingTiers.AddRange(
                new PushBomPricingTier { Id = Guid.NewGuid(), MinCandidates = 1, MaxCandidates = 9, CostTokens = 1m, IsActive = true },
                new PushBomPricingTier { Id = Guid.NewGuid(), MinCandidates = 10, MaxCandidates = 25, CostTokens = 2m, IsActive = true },
                new PushBomPricingTier { Id = Guid.NewGuid(), MinCandidates = 26, MaxCandidates = 50, CostTokens = 3m, IsActive = true },
                new PushBomPricingTier { Id = Guid.NewGuid(), MinCandidates = 51, MaxCandidates = null, CostTokens = 4m, IsActive = true });
        }
    }

    private static async Task<(Guid CompanyId, Guid VacancyId)> SeedDraftVacancyAsync(JobsyDbContext db, decimal tokenBalance)
    {
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Westland Demo",
            KvkNumber = "1",
            Address = "a",
            Location = new GeoPoint(51.9812, 4.2235)
        });
        db.Users.Add(new User
        {
            Id = managerId,
            Email = "enterprise@test.local",
            FullName = "Manager",
            Role = UserRole.EnterpriseManager,
            CompanyId = companyId,
            IsActive = true
        });
        db.UserCompanies.Add(new UserCompany { UserId = managerId, CompanyId = companyId });

        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            Title = "Plukker",
            Description = "Demo",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            Status = VacancyStatus.Draft,
            CompanyId = companyId,
            Location = new GeoPoint(51.9812, 4.2235),
            RequiredTransport = TransportMode.Bike
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

        await db.SaveChangesAsync();
        return (companyId, vacancyId);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
