using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class TokenLedgerServiceTests
{
    [Fact]
    public async Task TrySpend_publish_uses_configured_cost_and_updates_balance()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Co",
            KvkNumber = "1",
            Address = "a",
            Location = new GeoPoint(51.9, 4.2)
        });
        db.TokenSpendCosts.Add(new TokenSpendCost
        {
            Id = Guid.NewGuid(),
            Reason = TokenSpendReason.Publish,
            CostTokens = 1m,
            IsActive = true
        });
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

        var sut = new Infrastructure.Services.TokenLedgerService(db);
        var result = await sut.TrySpendAsync(companyId, TokenSpendReason.Publish);

        Assert.True(result.Succeeded);
        Assert.Equal(4m, result.Balance);
        Assert.Equal(-1m, result.Transaction!.Amount);
        Assert.Equal(TokenSpendReason.Publish, result.Transaction.Reason);
    }

    [Fact]
    public async Task TrySpend_fails_when_insufficient_balance()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Co",
            KvkNumber = "1",
            Address = "a",
            Location = new GeoPoint(51.9, 4.2)
        });
        db.TokenSpendCosts.Add(new TokenSpendCost
        {
            Id = Guid.NewGuid(),
            Reason = TokenSpendReason.PushBom,
            CostTokens = 3m,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var sut = new Infrastructure.Services.TokenLedgerService(db);
        var result = await sut.TrySpendAsync(companyId, TokenSpendReason.PushBom);

        Assert.False(result.Succeeded);
        Assert.Contains("Onvoldoende", result.ErrorMessage);
    }

    [Fact]
    public async Task Grant_rejects_non_positive_amount()
    {
        await using var db = CreateDb();
        var sut = new Infrastructure.Services.TokenLedgerService(db);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.GrantAsync(Guid.NewGuid(), 0));
    }

    [Fact]
    public async Task TrySpend_onSuccessBeforeCommit_persists_side_effect_atomically()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Co",
            KvkNumber = "1",
            Address = "a",
            Location = new GeoPoint(51.9, 4.2)
        });
        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            Title = "T",
            Description = "D",
            HourlyWage = 14,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Status = VacancyStatus.Draft,
            CompanyId = companyId,
            Location = new GeoPoint(51.9, 4.2),
            RequiredTransport = TransportMode.Bike
        });
        db.TokenSpendCosts.Add(new TokenSpendCost
        {
            Id = Guid.NewGuid(),
            Reason = TokenSpendReason.Publish,
            CostTokens = 1m,
            IsActive = true
        });
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

        var vacancy = await db.Vacancies.SingleAsync(v => v.Id == vacancyId);
        var sut = new Infrastructure.Services.TokenLedgerService(db);
        var result = await sut.TrySpendAsync(
            companyId,
            TokenSpendReason.Publish,
            vacancyId: vacancyId,
            onSuccessBeforeCommit: _ =>
            {
                vacancy.Status = VacancyStatus.Active;
                return Task.CompletedTask;
            });

        Assert.True(result.Succeeded);
        Assert.Equal(VacancyStatus.Active, (await db.Vacancies.SingleAsync(v => v.Id == vacancyId)).Status);
        Assert.Equal(1m, await db.TokenTransactions.Where(t => t.CompanyId == companyId).SumAsync(t => t.Amount));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
