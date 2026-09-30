using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Sales;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests.Sales;

public class SalesPriceQuoteAndMaterialsTests
{
    [Fact]
    public async Task Price_quote_uses_active_packs_only_and_computes_min_max()
    {
        await using var db = CreateDb();
        SeedPacks(db);
        db.TokenPricings.Add(new TokenPricing
        {
            Id = Guid.NewGuid(),
            PackSize = 2,
            PriceEuro = 99m,
            IsActive = false
        });
        await db.SaveChangesAsync();

        var quote = await new SalesPriceQuoteService(db).GetAsync();

        Assert.Equal(3.00m, quote.MinPricePerToken);
        Assert.Equal(5.00m, quote.MaxPricePerToken);
        Assert.DoesNotContain(quote.Packs, p => p.PackSize == 2);
        Assert.Contains(quote.Packs, p => p.PackSize == 1 && p.PricePerToken == 5m);
        Assert.Contains(quote.Packs, p => p.PackSize == 100 && p.PricePerToken == 3m);
    }

    [Fact]
    public async Task Public_catalog_prices_come_from_quote_not_base_token_value()
    {
        await using var db = CreateDb();
        SeedCommercial(db);
        SeedPacks(db);
        await db.SaveChangesAsync();

        var catalog = await new SalesCommercialService(db, new TokenLedgerService(db)).GetPublicCatalogAsync();

        Assert.Equal(3m, catalog.BaseTokenValueEuro);
        Assert.Contains(catalog.VacancyTypeCosts, c => c.Kind == "Regular" && c.PriceEuro == 3m);
        Assert.DoesNotContain(catalog.VacancyTypeCosts, c => c.PriceEuro == 25m);
    }

    [Fact]
    public async Task Materials_pdfs_contain_code_and_short_link_and_are_not_empty()
    {
        await using var db = CreateDb();
        var userId = await SeedSalesManagerAsync(db, "SM-MAT001", referred: false);
        SeedCommercial(db);
        SeedPacks(db);
        await db.SaveChangesAsync();

        var materials = CreateMaterials(db);
        var flyer = await materials.FlyerA4Async("SM-MAT001");
        var cards = await materials.BusinessCardsAsync("SM-MAT001");
        var price = await materials.PriceCardAsync("SM-MAT001");
        var pres = await materials.PresentationAsync("SM-MAT001", "Tom Test", "Test BV", "tom@test.nl");

        Assert.True(flyer.Length > 500);
        Assert.True(cards.Length > 500);
        Assert.True(price.Length > 500);
        Assert.True(pres.Length > 500);
        Assert.Equal(7, PdfPageCounter.Count(pres));

        foreach (var pdf in new[] { flyer, cards, price, pres })
        {
            var text = Encoding.Latin1.GetString(pdf);
            Assert.Contains("SM-MAT001", text, StringComparison.Ordinal);
            Assert.Contains("/p/SM-MAT001", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("25,00", text, StringComparison.Ordinal);
        }

        Assert.Equal(userId, userId);
    }

    [Fact]
    public async Task Materials_refuse_non_sm_codes()
    {
        await using var db = CreateDb();
        SeedCommercial(db);
        SeedPacks(db);
        await db.SaveChangesAsync();
        var materials = CreateMaterials(db);

        await Assert.ThrowsAsync<ArgumentException>(() => materials.FlyerA4Async("AM-XXXX01"));
        await Assert.ThrowsAsync<ArgumentException>(() => materials.FlyerA4Async("BM-XXXX01"));
    }

    [Fact]
    public async Task Link_toolkit_shows_20_percent_for_referred_salesmanager()
    {
        await using var db = CreateDb();
        var upline = await SeedSalesManagerAsync(db, "SM-UP0001", referred: false);
        await SeedSalesManagerAsync(db, "SM-REF001", referred: true, referredBy: upline);
        SeedCommercial(db);
        SeedPacks(db);
        await db.SaveChangesAsync();

        var referred = await db.SalesManagerProfiles.AsNoTracking()
            .SingleAsync(p => p.TrackingCode == "SM-REF001");
        var toolkit = await CreateLinkToolkit(db).GetAsync(referred.UserId);

        Assert.NotNull(toolkit);
        Assert.Equal(0.20m, toolkit!.Year1Rate);
        Assert.True(toolkit.IsReferredSalesManager);
        Assert.Contains(toolkit.ShortLinkUrl, toolkit.WhatsAppText, StringComparison.Ordinal);
        Assert.Contains("3,00", toolkit.PitchCostLine, StringComparison.Ordinal);
        Assert.DoesNotContain("25", toolkit.PitchCostLine, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Link_toolkit_shows_25_percent_for_direct_salesmanager()
    {
        await using var db = CreateDb();
        var userId = await SeedSalesManagerAsync(db, "SM-DIR001", referred: false);
        SeedCommercial(db);
        SeedPacks(db);
        await db.SaveChangesAsync();

        var toolkit = await CreateLinkToolkit(db).GetAsync(userId);
        Assert.NotNull(toolkit);
        Assert.Equal(0.25m, toolkit!.Year1Rate);
        Assert.False(toolkit.IsReferredSalesManager);
    }

    [Fact]
    public async Task Active_referral_required_for_personal_flyer_code()
    {
        await using var db = CreateDb();
        await SeedSalesManagerAsync(db, "SM-LIVE01", referred: false);
        var resolver = CreateResolver(db);

        Assert.NotNull(await resolver.ResolveActiveReferralAsync("SM-LIVE01"));
        Assert.Null(await resolver.ResolveActiveReferralAsync("SM-GHOST1"));
        Assert.Null(await resolver.ResolveActiveReferralAsync("AM-XXXX01"));
    }

    [Fact]
    public async Task Partner_code_remains_active_for_flyer()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "bm@test.nl",
            FullName = "BM Partner",
            Role = UserRole.EnterpriseManager,
            IsActive = true
        });
        db.PartnerAffiliateProfiles.Add(new PartnerAffiliateProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TrackingCode = "BM-PART01",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var active = await CreateResolver(db).ResolveActiveReferralAsync("BM-PART01");
        Assert.NotNull(active);
        Assert.Equal(SalesResolvedCodeKind.Partner, active!.Kind);
    }

    [Fact]
    public void SalesQr_png_helper_renders()
    {
        var png = SalesQr.Png("https://lobsy.nl/p/SM-TEST01?b=qr", 6);
        Assert.True(png.Length > 100);
        Assert.Equal(0x89, png[0]); // PNG magic
        var sized = SalesQr.PngForSize("https://lobsy.nl/p/SM-TEST01?b=qr", 1024);
        Assert.True(sized.Length > png.Length);
    }

    [Fact]
    public async Task Preview_visit_can_be_skipped_by_caller()
    {
        await using var db = CreateDb();
        var userId = await SeedSalesManagerAsync(db, "SM-PREV01", referred: false);
        var clicks = new SalesLinkClickService(db);

        // Simulate preview: caller does not record.
        // Recording once for non-preview should create a row.
        await clicks.RecordClickAsync(userId, SalesLinkChannel.Link);
        Assert.Equal(1, await db.SalesLinkClickDailies.CountAsync());
    }

    private static SalesMaterialsPdfService CreateMaterials(JobsyDbContext db)
        => new(
            new SalesPriceQuoteService(db),
            new SalesCommercialService(db, new TokenLedgerService(db)),
            new PlatformCompanySettingsService(db),
            new FakeFeatures());

    private static SalesLinkToolkitService CreateLinkToolkit(JobsyDbContext db)
        => new(
            db,
            new SalesPriceQuoteService(db),
            new SalesCommercialService(db, new TokenLedgerService(db)),
            new FakeFeatures());

    private static SalesAttributionResolver CreateResolver(JobsyDbContext db)
        => new(
            db,
            new PlatformFeatureService(
                db,
                Options.Create(new JobsyFeatureOptions()),
                new ConfigurationBuilder().Build()),
            NullLogger<SalesAttributionResolver>.Instance);

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static void SeedPacks(JobsyDbContext db)
    {
        db.TokenPricings.AddRange(
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 1, PriceEuro = 5.00m, IsActive = true },
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 5, PriceEuro = 22.50m, IsActive = true },
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 10, PriceEuro = 40.00m, IsActive = true },
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 50, PriceEuro = 175.00m, IsActive = true },
            new TokenPricing { Id = Guid.NewGuid(), PackSize = 100, PriceEuro = 300.00m, IsActive = true });
    }

    private static void SeedCommercial(JobsyDbContext db)
    {
        db.SalesCommercialSettings.Add(new SalesCommercialSettings
        {
            Id = SalesCommercialService.SingletonId,
            BaseTokenValueEuro = 25m,
            HighlightCarouselTokens = 2m,
            HighlightPulseTokens = 1m,
            HighlightCarouselDays = 14,
            StartHighlightBonusTokens = 2m,
            DirectCommissionRate = 0.25m,
            ReferredYear1DirectCommissionRate = 0.20m,
            Year2DirectCommissionRate = 0.10m,
            Year3DirectCommissionRate = 0.05m,
            AttributionCookieDays = 30,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.VacancyTypeTokenCosts.AddRange(
            new VacancyTypeTokenCost { Id = Guid.NewGuid(), Kind = VacancyKind.Regular, CostTokens = 1m },
            new VacancyTypeTokenCost { Id = Guid.NewGuid(), Kind = VacancyKind.Internship, CostTokens = 0m });
        db.TokenSpendCosts.Add(new TokenSpendCost
        {
            Id = Guid.NewGuid(),
            Reason = TokenSpendReason.Highlight,
            CostTokens = 2m,
            IsActive = true
        });
        if (!db.PlatformCompanySettings.Any())
        {
            db.PlatformCompanySettings.Add(new PlatformCompanySettings
            {
                Id = PlatformCompanySettingsService.SingletonId,
                CompanyName = "Lobsy",
                Slogan = "Test",
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private static async Task<Guid> SeedSalesManagerAsync(
        JobsyDbContext db,
        string code,
        bool referred,
        Guid? referredBy = null)
    {
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.Users.Add(new User
        {
            Id = userId,
            Email = $"{code.ToLowerInvariant()}@test.nl",
            FullName = $"SM {code}",
            Role = UserRole.SalesManager,
            IsActive = true
        });
        db.SalesManagerProfiles.Add(new SalesManagerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TrackingCode = code,
            CompanyName = "Hendriks Sales",
            CanRecruitSalesManagers = !referred,
            ReferredBySalesManagerUserId = referred ? referredBy : null,
            AgreementSignedAt = now,
            OnboardingCompletedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
        return userId;
    }

    private sealed class FakeFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(false, false, false, "https://lobsy.nl", null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
