using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class VacancyCategoryService : IVacancyCategoryService
{
    private static readonly Regex HexColor = new(@"^#([0-9A-Fa-f]{6})$", RegexOptions.Compiled);

    private readonly JobsyDbContext _db;

    public VacancyCategoryService(JobsyDbContext db) => _db = db;

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var existingSlugs = await _db.VacancyCategories
            .Select(c => c.Slug)
            .ToListAsync(cancellationToken);
        var added = false;

        foreach (var seed in VacancyCategoryDefaults.All)
        {
            if (existingSlugs.Contains(seed.Slug, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            _db.VacancyCategories.Add(new VacancyCategory
            {
                Id = seed.Id,
                Slug = seed.Slug,
                Name = seed.Name,
                ColorHex = seed.ColorHex,
                PublishCostTokens = seed.PublishCostTokens,
                HighlightAvailable = seed.HighlightAvailable && !seed.IsAlwaysFree,
                HighlightCostTokens = seed.IsAlwaysFree ? 0m : seed.HighlightCostTokens,
                PushBomAvailable = seed.PushBomAvailable && !seed.IsAlwaysFree,
                PushBomCostTokens = seed.IsAlwaysFree ? 0m : seed.PushBomCostTokens,
                IsAlwaysFree = seed.IsAlwaysFree,
                ExtraFieldsJson = VacancyCategoryExtraFields.SerializeKeys(seed.ExtraFields),
                PlacementKind = seed.PlacementKind,
                SortOrder = seed.SortOrder,
                IsActive = true,
                ShowInMapFilter = true,
                ShowInLegend = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            added = true;
        }

        // Keep seeded 65+ category name/color aligned with “Geschikt voor 65+” branding.
        var senior = await _db.VacancyCategories
            .FirstOrDefaultAsync(c => c.Id == VacancyCategoryDefaults.SeniorLightId, cancellationToken);
        if (senior is not null)
        {
            var seniorTouched = false;
            if (!string.Equals(senior.ColorHex, VacancyCategoryDefaults.SeniorPlusColorHex, StringComparison.OrdinalIgnoreCase))
            {
                senior.ColorHex = VacancyCategoryDefaults.SeniorPlusColorHex;
                seniorTouched = true;
            }

            if (!string.Equals(senior.Name, VacancyCategoryDefaults.SuitableFor65PlusLabel, StringComparison.Ordinal))
            {
                senior.Name = VacancyCategoryDefaults.SuitableFor65PlusLabel;
                seniorTouched = true;
            }

            if (seniorTouched)
            {
                senior.UpdatedAtUtc = DateTime.UtcNow;
                added = true;
            }
        }

        // Keep Uitzendbureau display name/color stable.
        var uitzend = await _db.VacancyCategories
            .FirstOrDefaultAsync(c => c.Id == VacancyCategoryDefaults.UitzendbureauId, cancellationToken);
        if (uitzend is not null)
        {
            var uitzendTouched = false;
            if (!string.Equals(uitzend.Name, VacancyCategoryDefaults.UitzendbureauLabel, StringComparison.Ordinal))
            {
                uitzend.Name = VacancyCategoryDefaults.UitzendbureauLabel;
                uitzendTouched = true;
            }

            if (!string.Equals(uitzend.ColorHex, VacancyCategoryDefaults.UitzendbureauColorHex, StringComparison.OrdinalIgnoreCase))
            {
                uitzend.ColorHex = VacancyCategoryDefaults.UitzendbureauColorHex;
                uitzendTouched = true;
            }

            if (uitzendTouched)
            {
                uitzend.UpdatedAtUtc = DateTime.UtcNow;
                added = true;
            }
        }

        if (added)
        {
            await _db.SaveChangesAsync(cancellationToken);
            await SyncLegacyTypeCostsAsync(cancellationToken);
        }
    }

    public async Task BackfillVacancyCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        var orphans = await _db.Vacancies
            .Where(v => v.CategoryId == null)
            .ToListAsync(cancellationToken);
        if (orphans.Count == 0)
        {
            return;
        }

        foreach (var vacancy in orphans)
        {
            vacancy.CategoryId = VacancyCategoryDefaults.ResolveDefaultId(vacancy.Kind);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VacancyCategoryDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var rows = await _db.VacancyCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<VacancyCategoryDto>> GetAllAdminAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var rows = await _db.VacancyCategories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<VacancyCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _db.VacancyCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<VacancyCategory?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.VacancyCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<VacancyCategoryDto> CreateAsync(
        string name,
        string colorHex,
        decimal publishCostTokens,
        bool highlightAvailable,
        decimal highlightCostTokens,
        bool pushBomAvailable,
        decimal? pushBomCostTokens,
        bool isAlwaysFree,
        VacancyKind placementKind,
        IEnumerable<string>? extraFields,
        int? sortOrder,
        bool showInMapFilter,
        bool showInLegend,
        CancellationToken cancellationToken = default)
    {
        Validate(name, colorHex, publishCostTokens, highlightCostTokens, pushBomCostTokens);

        var slug = await UniqueSlugAsync(Slugify(name), cancellationToken);
        var order = sortOrder
            ?? ((await _db.VacancyCategories.MaxAsync(c => (int?)c.SortOrder, cancellationToken)) ?? 0) + 10;

        var entity = ApplyRules(new VacancyCategory
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = name.Trim(),
            ColorHex = NormalizeColor(colorHex),
            PublishCostTokens = publishCostTokens,
            HighlightAvailable = highlightAvailable,
            HighlightCostTokens = highlightCostTokens,
            PushBomAvailable = pushBomAvailable,
            PushBomCostTokens = pushBomCostTokens,
            IsAlwaysFree = isAlwaysFree,
            ExtraFieldsJson = VacancyCategoryExtraFields.SerializeKeys(extraFields),
            PlacementKind = placementKind,
            SortOrder = Math.Max(0, order),
            IsActive = true,
            ShowInMapFilter = showInMapFilter,
            ShowInLegend = showInLegend,
            CreatedAtUtc = DateTime.UtcNow
        });

        _db.VacancyCategories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await SyncLegacyTypeCostsAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<VacancyCategoryDto?> UpdateAsync(
        Guid id,
        string name,
        string colorHex,
        decimal publishCostTokens,
        bool highlightAvailable,
        decimal highlightCostTokens,
        bool pushBomAvailable,
        decimal? pushBomCostTokens,
        bool isAlwaysFree,
        VacancyKind placementKind,
        IEnumerable<string>? extraFields,
        int? sortOrder,
        bool? isActive,
        bool showInMapFilter,
        bool showInLegend,
        CancellationToken cancellationToken = default)
    {
        Validate(name, colorHex, publishCostTokens, highlightCostTokens, pushBomCostTokens);

        var entity = await _db.VacancyCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = name.Trim();
        entity.ColorHex = NormalizeColor(colorHex);
        entity.PublishCostTokens = publishCostTokens;
        entity.HighlightAvailable = highlightAvailable;
        entity.HighlightCostTokens = highlightCostTokens;
        entity.PushBomAvailable = pushBomAvailable;
        entity.PushBomCostTokens = pushBomCostTokens;
        entity.IsAlwaysFree = isAlwaysFree;
        entity.ExtraFieldsJson = VacancyCategoryExtraFields.SerializeKeys(extraFields);
        entity.PlacementKind = placementKind;
        if (sortOrder is int order)
        {
            entity.SortOrder = Math.Max(0, order);
        }

        if (isActive is bool active)
        {
            entity.IsActive = active;
        }

        entity.ShowInMapFilter = showInMapFilter;
        entity.ShowInLegend = showInLegend;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        ApplyRules(entity);

        await _db.SaveChangesAsync(cancellationToken);
        await SyncLegacyTypeCostsAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.VacancyCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        var inUse = await _db.Vacancies.AnyAsync(v => v.CategoryId == id, cancellationToken);
        if (inUse)
        {
            // Soft-delete when vacancies still reference the category.
            entity.IsActive = false;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        _db.VacancyCategories.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<VacancyCategoryPricing> ResolvePricingAsync(
        Guid? categoryId,
        VacancyKind fallbackKind,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        VacancyCategory? category = null;
        if (categoryId is Guid id)
        {
            category = await _db.VacancyCategories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        category ??= await _db.VacancyCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == VacancyCategoryDefaults.ResolveDefaultId(fallbackKind), cancellationToken);

        if (category is null)
        {
            var seed = VacancyCategoryDefaults.All.First(c => c.PlacementKind == fallbackKind
                || (fallbackKind == VacancyKind.Regular && c.Id == VacancyCategoryDefaults.RegulierId));
            return new VacancyCategoryPricing(
                seed.Id,
                seed.Name,
                seed.ColorHex,
                seed.IsAlwaysFree ? 0m : seed.PublishCostTokens,
                seed.HighlightAvailable && !seed.IsAlwaysFree,
                seed.IsAlwaysFree ? 0m : seed.HighlightCostTokens,
                seed.PushBomAvailable && !seed.IsAlwaysFree,
                seed.IsAlwaysFree ? 0m : seed.PushBomCostTokens,
                !seed.IsAlwaysFree && seed.PushBomAvailable && seed.PushBomCostTokens is null,
                seed.IsAlwaysFree,
                seed.PlacementKind);
        }

        var alwaysFree = category.IsAlwaysFree;
        var pushBomAvailable = category.PushBomAvailable && !alwaysFree;
        return new VacancyCategoryPricing(
            category.Id,
            category.Name,
            category.ColorHex,
            alwaysFree ? 0m : category.PublishCostTokens,
            category.HighlightAvailable && !alwaysFree,
            alwaysFree ? 0m : category.HighlightCostTokens,
            pushBomAvailable,
            alwaysFree ? 0m : category.PushBomCostTokens,
            pushBomAvailable && category.PushBomCostTokens is null,
            alwaysFree,
            category.PlacementKind);
    }

    private async Task SyncLegacyTypeCostsAsync(CancellationToken cancellationToken)
    {
        // Keep VacancyTypeTokenCosts aligned with the primary category per PlacementKind
        // so older Kind-based pricing paths stay consistent.
        foreach (var kind in Enum.GetValues<VacancyKind>())
        {
            var preferredId = VacancyCategoryDefaults.ResolveDefaultId(kind);
            var category = await _db.VacancyCategories.AsNoTracking()
                .Where(c => c.IsActive && c.PlacementKind == kind)
                .OrderBy(c => c.Id == preferredId ? 0 : 1)
                .ThenBy(c => c.SortOrder)
                .FirstOrDefaultAsync(cancellationToken);
            if (category is null)
            {
                continue;
            }

            var row = await _db.VacancyTypeTokenCosts
                .FirstOrDefaultAsync(c => c.Kind == kind, cancellationToken);
            if (row is null)
            {
                _db.VacancyTypeTokenCosts.Add(new VacancyTypeTokenCost
                {
                    Id = Guid.NewGuid(),
                    Kind = kind,
                    CostTokens = category.IsAlwaysFree ? 0m : category.PublishCostTokens,
                    IsActive = true
                });
            }
            else
            {
                row.CostTokens = category.IsAlwaysFree ? 0m : category.PublishCostTokens;
                row.IsActive = true;
            }

            if (kind == VacancyKind.Regular)
            {
                var publish = await _db.TokenSpendCosts
                    .FirstOrDefaultAsync(c => c.Reason == TokenSpendReason.Publish, cancellationToken);
                if (publish is not null)
                {
                    publish.CostTokens = category.IsAlwaysFree ? 0m : category.PublishCostTokens;
                    publish.IsActive = true;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static VacancyCategory ApplyRules(VacancyCategory entity)
    {
        if (entity.IsAlwaysFree)
        {
            entity.PublishCostTokens = 0m;
            entity.HighlightAvailable = false;
            entity.HighlightCostTokens = 0m;
            entity.PushBomAvailable = false;
            entity.PushBomCostTokens = 0m;
            entity.PlacementKind = VacancyKind.Volunteer;
        }

        if (!entity.HighlightAvailable)
        {
            entity.HighlightCostTokens = 0m;
        }

        if (!entity.PushBomAvailable)
        {
            entity.PushBomCostTokens = 0m;
        }

        return entity;
    }

    private static void Validate(
        string name,
        string colorHex,
        decimal publishCostTokens,
        decimal highlightCostTokens,
        decimal? pushBomCostTokens)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Categorienaam is verplicht.");
        }

        if (name.Trim().Length > 128)
        {
            throw new ArgumentException("Categorienaam mag maximaal 128 tekens zijn.");
        }

        if (!HexColor.IsMatch(NormalizeColor(colorHex)))
        {
            throw new ArgumentException("Kies een geldige kleur (bijv. #F54A1B).");
        }

        if (publishCostTokens < 0 || highlightCostTokens < 0 || (pushBomCostTokens is < 0))
        {
            throw new ArgumentException("Tokenkosten mogen niet negatief zijn.");
        }
    }

    private static string NormalizeColor(string? colorHex)
    {
        var value = (colorHex ?? "").Trim();
        if (string.IsNullOrEmpty(value))
        {
            return "#F54A1B";
        }

        if (!value.StartsWith('#'))
        {
            value = "#" + value;
        }

        return value.ToUpperInvariant();
    }

    private static string Slugify(string name)
    {
        var slug = name.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "categorie" : slug[..Math.Min(slug.Length, 64)];
    }

    private async Task<string> UniqueSlugAsync(string baseSlug, CancellationToken cancellationToken)
    {
        var slug = baseSlug;
        var i = 2;
        while (await _db.VacancyCategories.AnyAsync(c => c.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{i++}";
        }

        return slug;
    }

    private static VacancyCategoryDto Map(VacancyCategory c)
    {
        var keys = VacancyCategoryExtraFields.DeserializeKeys(c.ExtraFieldsJson);
        var defs = keys
            .Select(VacancyCategoryExtraFields.Get)
            .Where(d => d is not null)
            .Select(d => new VacancyCategoryFieldDto(d!.Key, d.Label, d.InputType, d.Options))
            .ToList();

        return new VacancyCategoryDto(
            c.Id,
            c.Slug,
            c.Name,
            c.ColorHex,
            c.PublishCostTokens,
            c.HighlightAvailable,
            c.HighlightCostTokens,
            c.PushBomAvailable,
            c.PushBomCostTokens,
            c.IsAlwaysFree,
            c.PlacementKind.ToString(),
            keys,
            defs,
            c.SortOrder,
            c.IsActive,
            c.ShowInMapFilter,
            c.ShowInLegend);
    }
}
