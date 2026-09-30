using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Infrastructure.Services;

public sealed class CompanyCultureLookup : ICompanyCultureLookup
{
    private readonly JobsyDbContext _db;
    private readonly IMemoryCache _cache;

    public CompanyCultureLookup(JobsyDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyDictionary<Guid, CompanyCultureLookupResult>> GetForCompaniesAsync(
        IReadOnlyCollection<Guid> companyIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, CompanyCultureLookupResult>();
        if (companyIds.Count == 0)
        {
            return result;
        }

        var distinct = companyIds.Where(id => id != Guid.Empty).Distinct().ToList();
        var missing = new List<Guid>(distinct.Count);
        foreach (var id in distinct)
        {
            if (_cache.TryGetValue(CompanyCultureCacheKeys.ForCompany(id), out CompanyCultureLookupResult? cached))
            {
                if (cached is not null
                    && (cached.Culture is not null
                        || cached.Values is not null
                        || cached.Engagement is { Count: > 0 }))
                {
                    result[id] = cached;
                }

                continue;
            }

            missing.Add(id);
        }

        if (missing.Count == 0)
        {
            return result;
        }

        var parents = await _db.Companies.AsNoTracking()
            .Where(c => missing.Contains(c.Id))
            .Select(c => new { c.Id, c.ParentCompanyId })
            .ToListAsync(cancellationToken);

        var parentByChild = parents.ToDictionary(c => c.Id, c => c.ParentCompanyId);
        var parentIds = parents
            .Where(c => c.ParentCompanyId is Guid)
            .Select(c => c.ParentCompanyId!.Value)
            .Distinct()
            .ToList();
        var profileIds = missing.Concat(parentIds).Distinct().ToList();

        var cultureRows = await _db.CompanyCultureProfiles.AsNoTracking()
            .Where(p => profileIds.Contains(p.CompanyId)
                        && p.Status == CandidateCompetencyStatuses.Completed)
            .ToListAsync(cancellationToken);

        var valuesRows = await _db.CompanyValuesProfiles.AsNoTracking()
            .Where(p => profileIds.Contains(p.CompanyId))
            .ToListAsync(cancellationToken);

        var engagementRows = await _db.CompanyEngagementClaims.AsNoTracking()
            .Where(p => profileIds.Contains(p.CompanyId)
                        && p.Status != CompanyEngagementStatuses.Removed)
            .ToListAsync(cancellationToken);

        var cultureByCompany = new Dictionary<Guid, CulturePersonalityScores>();
        foreach (var row in cultureRows)
        {
            var scores = ToCultureScores(row);
            if (scores is null)
            {
                continue;
            }

            cultureByCompany[row.CompanyId] = scores;
        }

        var valuesByCompany = new Dictionary<Guid, SchwartzValuesScores>();
        foreach (var row in valuesRows)
        {
            valuesByCompany[row.CompanyId] = CompanyValueCards.ToScores(row);
        }

        var engagementByCompany = engagementRows
            .GroupBy(r => r.CompanyId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CompanyEngagementMatchItem>)g
                    .Select(r => new CompanyEngagementMatchItem(r.ItemId, r.Status))
                    .ToList());

        foreach (var id in missing)
        {
            CulturePersonalityScores? culture = null;
            SchwartzValuesScores? values = null;
            IReadOnlyList<CompanyEngagementMatchItem>? engagement = null;

            if (cultureByCompany.TryGetValue(id, out var ownCulture))
            {
                culture = ownCulture;
            }
            else if (parentByChild.TryGetValue(id, out var parentId)
                     && parentId is Guid pid
                     && cultureByCompany.TryGetValue(pid, out var parentCulture))
            {
                culture = parentCulture;
            }

            if (valuesByCompany.TryGetValue(id, out var ownValues))
            {
                values = ownValues;
            }
            else if (parentByChild.TryGetValue(id, out var parentId2)
                     && parentId2 is Guid pid2
                     && valuesByCompany.TryGetValue(pid2, out var parentValues))
            {
                values = parentValues;
            }

            if (engagementByCompany.TryGetValue(id, out var ownEngagement))
            {
                engagement = ownEngagement;
            }
            else if (parentByChild.TryGetValue(id, out var parentId3)
                     && parentId3 is Guid pid3
                     && engagementByCompany.TryGetValue(pid3, out var parentEngagement))
            {
                engagement = parentEngagement;
            }

            var resolved = new CompanyCultureLookupResult(culture, values, engagement);
            _cache.Set(CompanyCultureCacheKeys.ForCompany(id), resolved, CompanyCultureCacheKeys.Ttl);
            if (culture is not null || values is not null || engagement is { Count: > 0 })
            {
                result[id] = resolved;
            }
        }

        return result;
    }

    private static CulturePersonalityScores? ToCultureScores(CompanyCultureProfile row)
    {
        var scores = new CulturePersonalityScores(
            row.AutonomyPercent,
            row.InformalPercent,
            row.CollaborationPercent,
            row.FlexibilityPercent,
            row.InnovationPercent,
            row.PeopleFirstPercent,
            row.OpennessPercent,
            row.ConscientiousnessPercent,
            row.ExtraversionPercent,
            row.AgreeablenessPercent,
            row.EmotionalStabilityPercent);
        return scores.Autonomy is not null ? scores : null;
    }
}
