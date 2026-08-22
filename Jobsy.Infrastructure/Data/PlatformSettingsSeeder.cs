using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Data;

internal static class PlatformSettingsSeeder
{
    public static async Task SeedPlatformSettingsAsync(JobsyDbContext db, ILogger logger)
    {
        if (!await db.TokenPricings.AnyAsync())
        {
            db.TokenPricings.AddRange(
                new TokenPricing { Id = Guid.NewGuid(), PackSize = 1, PriceEuro = 5.00m },
                new TokenPricing { Id = Guid.NewGuid(), PackSize = 5, PriceEuro = 22.50m },
                new TokenPricing { Id = Guid.NewGuid(), PackSize = 10, PriceEuro = 40.00m },
                new TokenPricing { Id = Guid.NewGuid(), PackSize = 50, PriceEuro = 175.00m },
                new TokenPricing { Id = Guid.NewGuid(), PackSize = 100, PriceEuro = 300.00m });
        }

        if (!await db.TokenSpendCosts.AnyAsync())
        {
            db.TokenSpendCosts.AddRange(
                new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Publish, CostTokens = 1m },
                new TokenSpendCost
                {
                    Id = Guid.NewGuid(),
                    Reason = TokenSpendReason.Highlight,
                    CostTokens = VacancyProductRules.DefaultHighlightCostTokens
                },
                new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.PushBom, CostTokens = 3m },
                new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Extend, CostTokens = 1m });
        }
        else
        {
            // Raise legacy highlight pricing into the current 2-token product price.
            var highlightCost = await db.TokenSpendCosts
                .FirstOrDefaultAsync(c => c.Reason == TokenSpendReason.Highlight && c.IsActive);
            if (highlightCost is not null && highlightCost.CostTokens < VacancyProductRules.DefaultHighlightCostTokens)
            {
                highlightCost.CostTokens = VacancyProductRules.DefaultHighlightCostTokens;
            }
        }

        if (!await db.EarlyAdapterRules.AnyAsync())
        {
            db.EarlyAdapterRules.Add(new EarlyAdapterRule
            {
                Id = Guid.NewGuid(),
                Name = "Launch early adapters",
                MonthlyGrantTokens = 10,
                PurchaseDiscountPercent = 20m,
                IsActive = true
            });
        }

        if (!await db.PushBomSettings.AnyAsync())
        {
            db.PushBomSettings.Add(new PushBomSettings
            {
                Id = Guid.NewGuid(),
                RadiusKm = VacancyProductRules.PushBomRadiusKm,
                MaxTravelMinutes = VacancyProductRules.PushBomMaxTravelMinutes,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        if (!await db.PushBomPricingTiers.AnyAsync())
        {
            db.PushBomPricingTiers.AddRange(
                new PushBomPricingTier
                {
                    Id = Guid.NewGuid(),
                    MinCandidates = 1,
                    MaxCandidates = 9,
                    CostTokens = 1m,
                    IsActive = true
                },
                new PushBomPricingTier
                {
                    Id = Guid.NewGuid(),
                    MinCandidates = 10,
                    MaxCandidates = 25,
                    CostTokens = 2m,
                    IsActive = true
                },
                new PushBomPricingTier
                {
                    Id = Guid.NewGuid(),
                    MinCandidates = 26,
                    MaxCandidates = 50,
                    CostTokens = 3m,
                    IsActive = true
                },
                new PushBomPricingTier
                {
                    Id = Guid.NewGuid(),
                    MinCandidates = 51,
                    MaxCandidates = null,
                    CostTokens = 4m,
                    IsActive = true
                });
        }

        if (!await db.PlatformCompanySettings.AnyAsync())
        {
            db.PlatformCompanySettings.Add(new PlatformCompanySettings
            {
                Id = PlatformCompanySettingsService.SingletonId,
                CompanyName = PlatformCompanySettingsService.DefaultCompanyName,
                Slogan = PlatformCompanySettingsService.DefaultSlogan,
                Country = "NL",
                UpdatedAtUtc = DateTime.UtcNow
            });
            logger.LogInformation("Seeded default PlatformCompanySettings (Lobsy).");
        }

        if (!await db.AboutPageSettings.AnyAsync())
        {
            db.AboutPageSettings.Add(new AboutPageSettings
            {
                Id = AboutPageSettingsService.SingletonId,
                Title = AboutPageSettingsService.DefaultTitle,
                Lead = AboutPageSettingsService.DefaultLead,
                BodyHtml = AboutPageSettingsService.DefaultBodyHtml,
                UpdatedAtUtc = DateTime.UtcNow
            });
            logger.LogInformation("Seeded default AboutPageSettings (Wie zijn wij).");
        }

        if (!await db.MarketingFlyerSettings.AnyAsync())
        {
            var defaults = MarketingFlyerSettingsService.DefaultsUpdate();
            db.MarketingFlyerSettings.Add(new MarketingFlyerSettings
            {
                Id = MarketingFlyerSettingsService.SingletonId,
                Headline = defaults.Headline,
                Subheadline = defaults.Subheadline,
                Intro = defaults.Intro,
                BulletPoints = defaults.BulletPoints,
                PromoFreeText = defaults.PromoFreeText,
                PromoDiscountText = defaults.PromoDiscountText,
                CtaTitle = defaults.CtaTitle,
                CtaBody = defaults.CtaBody,
                QrCaption = defaults.QrCaption,
                QrPath = defaults.QrPath,
                FooterNote = defaults.FooterNote,
                UpdatedAtUtc = DateTime.UtcNow
            });
            logger.LogInformation("Seeded default MarketingFlyerSettings (werkgeversflyer).");
        }

        if (!await db.SalesCommercialSettings.AnyAsync())
        {
            db.SalesCommercialSettings.Add(new SalesCommercialSettings
            {
                Id = SalesCommercialService.SingletonId,
                BaseTokenValueEuro = VacancyProductRules.DefaultBaseTokenValueEuro,
                HighlightCarouselTokens = VacancyProductRules.DefaultHighlightCarouselTokens,
                HighlightPulseTokens = VacancyProductRules.DefaultHighlightPulseTokens,
                HighlightCarouselDays = VacancyProductRules.DefaultHighlightCarouselDays,
                StartHighlightBonusTokens = VacancyProductRules.DefaultHighlightCarouselTokens,
                DirectCommissionRate = SalesCommissionRules.DefaultDirectCommissionRate,
                IndirectCommissionRate = SalesCommissionRules.DefaultIndirectCommissionRate,
                CommissionDurationDays = SalesCommissionRules.DefaultCommissionDurationDays,
                PartnerCommissionRate = SalesCommissionRules.DefaultPartnerCommissionRate,
                UpdatedAtUtc = DateTime.UtcNow
            });
            logger.LogInformation("Seeded default SalesCommercialSettings (token €25 / highlight / SM commissions).");
        }
        else
        {
            // Backfill commission defaults on existing singleton rows (pre-referral migration).
            var existing = await db.SalesCommercialSettings.OrderBy(s => s.Id).FirstAsync();
            var touched = false;
            if (existing.DirectCommissionRate <= 0)
            {
                existing.DirectCommissionRate = SalesCommissionRules.DefaultDirectCommissionRate;
                touched = true;
            }

            if (existing.IndirectCommissionRate < 0)
            {
                existing.IndirectCommissionRate = SalesCommissionRules.DefaultIndirectCommissionRate;
                touched = true;
            }

            if (existing.CommissionDurationDays <= 0)
            {
                existing.CommissionDurationDays = SalesCommissionRules.DefaultCommissionDurationDays;
                touched = true;
            }

            if (existing.PartnerCommissionRate <= 0)
            {
                existing.PartnerCommissionRate = SalesCommissionRules.DefaultPartnerCommissionRate;
                touched = true;
            }

            if (touched)
            {
                existing.UpdatedAtUtc = DateTime.UtcNow;
                logger.LogInformation("Backfilled SalesCommercialSettings commission defaults.");
            }
        }

        if (!await db.VacancyTypeTokenCosts.AnyAsync())
        {
            db.VacancyTypeTokenCosts.AddRange(
                new VacancyTypeTokenCost { Id = Guid.NewGuid(), Kind = VacancyKind.Regular, CostTokens = 1m },
                new VacancyTypeTokenCost { Id = Guid.NewGuid(), Kind = VacancyKind.Internship, CostTokens = 0m },
                new VacancyTypeTokenCost { Id = Guid.NewGuid(), Kind = VacancyKind.Volunteer, CostTokens = 0m });
        }

        if (!await db.SalesPackages.AnyAsync())
        {
            db.SalesPackages.AddRange(
                new SalesPackage
                {
                    Id = Guid.NewGuid(),
                    Name = "Starter",
                    Code = "STD-STARTER",
                    Category = SalesPackageCategory.Standard,
                    TokenAmount = 10,
                    PriceEuro = 200m,
                    Description = "Instappakket voor lokale werkgevers",
                    SortOrder = 10
                },
                new SalesPackage
                {
                    Id = Guid.NewGuid(),
                    Name = "Groei",
                    Code = "STD-GROEI",
                    Category = SalesPackageCategory.Standard,
                    TokenAmount = 50,
                    PriceEuro = 875m,
                    Description = "Bulkkorting t.o.v. losse tokens",
                    SortOrder = 20
                },
                new SalesPackage
                {
                    Id = Guid.NewGuid(),
                    Name = "Silver",
                    Code = "FYS-SILVER",
                    Category = SalesPackageCategory.FirstYearSupplier,
                    TokenAmount = 40,
                    PriceEuro = 800m,
                    Description = "First Year Supplier — Silver",
                    SortOrder = 10
                },
                new SalesPackage
                {
                    Id = Guid.NewGuid(),
                    Name = "Gold",
                    Code = "FYS-GOLD",
                    Category = SalesPackageCategory.FirstYearSupplier,
                    TokenAmount = 100,
                    PriceEuro = 1800m,
                    Description = "First Year Supplier — Gold",
                    SortOrder = 20
                },
                new SalesPackage
                {
                    Id = Guid.NewGuid(),
                    Name = "Platinum",
                    Code = "FYS-PLATINUM",
                    Category = SalesPackageCategory.FirstYearSupplier,
                    TokenAmount = 250,
                    PriceEuro = 4000m,
                    Description = "First Year Supplier — Platinum",
                    SortOrder = 30
                },
                new SalesPackage
                {
                    Id = Guid.NewGuid(),
                    Name = "Enterprise Silver",
                    Code = "ENT-SILVER",
                    Category = SalesPackageCategory.Enterprise,
                    TokenAmount = 200,
                    PriceEuro = 3500m,
                    Description = "Enterprise — Silver",
                    SortOrder = 10
                },
                new SalesPackage
                {
                    Id = Guid.NewGuid(),
                    Name = "Enterprise Gold",
                    Code = "ENT-GOLD",
                    Category = SalesPackageCategory.Enterprise,
                    TokenAmount = 500,
                    PriceEuro = 8000m,
                    Description = "Enterprise — Gold",
                    SortOrder = 20
                },
                new SalesPackage
                {
                    Id = Guid.NewGuid(),
                    Name = "Enterprise Platinum",
                    Code = "ENT-PLATINUM",
                    Category = SalesPackageCategory.Enterprise,
                    TokenAmount = 1000,
                    PriceEuro = 14000m,
                    Description = "Enterprise — Platinum",
                    SortOrder = 30
                });
            logger.LogInformation("Seeded standard + First Year / Enterprise sales packages.");
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Ensured platform token pricing / spend costs / PushBom tiers / early-adapter rules.");
    }
}
