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
            // Raise legacy 0.5 highlight pricing into the 1–2 token product band.
            var highlightCost = await db.TokenSpendCosts
                .FirstOrDefaultAsync(c => c.Reason == TokenSpendReason.Highlight && c.IsActive);
            if (highlightCost is not null && highlightCost.CostTokens < 1m)
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

        await db.SaveChangesAsync();
        logger.LogInformation("Ensured platform token pricing / spend costs / PushBom tiers / early-adapter rules.");
    }
}
