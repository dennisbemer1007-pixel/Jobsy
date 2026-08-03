using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class SalesCommercialServiceTests
{
    [Fact]
    public async Task Public_catalog_exposes_active_type_costs_and_packages()
    {
        await using var db = CreateDb();
        SeedCommercial(db);
        var sut = new SalesCommercialService(db, new TokenLedgerService(db));

        var catalog = await sut.GetPublicCatalogAsync();

        Assert.Equal(25m, catalog.BaseTokenValueEuro);
        Assert.Equal(2m, catalog.HighlightCarouselTokens);
        Assert.Contains(catalog.VacancyTypeCosts, c => c.Kind == "Regular" && c.CostTokens == 1m);
        Assert.Contains(catalog.VacancyTypeCosts, c => c.Kind == "Volunteer" && c.CostTokens == 0m);
        Assert.Contains(catalog.Packages, p => p.Name == "Gold");
    }

    [Fact]
    public async Task Publish_cost_follows_vacancy_kind()
    {
        await using var db = CreateDb();
        SeedCommercial(db);
        var sut = new SalesCommercialService(db, new TokenLedgerService(db));

        Assert.Equal(1m, await sut.GetPublishCostTokensAsync(VacancyKind.Regular));
        Assert.Equal(0.5m, await sut.GetPublishCostTokensAsync(VacancyKind.Internship));
        Assert.Equal(0m, await sut.GetPublishCostTokensAsync(VacancyKind.Volunteer));
    }

    [Fact]
    public async Task Publish_with_pending_start_highlight_applies_free_highlight()
    {
        await using var db = CreateDb();
        SeedCommercial(db);
        SeedSpendCosts(db);

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Test BV",
            KvkNumber = "12345678",
            Address = "Teststraat 1",
            Location = new GeoPoint(52.0, 4.3),
            PendingStartHighlightBonus = true
        };
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            Title = "Medewerker",
            Description = "Test",
            HourlyWage = 14.5m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(2),
            Status = VacancyStatus.Draft,
            CompanyId = company.Id,
            Company = company,
            Location = company.Location,
            RequiredTransport = TransportMode.Bike,
            Kind = VacancyKind.Regular
        };
        db.Companies.Add(company);
        db.Vacancies.Add(vacancy);
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Amount = 5,
            Kind = TokenTransactionKind.Grant,
            OldBalance = 0,
            NewBalance = 5,
            CreatedAt = DateTime.UtcNow,
            Note = "seed"
        });
        await db.SaveChangesAsync();

        var features = new PlatformFeatureService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Core.Options.JobsyFeatureOptions()),
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var products = new VacancyProductService(
            db,
            new TokenLedgerService(db),
            new SalesCommercialService(db, new TokenLedgerService(db)),
            new PushNotificationServiceStub(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<PushNotificationServiceStub>.Instance),
            new EmailServiceStub(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<EmailServiceStub>.Instance),
            features,
            new MockRoutingService(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<VacancyProductService>.Instance);

        var result = await products.PublishAsync(
            vacancy,
            new VacancyPublishOptions(Highlight: false),
            actorUserId: null);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(VacancyStatus.Active, result.Vacancy!.Status);
        Assert.True(result.Vacancy.IsHighlighted);
        Assert.NotNull(result.Vacancy.HighlightedUntil);

        var refreshed = await db.Companies.AsNoTracking().SingleAsync(c => c.Id == company.Id);
        Assert.False(refreshed.PendingStartHighlightBonus);

        // Only publish cost charged (1 token); highlight was free.
        var balance = await new TokenLedgerService(db).GetBalanceAsync(company.Id);
        Assert.Equal(4m, balance);
    }

    [Fact]
    public async Task Flyer_pdf_renders_single_page_with_full_catalog()
    {
        await using var db = CreateDb();
        SeedCommercial(db);
        for (var i = 1; i <= 8; i++)
        {
            db.SalesPackages.Add(new SalesPackage
            {
                Id = Guid.NewGuid(),
                Name = $"Pakket {i}",
                Code = $"STD-P{i:00}",
                Category = SalesPackageCategory.Standard,
                TokenAmount = 10 * i,
                PriceEuro = 200m * i,
                IsActive = true,
                SortOrder = i
            });
        }

        await db.SaveChangesAsync();

        var companySettings = new PlatformCompanySettingsService(db);
        var flyer = new PartnerFlyerPdfService(
            new SalesCommercialService(db, new TokenLedgerService(db)),
            companySettings,
            new FlyerFakeFeatures());

        var bytes = await flyer.RenderAsync("SM-DEMO01");
        Assert.True(bytes.Length > 500);
        Assert.Equal(0x25, bytes[0]); // %
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
        Assert.Equal(1, PdfPageCounter.Count(bytes));

        var text = System.Text.Encoding.Latin1.GetString(bytes);
        Assert.DoesNotContain("lobsy.nl/register", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/register?ref=", text, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FlyerFakeFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(false, false, false, "https://lobsy.nl", null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    [Fact]
    public async Task Public_catalog_is_read_only_without_seed_writes()
    {
        await using var db = CreateDb();
        var sut = new SalesCommercialService(db, new TokenLedgerService(db));

        var catalog = await sut.GetPublicCatalogAsync();

        Assert.Equal(25m, catalog.BaseTokenValueEuro);
        Assert.Equal(0, await db.SalesCommercialSettings.CountAsync());
        Assert.Equal(0, await db.VacancyTypeTokenCosts.CountAsync());
        Assert.Contains(catalog.VacancyTypeCosts, c => c.Kind == "Volunteer");
    }

    [Fact]
    public async Task Concurrent_start_highlight_consume_is_one_shot()
    {
        await using var db = CreateDb();
        SeedCommercial(db);
        SeedSpendCosts(db);

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Bonus BV",
            KvkNumber = "12345678",
            Address = "Teststraat 1",
            Location = new GeoPoint(52.0, 4.3),
            PendingStartHighlightBonus = true
        };
        db.Companies.Add(company);
        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Amount = 10,
            Kind = TokenTransactionKind.Grant,
            OldBalance = 0,
            NewBalance = 10,
            CreatedAt = DateTime.UtcNow,
            Note = "seed"
        });

        Vacancy MakeDraft(string title) => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test",
            HourlyWage = 14.5m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(2),
            Status = VacancyStatus.Draft,
            CompanyId = company.Id,
            Company = company,
            Location = company.Location,
            RequiredTransport = TransportMode.Bike,
            Kind = VacancyKind.Regular
        };

        var a = MakeDraft("A");
        var b = MakeDraft("B");
        db.Vacancies.AddRange(a, b);
        await db.SaveChangesAsync();

        var features = new PlatformFeatureService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Core.Options.JobsyFeatureOptions()),
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        VacancyProductService Products() => new(
            db,
            new TokenLedgerService(db),
            new SalesCommercialService(db, new TokenLedgerService(db)),
            new PushNotificationServiceStub(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<PushNotificationServiceStub>.Instance),
            new EmailServiceStub(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<EmailServiceStub>.Instance),
            features,
            new MockRoutingService(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<VacancyProductService>.Instance);

        var first = await Products().PublishAsync(a, new VacancyPublishOptions(), null);
        Assert.True(first.Succeeded, first.ErrorMessage);
        Assert.True(first.Vacancy!.IsHighlighted);

        // Second publish must not get another free highlight (bonus already consumed).
        var second = await Products().PublishAsync(b, new VacancyPublishOptions(Highlight: true), null);
        Assert.True(second.Succeeded, second.ErrorMessage);
        Assert.True(second.Vacancy!.IsHighlighted);

        var balance = await new TokenLedgerService(db).GetBalanceAsync(company.Id);
        // First: publish 1 + free highlight. Second: publish 1 + paid highlight 2 → spent 4, balance 6.
        Assert.Equal(6m, balance);
        Assert.False((await db.Companies.AsNoTracking().SingleAsync(c => c.Id == company.Id)).PendingStartHighlightBonus);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static void SeedCommercial(JobsyDbContext db)
    {
        db.SalesCommercialSettings.Add(new SalesCommercialSettings
        {
            Id = SalesCommercialService.SingletonId,
            BaseTokenValueEuro = VacancyProductRules.DefaultBaseTokenValueEuro,
            HighlightCarouselTokens = VacancyProductRules.DefaultHighlightCarouselTokens,
            HighlightPulseTokens = VacancyProductRules.DefaultHighlightPulseTokens,
            HighlightCarouselDays = VacancyProductRules.DefaultHighlightCarouselDays,
            StartHighlightBonusTokens = VacancyProductRules.DefaultHighlightCarouselTokens,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.VacancyTypeTokenCosts.AddRange(
            new VacancyTypeTokenCost { Id = Guid.NewGuid(), Kind = VacancyKind.Regular, CostTokens = 1m },
            new VacancyTypeTokenCost { Id = Guid.NewGuid(), Kind = VacancyKind.Internship, CostTokens = 0.5m },
            new VacancyTypeTokenCost { Id = Guid.NewGuid(), Kind = VacancyKind.Volunteer, CostTokens = 0m });
        db.SalesPackages.Add(new SalesPackage
        {
            Id = Guid.NewGuid(),
            Name = "Gold",
            Code = "FYS-GOLD",
            Category = SalesPackageCategory.FirstYearSupplier,
            TokenAmount = 100,
            PriceEuro = 1800m,
            IsActive = true,
            SortOrder = 20
        });
        db.PlatformCompanySettings.Add(new PlatformCompanySettings
        {
            Id = PlatformCompanySettingsService.SingletonId,
            CompanyName = "Lobsy",
            Slogan = "Test",
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private static void SeedSpendCosts(JobsyDbContext db)
    {
        db.TokenSpendCosts.AddRange(
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Publish, CostTokens = 1m },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Highlight, CostTokens = 2m },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.PushBom, CostTokens = 3m },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Extend, CostTokens = 1m });
        db.SaveChanges();
    }
}
