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

/// <summary>
/// Minimal prepaid-token publish checks: free stage/volunteer at 0 balance,
/// hard block + no negative ledger for paid (operational) publish, exact debit.
/// </summary>
public class TokenPublishBalanceChecklistTests
{
    [Theory]
    [InlineData(VacancyKind.Volunteer)]
    [InlineData(VacancyKind.Internship)]
    public async Task Zero_token_balance_can_publish_internship_or_volunteer(VacancyKind kind)
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0, kind);
        SeedSpendCosts(db);

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        var ledger = new TokenLedgerService(db);
        var result = await CreateProducts(db).PublishAsync(
            vacancy,
            new VacancyPublishOptions(),
            actorUserId: null,
            allowPendingApproval: false);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.False(result.InsufficientTokens);
        Assert.False(result.PendingApproval);
        Assert.Equal(VacancyStatus.Active, vacancy.Status);
        Assert.Equal(0m, await ledger.GetBalanceAsync(companyId));
        Assert.Equal(0, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend));
    }

    [Fact]
    public async Task Zero_token_balance_blocks_operational_publish_without_going_negative()
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0, VacancyKind.Regular);
        SeedSpendCosts(db);

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        var ledger = new TokenLedgerService(db);
        var result = await CreateProducts(db).PublishAsync(
            vacancy,
            new VacancyPublishOptions(),
            actorUserId: null,
            allowPendingApproval: false);

        Assert.False(result.Succeeded);
        Assert.True(result.InsufficientTokens);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        Assert.Contains("token", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(VacancyStatus.Draft, vacancy.Status);
        Assert.Equal(0m, result.Balance);
        Assert.True(result.RequiredTokens >= 1m);

        var balance = await ledger.GetBalanceAsync(companyId);
        Assert.Equal(0m, balance);
        Assert.Equal(0, await db.TokenTransactions.CountAsync(t => t.Kind == TokenTransactionKind.Spend));
        Assert.DoesNotContain(db.TokenTransactions, t => t.NewBalance < 0);
    }

    [Fact]
    public async Task Operational_publish_debits_exactly_one_token_by_default()
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 1m, VacancyKind.Regular);
        SeedSpendCosts(db);

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        var ledger = new TokenLedgerService(db);
        var result = await CreateProducts(db).PublishAsync(
            vacancy,
            new VacancyPublishOptions(),
            actorUserId: null,
            allowPendingApproval: false);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(VacancyStatus.Active, vacancy.Status);
        Assert.Equal(0m, await ledger.GetBalanceAsync(companyId));

        var spend = Assert.Single(db.TokenTransactions.Where(t => t.Kind == TokenTransactionKind.Spend));
        Assert.Equal(TokenSpendReason.Publish, spend.Reason);
        Assert.Equal(-1m, spend.Amount);
        Assert.Equal(1m, spend.OldBalance);
        Assert.Equal(0m, spend.NewBalance);
        Assert.Equal(vacancyId, spend.VacancyId);
    }

    [Fact]
    public async Task Operational_publish_debits_configured_category_rate()
    {
        await using var db = CreateDb();
        const decimal configuredRate = 2m;
        var categoryId = Guid.NewGuid();
        db.VacancyCategories.Add(new VacancyCategory
        {
            Id = categoryId,
            Slug = "operationeel-test",
            Name = "Operationeel test",
            ColorHex = "#F54A1B",
            PublishCostTokens = configuredRate,
            HighlightAvailable = true,
            HighlightCostTokens = 2m,
            PushBomAvailable = false,
            PlacementKind = VacancyKind.Regular,
            SortOrder = 99,
            IsActive = true
        });

        var (companyId, vacancyId) = await SeedDraftVacancyAsync(
            db, tokenBalance: configuredRate, VacancyKind.Regular, categoryId);
        SeedSpendCosts(db);

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        var ledger = new TokenLedgerService(db);
        var result = await CreateProducts(db).PublishAsync(
            vacancy,
            new VacancyPublishOptions(),
            actorUserId: null,
            allowPendingApproval: false);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(0m, await ledger.GetBalanceAsync(companyId));

        var spend = Assert.Single(db.TokenTransactions.Where(t => t.Kind == TokenTransactionKind.Spend));
        Assert.Equal(-configuredRate, spend.Amount);
        Assert.Equal(0m, spend.NewBalance);
        Assert.True(spend.NewBalance >= 0m);
    }

    private static IVacancyProductService CreateProducts(JobsyDbContext db)
    {
        var existing = db.PlatformFeatureSettings.Local.FirstOrDefault()
                       ?? db.PlatformFeatureSettings.FirstOrDefault();
        if (existing is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                FreePublishUntil = null,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.FreePublishUntil = null;
        }

        db.SaveChanges();

        var features = new PlatformFeatureService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());

        return new VacancyProductService(
            db,
            new TokenLedgerService(db),
            new SalesCommercialService(db, new TokenLedgerService(db)),
            new VacancyCategoryService(db),
            new PushNotificationServiceStub(db, NullLogger<PushNotificationServiceStub>.Instance),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            features,
            new MockRoutingService(),
            new UserNotificationService(db),
            new CandidateActionTokenService(db),
            NullLogger<VacancyProductService>.Instance);
    }

    private static void SeedSpendCosts(JobsyDbContext db)
    {
        db.TokenSpendCosts.AddRange(
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Publish, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Highlight, CostTokens = VacancyProductRules.DefaultHighlightCostTokens, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.PushBom, CostTokens = 3m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Extend, CostTokens = 1m, IsActive = true });
        db.SaveChanges();
    }

    private static async Task<(Guid CompanyId, Guid VacancyId)> SeedDraftVacancyAsync(
        JobsyDbContext db,
        decimal tokenBalance,
        VacancyKind kind,
        Guid? categoryId = null)
    {
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Token Checklist Co",
            KvkNumber = "12345678",
            Address = "Westland",
            Location = new GeoPoint(51.99, 4.22),
            KvkVerificationStatus = KvkVerificationStatus.Verified
        });

        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = kind == VacancyKind.Regular ? "Kassamedewerker" : kind.ToString(),
            Description = "Demo",
            Status = VacancyStatus.Draft,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Location = new GeoPoint(51.99, 4.22),
            RequiredTransport = TransportMode.Bike,
            Kind = kind,
            CategoryId = categoryId
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
