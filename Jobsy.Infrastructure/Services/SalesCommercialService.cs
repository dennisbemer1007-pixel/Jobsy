using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class SalesCommercialService : ISalesCommercialService
{
    public static readonly Guid SingletonId = Guid.Parse("a8c3e6f1-4b2d-4e9a-9c1f-7d5e2b8a0f31");

    private readonly JobsyDbContext _db;
    private readonly ITokenLedgerService _tokens;

    public SalesCommercialService(JobsyDbContext db, ITokenLedgerService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<SalesCommercialSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _db.SalesCommercialSettings
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = CreateDefaultSettings();
        _db.SalesCommercialSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task<PartnerSalesCatalogDto> GetPublicCatalogAsync(CancellationToken cancellationToken = default)
    {
        // Read-only: never insert defaults from anonymous traffic (seed/admin paths own writes).
        var settings = await _db.SalesCommercialSettings.AsNoTracking()
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken) ?? CreateDefaultSettings();

        var typeCosts = await _db.VacancyTypeTokenCosts.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Kind)
            .ToListAsync(cancellationToken);

        if (typeCosts.Count == 0)
        {
            typeCosts = DefaultTypeCosts
                .Select(d => new VacancyTypeTokenCost
                {
                    Id = Guid.Empty,
                    Kind = d.Kind,
                    CostTokens = d.Cost,
                    IsActive = true
                })
                .ToList();
        }

        var packages = await _db.SalesPackages.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return new PartnerSalesCatalogDto(
            settings.BaseTokenValueEuro,
            settings.HighlightCarouselTokens,
            settings.HighlightPulseTokens,
            settings.HighlightCarouselDays,
            settings.StartHighlightBonusTokens,
            typeCosts.Select(c => new VacancyTypeCostDto(
                c.Kind.ToString(),
                VacancyKindLabels.ToDutch(c.Kind),
                c.CostTokens,
                Math.Round(c.CostTokens * settings.BaseTokenValueEuro, 2),
                c.IsActive)).ToList(),
            packages.Select(MapPackage).ToList());
    }

    public async Task<SalesCommercialAdminDto> GetAdminAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        await EnsureVacancyTypeCostsAsync(cancellationToken);

        var typeCosts = await _db.VacancyTypeTokenCosts.AsNoTracking()
            .OrderBy(c => c.Kind)
            .ToListAsync(cancellationToken);

        var packages = await _db.SalesPackages.AsNoTracking()
            .OrderBy(p => p.Category)
            .ThenBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return new SalesCommercialAdminDto(
            settings.Id,
            settings.BaseTokenValueEuro,
            settings.HighlightCarouselTokens,
            settings.HighlightPulseTokens,
            settings.HighlightCarouselDays,
            settings.StartHighlightBonusTokens,
            settings.UpdatedAtUtc,
            typeCosts.Select(c => new VacancyTypeCostDto(
                c.Kind.ToString(),
                VacancyKindLabels.ToDutch(c.Kind),
                c.CostTokens,
                Math.Round(c.CostTokens * settings.BaseTokenValueEuro, 2),
                c.IsActive)).ToList(),
            packages.Select(MapPackage).ToList());
    }

    public async Task<SalesCommercialSettings> UpdateSettingsAsync(
        decimal baseTokenValueEuro,
        decimal highlightCarouselTokens,
        decimal highlightPulseTokens,
        int highlightCarouselDays,
        decimal startHighlightBonusTokens,
        CancellationToken cancellationToken = default)
    {
        if (baseTokenValueEuro < 0
            || highlightCarouselTokens < 0
            || highlightPulseTokens < 0
            || startHighlightBonusTokens < 0)
        {
            throw new ArgumentException("Tokenwaarden mogen niet negatief zijn.");
        }

        if (highlightCarouselDays is < 1 or > 90)
        {
            throw new ArgumentException("Highlight-duur moet tussen 1 en 90 dagen liggen.");
        }

        var settings = await GetSettingsAsync(cancellationToken);
        settings.BaseTokenValueEuro = baseTokenValueEuro;
        settings.HighlightCarouselTokens = highlightCarouselTokens;
        settings.HighlightPulseTokens = highlightPulseTokens;
        settings.HighlightCarouselDays = highlightCarouselDays;
        settings.StartHighlightBonusTokens = startHighlightBonusTokens;
        settings.UpdatedAtUtc = DateTime.UtcNow;

        // Keep legacy TokenSpendCost.Highlight in sync for admin settings screens.
        var highlightCost = await _db.TokenSpendCosts
            .FirstOrDefaultAsync(c => c.Reason == TokenSpendReason.Highlight, cancellationToken);
        if (highlightCost is not null)
        {
            highlightCost.CostTokens = highlightCarouselTokens;
            highlightCost.IsActive = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task<VacancyTypeTokenCost> UpdateVacancyTypeCostAsync(
        VacancyKind kind,
        decimal costTokens,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (costTokens < 0)
        {
            throw new ArgumentException("Kosten mogen niet negatief zijn.");
        }

        await EnsureVacancyTypeCostsAsync(cancellationToken);
        var row = await _db.VacancyTypeTokenCosts
            .FirstOrDefaultAsync(c => c.Kind == kind, cancellationToken)
            ?? throw new KeyNotFoundException($"Geen tarief voor vacaturetype {kind}.");

        row.CostTokens = costTokens;
        row.IsActive = isActive;

        if (kind == VacancyKind.Regular)
        {
            var publish = await _db.TokenSpendCosts
                .FirstOrDefaultAsync(c => c.Reason == TokenSpendReason.Publish, cancellationToken);
            if (publish is not null)
            {
                publish.CostTokens = costTokens;
                publish.IsActive = isActive;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task<decimal> GetPublishCostTokensAsync(VacancyKind kind, CancellationToken cancellationToken = default)
    {
        await EnsureVacancyTypeCostsAsync(cancellationToken);
        // Pricing always follows the configured type row (IsActive only hides it from the public catalog).
        var row = await _db.VacancyTypeTokenCosts.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Kind == kind, cancellationToken);
        if (row is not null)
        {
            return row.CostTokens;
        }

        return await _tokens.GetCostAsync(TokenSpendReason.Publish, cancellationToken) ?? 1m;
    }

    public async Task<decimal> GetHighlightCostTokensAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return settings.HighlightCarouselTokens;
    }

    public async Task<int> GetHighlightDaysAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return settings.HighlightCarouselDays > 0
            ? settings.HighlightCarouselDays
            : VacancyProductRules.DefaultHighlightCarouselDays;
    }

    public async Task<SalesPackage> UpsertPackageAsync(SalesPackage package, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(package.Name))
        {
            throw new ArgumentException("Pakketnaam is verplicht.");
        }

        if (package.TokenAmount < 0 || package.PriceEuro < 0)
        {
            throw new ArgumentException("Tokens en prijs mogen niet negatief zijn.");
        }

        SalesPackage entity;
        if (package.Id == Guid.Empty)
        {
            entity = new SalesPackage { Id = Guid.NewGuid() };
            _db.SalesPackages.Add(entity);
        }
        else
        {
            entity = await _db.SalesPackages.FirstOrDefaultAsync(p => p.Id == package.Id, cancellationToken)
                     ?? throw new KeyNotFoundException("Pakket niet gevonden.");
        }

        entity.Name = package.Name.Trim();
        entity.Code = string.IsNullOrWhiteSpace(package.Code) ? null : package.Code.Trim().ToUpperInvariant();
        entity.Category = package.Category;
        entity.TokenAmount = package.TokenAmount;
        entity.PriceEuro = package.PriceEuro;
        entity.Description = string.IsNullOrWhiteSpace(package.Description) ? null : package.Description.Trim();
        entity.IsActive = package.IsActive;
        entity.SortOrder = package.SortOrder;

        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeletePackageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SalesPackages.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        _db.SalesPackages.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureVacancyTypeCostsAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.VacancyTypeTokenCosts.Select(c => c.Kind).ToListAsync(cancellationToken);
        var added = false;
        foreach (var (kind, cost) in DefaultTypeCosts)
        {
            if (existing.Contains(kind))
            {
                continue;
            }

            _db.VacancyTypeTokenCosts.Add(new VacancyTypeTokenCost
            {
                Id = Guid.NewGuid(),
                Kind = kind,
                CostTokens = cost,
                IsActive = true
            });
            added = true;
        }

        if (added)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private static SalesCommercialSettings CreateDefaultSettings() => new()
    {
        Id = SingletonId,
        BaseTokenValueEuro = VacancyProductRules.DefaultBaseTokenValueEuro,
        HighlightCarouselTokens = VacancyProductRules.DefaultHighlightCarouselTokens,
        HighlightPulseTokens = VacancyProductRules.DefaultHighlightPulseTokens,
        HighlightCarouselDays = VacancyProductRules.DefaultHighlightCarouselDays,
        StartHighlightBonusTokens = VacancyProductRules.DefaultHighlightCarouselTokens,
        UpdatedAtUtc = DateTime.UtcNow
    };

    private static SalesPackageDto MapPackage(SalesPackage p) => new(
        p.Id,
        p.Name,
        p.Code,
        p.Category.ToString(),
        p.TokenAmount,
        p.PriceEuro,
        p.Description,
        p.IsActive,
        p.SortOrder);

    private static readonly (VacancyKind Kind, decimal Cost)[] DefaultTypeCosts =
    [
        (VacancyKind.Regular, 1m),
        (VacancyKind.Internship, 0.5m),
        (VacancyKind.Volunteer, 0m)
    ];
}
