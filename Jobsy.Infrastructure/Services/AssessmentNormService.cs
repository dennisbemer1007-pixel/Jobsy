using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class AssessmentNormService : IAssessmentNormService
{
    private readonly JobsyDbContext _db;

    public AssessmentNormService(JobsyDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<string, double>?> GetMeansIfReadyAsync(
        AssessmentKind kind,
        CancellationToken ct = default)
    {
        if (kind is AssessmentKind.Competence)
        {
            return null; // Johnson2014 via INormProvider
        }

        var n = await GetSampleSizeAsync(kind, ct);
        if (n < IAssessmentNormService.MinSampleSize)
        {
            return null;
        }

        var latest = await _db.AssessmentNormSnapshots.AsNoTracking()
            .Where(s => s.Kind == kind)
            .GroupBy(s => s.Domain)
            .Select(g => g.OrderByDescending(x => x.ComputedAtUtc).First())
            .ToListAsync(ct);

        if (latest.Count == 0 || latest.Any(s => s.N < IAssessmentNormService.MinSampleSize))
        {
            return null;
        }

        return latest.ToDictionary(s => s.Domain, s => s.Mean, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<int> GetSampleSizeAsync(AssessmentKind kind, CancellationToken ct = default)
    {
        var row = await _db.AssessmentNormSnapshots.AsNoTracking()
            .Where(s => s.Kind == kind)
            .OrderByDescending(s => s.ComputedAtUtc)
            .Select(s => (int?)s.N)
            .FirstOrDefaultAsync(ct);
        return row ?? 0;
    }

    public async Task RecomputeAsync(CancellationToken ct = default)
    {
        foreach (var kind in new[] { AssessmentKind.Career, AssessmentKind.Culture, AssessmentKind.Values })
        {
            await RecomputeKindAsync(kind, ct);
        }
    }

    private async Task RecomputeKindAsync(AssessmentKind kind, CancellationToken ct)
    {
        var rows = await _db.CandidateDeepAnalyses.AsNoTracking()
            .Where(d => d.Kind == kind && d.Status == CandidateDeepAnalysisStatuses.Completed)
            .Join(
                _db.Users.AsNoTracking().Where(u => !u.Email.Contains("jobsy.local") && !u.Email.Contains("demo")),
                d => d.UserId,
                u => u.Id,
                (d, _) => d)
            .Select(d => d.AnswersJson)
            .ToListAsync(ct);

        var buckets = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var json in rows)
        {
            var answers = DeepAnalysisCatalog.ParseAnswersJson(json, kind);
            foreach (var score in DeepAnalysisCatalog.ScoreDomains(answers, kind))
            {
                if (!buckets.TryGetValue(score.Domain, out var list))
                {
                    list = [];
                    buckets[score.Domain] = list;
                }

                list.Add(score.Percent);
            }
        }

        var n = rows.Count;
        var now = DateTime.UtcNow;
        var existing = await _db.AssessmentNormSnapshots.Where(s => s.Kind == kind).ToListAsync(ct);
        _db.AssessmentNormSnapshots.RemoveRange(existing);

        foreach (var (domain, values) in buckets)
        {
            if (values.Count == 0)
            {
                continue;
            }

            var ordered = values.OrderBy(v => v).ToList();
            _db.AssessmentNormSnapshots.Add(new AssessmentNormSnapshot
            {
                Id = Guid.NewGuid(),
                Kind = kind,
                Domain = domain,
                N = n,
                Mean = values.Average(),
                P25 = Percentile(ordered, 0.25),
                P50 = Percentile(ordered, 0.50),
                P75 = Percentile(ordered, 0.75),
                ComputedAtUtc = now
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private static double Percentile(List<int> ordered, double p)
    {
        if (ordered.Count == 0)
        {
            return 0;
        }

        var idx = (int)Math.Clamp(Math.Round((ordered.Count - 1) * p), 0, ordered.Count - 1);
        return ordered[idx];
    }
}
