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

    public async Task<IReadOnlyDictionary<Guid, CulturePersonalityScores>> GetForCompaniesAsync(
        IReadOnlyCollection<Guid> companyIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, CulturePersonalityScores>();
        if (companyIds.Count == 0)
        {
            return result;
        }

        var distinct = companyIds.Where(id => id != Guid.Empty).Distinct().ToList();
        var missing = new List<Guid>(distinct.Count);
        foreach (var id in distinct)
        {
            if (_cache.TryGetValue(CompanyCultureCacheKeys.ForCompany(id), out CulturePersonalityScores? cached))
            {
                if (cached is not null)
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

        // One query for the requested companies (parent ids), one for all relevant profiles.
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

        var rows = await _db.CompanyCultureProfiles.AsNoTracking()
            .Where(p => profileIds.Contains(p.CompanyId)
                        && p.Status == CandidateCompetencyStatuses.Completed)
            .ToListAsync(cancellationToken);

        var scoresByCompany = new Dictionary<Guid, CulturePersonalityScores>();
        foreach (var row in rows)
        {
            var scores = ToScores(row);
            if (scores is null)
            {
                continue;
            }

            scoresByCompany[row.CompanyId] = scores;
            _cache.Set(CompanyCultureCacheKeys.ForCompany(row.CompanyId), scores, CompanyCultureCacheKeys.Ttl);
        }

        foreach (var id in missing)
        {
            CulturePersonalityScores? resolved = null;
            if (scoresByCompany.TryGetValue(id, out var own))
            {
                resolved = own;
            }
            else if (parentByChild.TryGetValue(id, out var parentId)
                     && parentId is Guid pid
                     && scoresByCompany.TryGetValue(pid, out var parent))
            {
                resolved = parent;
            }

            _cache.Set(CompanyCultureCacheKeys.ForCompany(id), resolved, CompanyCultureCacheKeys.Ttl);
            if (resolved is not null)
            {
                result[id] = resolved;
            }
        }

        return result;
    }

    private static CulturePersonalityScores? ToScores(CompanyCultureProfile row)
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
