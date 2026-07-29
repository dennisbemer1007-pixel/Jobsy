using Jobsy.Api.Controllers;
using Jobsy.Api.Jobs;
using Jobsy.Api.Models;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class Sprint6AdminSuiteTests
{
    [Fact]
    public async Task Metrics_include_platform_only_keys_when_admin()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Employer Co",
            KvkNumber = "11111111",
            Address = "A",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        db.Companies.Add(new Company
        {
            Id = Guid.NewGuid(),
            Name = "Flex Intermediair",
            KvkNumber = "22222222",
            Address = "B",
            Location = new GeoPoint(52.1, 4.1),
            Type = CompanyType.Intermediary
        });
        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Error,
            Category = "Test",
            Message = "boom",
            CreatedAt = DateTime.UtcNow
        });
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Amount = -1m,
            Kind = TokenTransactionKind.Spend,
            Reason = TokenSpendReason.PushBom,
            OldBalance = 5,
            NewBalance = 4,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new MetricsQueryService(db);
        var admin = await sut.GetSummaryAsync(includePlatformOnly: true, companyIds: null, period: "month");
        var employer = await sut.GetSummaryAsync(includePlatformOnly: false, companyIds: [companyId], period: "month");

        Assert.Contains(admin, m => m.Key == "companies_intermediaries" && m.Value == 1);
        Assert.Contains(admin, m => m.Key == "errors" && m.Value == 1);
        Assert.Contains(admin, m => m.Key == "pushboms" && m.Value == 1);
        Assert.DoesNotContain(employer, m => m.Key == "companies_intermediaries");
        Assert.DoesNotContain(employer, m => m.Key == "errors");
    }

    [Fact]
    public async Task Tokens_purchased_excludes_grants()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Buyer",
            KvkNumber = "11111111",
            Address = "A",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        db.TokenTransactions.AddRange(
            new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Amount = 10m,
                Kind = TokenTransactionKind.Grant,
                OldBalance = 0,
                NewBalance = 10m,
                CreatedAt = DateTime.UtcNow
            },
            new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Amount = 5m,
                Kind = TokenTransactionKind.Purchase,
                OldBalance = 10m,
                NewBalance = 15m,
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var metrics = await new MetricsQueryService(db)
            .GetSummaryAsync(includePlatformOnly: true, companyIds: null, period: "month");

        Assert.Contains(metrics, m => m.Key == "tokens_purchased" && m.Value == 5m);
    }

    [Fact]
    public async Task Intermediary_company_is_counted_in_admin_metrics()
    {
        await using var db = CreateDb();
        var intermediaryCompanyId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        db.Companies.Add(new Company
        {
            Id = intermediaryCompanyId,
            Name = "Demo Intermediair Flex BV",
            KvkNumber = "55667788",
            KvkEstablishmentId = "55667788_0001",
            Address = "Binckhorstlaan 36, Den Haag",
            Type = CompanyType.Intermediary,
            Location = new GeoPoint(52.0680, 4.3350)
        });
        await db.SaveChangesAsync();

        var metrics = await new MetricsQueryService(db)
            .GetSummaryAsync(includePlatformOnly: true, companyIds: null, period: "year");

        var intermediaries = Assert.Single(metrics, m => m.Key == "companies_intermediaries");
        Assert.Equal(1, intermediaries.Value);
        Assert.Equal(CompanyType.Intermediary, await db.Companies
            .Where(c => c.Id == intermediaryCompanyId)
            .Select(c => c.Type)
            .SingleAsync());
    }

    [Fact]
    public async Task Semi_annual_wage_update_creates_new_rates()
    {
        await using var db = CreateDb();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingFrom = today.AddMonths(-6);
        db.MinimumWageRates.AddRange(
            new MinimumWageRate
            {
                Id = Guid.NewGuid(),
                AgeYears = 21,
                HourlyRate = 14.06m,
                Label = "21+",
                EffectiveFrom = existingFrom
            },
            new MinimumWageRate
            {
                Id = Guid.NewGuid(),
                AgeYears = 20,
                HourlyRate = 11.25m,
                Label = "20",
                EffectiveFrom = existingFrom
            });
        await db.SaveChangesAsync();

        var before = await db.MinimumWageRates.CountAsync();
        var controller = new WagesController(db, new SalaryService(db));
        var result = await controller.SemiAnnualUpdate(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SemiAnnualWageUpdateResultDto>(ok.Value);
        Assert.True(dto.RatesUpdated >= 2);
        Assert.Equal(before + dto.RatesUpdated, await db.MinimumWageRates.CountAsync());
        Assert.True(await db.MinimumWageRates.AnyAsync(r => r.EffectiveFrom == dto.EffectiveFrom));
        Assert.Equal(WagesController.ResolveSemiAnnualEffectiveFrom(today), dto.EffectiveFrom);
        Assert.Contains(await db.PlatformLogs.ToListAsync(), l => l.Category == "Wages");
    }

    [Theory]
    [InlineData(1, 1, 2026, "2026-01-01")]
    [InlineData(1, 7, 2026, "2026-07-01")]
    [InlineData(15, 3, 2026, "2026-07-01")]
    [InlineData(15, 8, 2026, "2027-01-01")]
    public void Semi_annual_effective_from_aligns_with_due_dates(int day, int month, int year, string expected)
    {
        var today = new DateOnly(year, month, day);
        Assert.Equal(DateOnly.Parse(expected), WagesController.ResolveSemiAnnualEffectiveFrom(today));
    }

    [Fact]
    public void SalaryService_uses_database_rates_over_hardcoded()
    {
        using var db = CreateDb();
        db.MinimumWageRates.Add(new MinimumWageRate
        {
            Id = Guid.NewGuid(),
            AgeYears = 21,
            HourlyRate = 99.99m,
            Label = "21+",
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)
        });
        db.SaveChanges();

        var salary = new SalaryService(db);
        Assert.Equal(99.99m, salary.GetMinimumHourlyWage(21));
        Assert.True(salary.MeetsMinimumWage(100m, 21));
        Assert.False(salary.MeetsMinimumWage(99m, 21));
    }

    [Fact]
    public async Task Wages_get_returns_only_current_effective_rates()
    {
        await using var db = CreateDb();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.MinimumWageRates.AddRange(
            new MinimumWageRate
            {
                Id = Guid.NewGuid(),
                AgeYears = 21,
                HourlyRate = 10m,
                Label = "old",
                EffectiveFrom = today.AddYears(-1)
            },
            new MinimumWageRate
            {
                Id = Guid.NewGuid(),
                AgeYears = 21,
                HourlyRate = 14.06m,
                Label = "current",
                EffectiveFrom = today.AddMonths(-1)
            },
            new MinimumWageRate
            {
                Id = Guid.NewGuid(),
                AgeYears = 21,
                HourlyRate = 20m,
                Label = "future",
                EffectiveFrom = today.AddMonths(6)
            });
        await db.SaveChangesAsync();

        var controller = new WagesController(db, new SalaryService(db));
        var result = await controller.GetAll(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rates = Assert.IsAssignableFrom<IEnumerable<MinimumWageRateDto>>(ok.Value).ToList();
        var age21 = Assert.Single(rates, r => r.AgeYears == 21);
        Assert.Equal(14.06m, age21.HourlyRate);
        Assert.Equal("current", age21.Label);
    }

    [Fact]
    public async Task Settings_upsert_early_adapter_and_update_token_pack()
    {
        await using var db = CreateDb();
        var packId = Guid.NewGuid();
        db.TokenPricings.Add(new TokenPricing
        {
            Id = packId,
            PackSize = 10,
            PriceEuro = 25m,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var controller = new SettingsController(
            db,
            new IntegrationCredentialService(db, new PassthroughSecretProtector()),
            new PlatformFeatureService(
                db,
                Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
                new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()),
            new PlatformCompanySettingsService(db),
            new AboutPageSettingsService(db));

        var create = await controller.UpsertEarlyAdapterRule(
            new UpsertEarlyAdapterRuleRequest(null, "Pilot", 5, 10m, true),
            CancellationToken.None);
        var createOk = Assert.IsType<OkObjectResult>(create.Result);
        Assert.NotNull(createOk.Value);
        Assert.Equal(1, await db.EarlyAdapterRules.CountAsync());
        var rule = await db.EarlyAdapterRules.SingleAsync();
        Assert.Equal("Pilot", rule.Name);
        Assert.Equal(5, rule.MonthlyGrantTokens);

        var updateRule = await controller.UpsertEarlyAdapterRule(
            new UpsertEarlyAdapterRuleRequest(rule.Id, "Pilot+", 8, 15m, false),
            CancellationToken.None);
        Assert.IsType<OkObjectResult>(updateRule.Result);
        await db.Entry(rule).ReloadAsync();
        Assert.Equal("Pilot+", rule.Name);
        Assert.Equal(8, rule.MonthlyGrantTokens);
        Assert.Equal(15m, rule.PurchaseDiscountPercent);
        Assert.False(rule.IsActive);

        var packResult = await controller.UpdatePack(
            packId,
            new UpdateTokenPackRequest(packId, 29.99m, false),
            CancellationToken.None);
        Assert.IsType<NoContentResult>(packResult);
        var pack = await db.TokenPricings.SingleAsync(p => p.Id == packId);
        Assert.Equal(29.99m, pack.PriceEuro);
        Assert.False(pack.IsActive);
    }

    [Fact]
    public void DutchToday_uses_amsterdam_calendar()
    {
        // 2026-06-30 22:30 UTC = 2026-07-01 00:30 CEST → July 1 local
        var utc = new DateTime(2026, 6, 30, 22, 30, 0, DateTimeKind.Utc);
        var dutch = MinimumWageUpdateHostedService.DutchToday(utc);
        Assert.Equal(new DateOnly(2026, 7, 1), dutch);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
