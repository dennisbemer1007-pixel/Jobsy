using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Infrastructure.Services;

public sealed class FlexCommercialService : IFlexCommercialService
{
    public static readonly Guid SettingsSingletonId = Guid.Parse("f1e20001-0000-4000-8000-000000000001");
    public static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private const string CacheKey = "flex-commercial-settings";

    private readonly JobsyDbContext _db;
    private readonly IMemoryCache _cache;

    public FlexCommercialService(JobsyDbContext db)
        : this(db, new MemoryCache(new MemoryCacheOptions()))
    {
    }

    public FlexCommercialService(JobsyDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<FlexCommercialSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out FlexCommercialSettingsDto? cached) && cached is not null)
        {
            return cached;
        }

        var settings = await EnsureSettingsAsync(cancellationToken);
        var dto = MapSettings(settings);
        _cache.Set(CacheKey, dto, CacheTtl);
        return dto;
    }

    public async Task<FlexCommercialSettingsDto> UpdateAsync(
        FlexCommercialSettingsUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (update.MarginPerHourEuro is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(update), "Flex-marge moet tussen € 0 en € 100 liggen.");
        }

        ValidateDeepPrice(update.DeepTestPriceCompetenceEuro, nameof(update.DeepTestPriceCompetenceEuro));
        ValidateDeepPrice(update.DeepTestPriceCareerEuro, nameof(update.DeepTestPriceCareerEuro));
        ValidateDeepPrice(update.DeepTestPriceValuesEuro, nameof(update.DeepTestPriceValuesEuro));
        ValidateDeepPrice(update.DeepTestPriceCultureEuro, nameof(update.DeepTestPriceCultureEuro));

        if (update.AgencyAnnualPriceEuro is < 0 or > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(update), "Uitzend-jaarabonnement moet tussen € 0 en € 1.000.000 liggen.");
        }

        if (update.ContactUnlockCostTokens is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(update), "ContactUnlock moet tussen 0,1 en 100 tokens liggen.");
        }

        if (update.AcceptCandidatePilotCostTokens is <= 0 or > 100
            || update.AcceptCandidateStandardCostTokens is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(update), "Accept-kosten moeten tussen 0,1 en 100 tokens liggen.");
        }

        if (string.IsNullOrWhiteSpace(update.BackofficePartnerName))
        {
            throw new ArgumentException("Backoffice-partner is verplicht.", nameof(update));
        }

        var settings = await EnsureSettingsAsync(cancellationToken);
        settings.MarginPerHourEuro = Math.Round(update.MarginPerHourEuro, 2, MidpointRounding.AwayFromZero);
        settings.BackofficePartnerName = update.BackofficePartnerName.Trim();
        settings.DeepTestPriceCompetenceEuro = RoundPrice(update.DeepTestPriceCompetenceEuro);
        settings.DeepTestPriceCareerEuro = RoundPrice(update.DeepTestPriceCareerEuro);
        settings.DeepTestPriceValuesEuro = RoundPrice(update.DeepTestPriceValuesEuro);
        settings.DeepTestPriceCultureEuro = RoundPrice(update.DeepTestPriceCultureEuro);
        settings.AgencyAnnualPriceEuro = Math.Round(update.AgencyAnnualPriceEuro, 2, MidpointRounding.AwayFromZero);
        settings.ContactUnlockCostTokens = Math.Round(update.ContactUnlockCostTokens, 2, MidpointRounding.AwayFromZero);
        settings.AcceptCandidatePilotCostTokens =
            Math.Round(update.AcceptCandidatePilotCostTokens, 2, MidpointRounding.AwayFromZero);
        settings.AcceptCandidatePilotEndsOn = update.AcceptCandidatePilotEndsOn;
        settings.AcceptCandidateStandardCostTokens =
            Math.Round(update.AcceptCandidateStandardCostTokens, 2, MidpointRounding.AwayFromZero);
        settings.FirstEmployerAcceptanceFreeEnabled = update.FirstEmployerAcceptanceFreeEnabled;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        await SyncContactUnlockSpendCostAsync(settings.ContactUnlockCostTokens, cancellationToken);
        await SyncAcceptCandidateSpendCostAsync(settings, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
        var dto = MapSettings(settings);
        _cache.Set(CacheKey, dto, CacheTtl);
        return dto;
    }

    public async Task<bool> HasActiveAgencySubscriptionAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _db.AgencyAnnualSubscriptions.AsNoTracking()
            .AnyAsync(
                s => s.CompanyId == companyId
                     && s.IsActive
                     && s.StartsAtUtc <= now
                     && s.EndsAtUtc > now,
                cancellationToken);
    }

    public async Task<AgencySubscriptionDto?> GetAgencySubscriptionAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var row = await _db.AgencyAnnualSubscriptions.AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.IsActive && s.EndsAtUtc > now)
            .OrderByDescending(s => s.EndsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : MapSubscription(row);
    }

    public async Task<AgencySubscriptionDto> ActivateAgencySubscriptionAsync(
        Guid companyId,
        DateTime? startsAtUtc = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        _ = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Bedrijf niet gevonden.");

        var commercial = await EnsureSettingsAsync(cancellationToken);
        var start = startsAtUtc ?? DateTime.UtcNow;
        var row = new AgencyAnnualSubscription
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            StartsAtUtc = start,
            EndsAtUtc = start.AddYears(1),
            IsActive = true,
            PriceEuro = commercial.AgencyAnnualPriceEuro,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.AgencyAnnualSubscriptions.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return MapSubscription(row);
    }

    private static void ValidateDeepPrice(decimal price, string paramName)
    {
        if (price is <= 0 or > 100)
        {
            throw new ArgumentException("invalid_price", paramName);
        }
    }

    private static decimal RoundPrice(decimal price)
        => Math.Round(price, 2, MidpointRounding.AwayFromZero);

    private async Task SyncContactUnlockSpendCostAsync(decimal costTokens, CancellationToken cancellationToken)
    {
        var row = await _db.TokenSpendCosts
            .FirstOrDefaultAsync(c => c.Reason == TokenSpendReason.ContactUnlock, cancellationToken);
        if (row is null)
        {
            _db.TokenSpendCosts.Add(new TokenSpendCost
            {
                Id = Guid.NewGuid(),
                Reason = TokenSpendReason.ContactUnlock,
                CostTokens = costTokens,
                IsActive = true
            });
            return;
        }

        row.CostTokens = costTokens;
        row.IsActive = true;
    }

    private async Task SyncAcceptCandidateSpendCostAsync(
        FlexCommercialSettings settings,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cost = Jobsy.Core.Rules.AcceptCandidatePricingRules.ResolveCostTokens(settings, today);
        var row = await _db.TokenSpendCosts
            .FirstOrDefaultAsync(c => c.Reason == TokenSpendReason.AcceptCandidate, cancellationToken);
        if (row is null)
        {
            _db.TokenSpendCosts.Add(new TokenSpendCost
            {
                Id = Guid.NewGuid(),
                Reason = TokenSpendReason.AcceptCandidate,
                CostTokens = cost,
                IsActive = true
            });
            return;
        }

        row.CostTokens = cost;
        row.IsActive = true;
    }

    private async Task<FlexCommercialSettings> EnsureSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.FlexCommercialSettings
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            EnsurePerKindDefaults(settings);
            return settings;
        }

        settings = new FlexCommercialSettings
        {
            Id = SettingsSingletonId,
            MarginPerHourEuro = FlexCommercialSettings.DefaultMarginPerHourEuro,
            BackofficePartnerName = FlexCommercialSettings.DefaultBackofficePartnerName,
            DeepTestPriceCompetenceEuro = FlexCommercialSettings.DefaultDeepAnalysisPriceEuro,
            DeepTestPriceCareerEuro = FlexCommercialSettings.DefaultDeepAnalysisPriceEuro,
            DeepTestPriceValuesEuro = FlexCommercialSettings.DefaultDeepAnalysisPriceEuro,
            DeepTestPriceCultureEuro = FlexCommercialSettings.DefaultDeepAnalysisPriceEuro,
            AgencyAnnualPriceEuro = FlexCommercialSettings.DefaultAgencyAnnualPriceEuro,
            ContactUnlockCostTokens = FlexCommercialSettings.DefaultContactUnlockCostTokens,
            AcceptCandidatePilotCostTokens = FlexCommercialSettings.DefaultAcceptCandidatePilotCostTokens,
            AcceptCandidateStandardCostTokens = FlexCommercialSettings.DefaultAcceptCandidateStandardCostTokens,
            FirstEmployerAcceptanceFreeEnabled = true,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _db.FlexCommercialSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        await SyncAcceptCandidateSpendCostAsync(settings, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static void EnsurePerKindDefaults(FlexCommercialSettings settings)
    {
        var fallback = FlexCommercialSettings.DefaultDeepAnalysisPriceEuro;
        if (settings.DeepTestPriceCompetenceEuro <= 0)
        {
            settings.DeepTestPriceCompetenceEuro = fallback;
        }

        if (settings.DeepTestPriceCareerEuro <= 0)
        {
            settings.DeepTestPriceCareerEuro = fallback;
        }

        if (settings.DeepTestPriceValuesEuro <= 0)
        {
            settings.DeepTestPriceValuesEuro = fallback;
        }

        if (settings.DeepTestPriceCultureEuro <= 0)
        {
            settings.DeepTestPriceCultureEuro = fallback;
        }
    }

    private static FlexCommercialSettingsDto MapSettings(FlexCommercialSettings settings)
    {
        EnsurePerKindDefaults(settings);
        return new(
            settings.MarginPerHourEuro,
            settings.BackofficePartnerName,
            settings.DeepTestPriceCompetenceEuro,
            settings.DeepTestPriceCareerEuro,
            settings.DeepTestPriceValuesEuro,
            settings.DeepTestPriceCultureEuro,
            settings.AgencyAnnualPriceEuro,
            settings.ContactUnlockCostTokens,
            settings.AcceptCandidatePilotCostTokens,
            settings.AcceptCandidatePilotEndsOn,
            settings.AcceptCandidateStandardCostTokens,
            settings.FirstEmployerAcceptanceFreeEnabled,
            settings.UpdatedAtUtc);
    }

    private static AgencySubscriptionDto MapSubscription(AgencyAnnualSubscription row) => new(
        row.Id,
        row.CompanyId,
        row.StartsAtUtc,
        row.EndsAtUtc,
        row.IsActive,
        row.PriceEuro,
        row.Note);
}
