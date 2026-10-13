using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Infrastructure.Services;

public sealed class CompanyProfileExtrasService : ICompanyProfileExtrasService
{
    private readonly JobsyDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IKvkService _kvk;

    public CompanyProfileExtrasService(JobsyDbContext db, IMemoryCache cache, IKvkService kvk)
    {
        _db = db;
        _cache = cache;
        _kvk = kvk;
    }

    public async Task<Guid> ResolveRootCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var parentId = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => c.ParentCompanyId)
            .FirstOrDefaultAsync(cancellationToken);
        return parentId ?? companyId;
    }

    public async Task<CompanyProfileExtrasDto?> GetAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return null;
        }

        var rootId = company.ParentCompanyId ?? company.Id;
        var root = rootId == company.Id
            ? company
            : await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == rootId, cancellationToken);
        if (root is null)
        {
            return null;
        }

        var culture = await _db.CompanyCultureProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.CompanyId == rootId, cancellationToken);
        var values = await _db.CompanyValuesProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.CompanyId == rootId, cancellationToken);

        var stored = WorkTypeLabels.NormalizeCompanyLabels(
            WorkTypeLabels.SplitStored(root.WorkTypeLabels));
        var suggested = await SuggestFromKvkAsync(root.KvkNumber, cancellationToken);
        var fromKvk = stored.Length == 0 && suggested.Count > 0;
        var displayLabels = stored.Length > 0 ? stored : suggested.ToArray();

        IReadOnlyDictionary<string, int>? sliders = null;
        CulturePersonalityScores? cultureScores = null;
        string? cultureSource = null;
        if (culture is not null)
        {
            cultureSource = culture.Source;
            var answers = CulturePersonalityCatalog.ParseAnswers(culture.AnswersJson);
            sliders = QuickCultureSliders.FromAnswers(answers);
            if (CandidateCompetencyStatuses.IsCompleted(culture.Status))
            {
                cultureScores = new CulturePersonalityScores(
                    culture.AutonomyPercent,
                    culture.InformalPercent,
                    culture.CollaborationPercent,
                    culture.FlexibilityPercent,
                    culture.InnovationPercent,
                    culture.PeopleFirstPercent,
                    culture.OpennessPercent,
                    culture.ConscientiousnessPercent,
                    culture.ExtraversionPercent,
                    culture.AgreeablenessPercent,
                    culture.EmotionalStabilityPercent);
            }
        }

        var cardIds = values is null ? Array.Empty<string>() : CompanyValueCards.Parse(values.CardIdsJson);
        SchwartzValuesScores? valuesScores = values is null ? null : CompanyValueCards.ToScores(values);

        return new CompanyProfileExtrasDto(
            CompanyId: companyId,
            RootCompanyId: rootId,
            WorkTypeLabels: displayLabels,
            SuggestedWorkTypeLabels: suggested,
            CultureSliders: sliders,
            CultureSource: cultureSource,
            CultureScores: cultureScores,
            ValueCardIds: cardIds,
            ValuesScores: valuesScores,
            WorkTypesFromKvk: fromKvk);
    }

    public async Task<CompanyProfileExtrasDto> SaveAsync(
        Guid companyId,
        CompanyProfileExtrasUpdate update,
        CancellationToken cancellationToken = default)
    {
        var rootId = await ResolveRootCompanyIdAsync(companyId, cancellationToken);
        var root = await _db.Companies.FirstOrDefaultAsync(c => c.Id == rootId, cancellationToken)
                   ?? throw new InvalidOperationException("Bedrijf niet gevonden.");

        if (update.WorkTypeLabels is not null)
        {
            if (!WorkTypeLabels.IsValidCompanySelection(update.WorkTypeLabels))
            {
                throw new InvalidOperationException(
                    $"Kies maximaal {WorkTypeLabels.MaxPerCompany} bekende branches.");
            }

            root.WorkTypeLabels = WorkTypeLabels.CombineStoredForCompany(
                WorkTypeLabels.NormalizeCompanyLabels(update.WorkTypeLabels));
        }

        if (update.CultureSliders is not null)
        {
            var sliderError = QuickCultureSliders.Validate(update.CultureSliders);
            if (sliderError is not null)
            {
                throw new InvalidOperationException(sliderError);
            }

            await SaveQuickCultureAsync(rootId, update.CultureSliders, cancellationToken);
        }

        if (update.ValueCardIds is not null)
        {
            if (!CompanyValueCards.IsValidSelection(update.ValueCardIds))
            {
                throw new InvalidOperationException("Kies precies 3 kernwaarden, of geen.");
            }

            var normalized = CompanyValueCards.Normalize(update.ValueCardIds);
            if (normalized.Count == 0)
            {
                var existing = await _db.CompanyValuesProfiles
                    .FirstOrDefaultAsync(p => p.CompanyId == rootId, cancellationToken);
                if (existing is not null)
                {
                    _db.CompanyValuesProfiles.Remove(existing);
                }
            }
            else
            {
                var row = await _db.CompanyValuesProfiles
                    .FirstOrDefaultAsync(p => p.CompanyId == rootId, cancellationToken);
                var now = DateTime.UtcNow;
                if (row is null)
                {
                    row = new CompanyValuesProfile
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = rootId,
                        CreatedAtUtc = now
                    };
                    _db.CompanyValuesProfiles.Add(row);
                }

                CompanyValueCards.ApplyScores(row, normalized);
                row.UpdatedAtUtc = now;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await EvictCultureCacheAsync(rootId, cancellationToken);

        return (await GetAsync(companyId, cancellationToken))!;
    }

    private async Task SaveQuickCultureAsync(
        Guid rootId,
        IReadOnlyDictionary<string, int> sliders,
        CancellationToken cancellationToken)
    {
        var answers = QuickCultureSliders.ToAnswers(sliders);
        var padded = answers.ToDictionary(kv => kv.Key, kv => kv.Value);
        for (var i = 13; i <= 18; i++)
        {
            padded.TryAdd(i, 3);
        }

        var preview = CulturePersonalityCatalog.Score(padded)
                      ?? throw new InvalidOperationException("Cultuurscores konden niet worden berekend.");

        var row = await _db.CompanyCultureProfiles
            .FirstOrDefaultAsync(c => c.CompanyId == rootId, cancellationToken);
        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CompanyCultureProfile
            {
                Id = Guid.NewGuid(),
                CompanyId = rootId,
                CreatedAtUtc = now
            };
            _db.CompanyCultureProfiles.Add(row);
        }

        row.AnswersJson = CulturePersonalityCatalog.SerializeAnswers(padded);
        row.Status = CandidateCompetencyStatuses.Completed;
        row.Source = CompanyCultureSources.Quick;
        row.AutonomyPercent = preview.Autonomy;
        row.InformalPercent = preview.Informal;
        row.CollaborationPercent = preview.Collaboration;
        row.FlexibilityPercent = preview.Flexibility;
        row.InnovationPercent = preview.Innovation;
        row.PeopleFirstPercent = preview.PeopleFirst;
        row.OpennessPercent = preview.Openness;
        row.ConscientiousnessPercent = preview.Conscientiousness;
        row.ExtraversionPercent = preview.Extraversion;
        row.AgreeablenessPercent = preview.Agreeableness;
        row.EmotionalStabilityPercent = preview.EmotionalStability;
        row.UpdatedAtUtc = now;
        row.CompletedAtUtc = now;
    }

    private async Task<IReadOnlyList<string>> SuggestFromKvkAsync(string kvkNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(kvkNumber))
        {
            return [];
        }

        try
        {
            var profile = await _kvk.GetByKvkNumberAsync(kvkNumber.Trim(), cancellationToken);
            return SbiWorkTypeMap.Map(profile?.EffectiveSbiCodes);
        }
        catch
        {
            return [];
        }
    }

    private async Task EvictCultureCacheAsync(Guid companyId, CancellationToken cancellationToken)
    {
        _cache.Remove(CompanyCultureCacheKeys.ForCompany(companyId));
        var childIds = await _db.Companies.AsNoTracking()
            .Where(c => c.ParentCompanyId == companyId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        foreach (var childId in childIds)
        {
            _cache.Remove(CompanyCultureCacheKeys.ForCompany(childId));
        }
    }
}
