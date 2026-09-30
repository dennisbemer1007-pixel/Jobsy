using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Scholen;

public sealed class SchoolAggregateSnapshotter : ISchoolAggregateSnapshotter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly JobsyDbContext _db;
    private readonly TimeProvider _clock;

    public SchoolAggregateSnapshotter(JobsyDbContext db, TimeProvider? clock = null)
    {
        _db = db;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<int> SnapshotAllYearsForSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var years = await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.SchoolId == schoolId)
            .Select(c => c.SchoolYearStart)
            .Distinct()
            .ToListAsync(cancellationToken);
        var total = 0;
        foreach (var year in years)
        {
            total += await SnapshotSchoolYearAsync(schoolId, year, cancellationToken);
        }

        return total;
    }

    public async Task<int> SnapshotAllSchoolsForYearAsync(
        int schoolYearStart,
        CancellationToken cancellationToken = default)
    {
        var schoolIds = await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.SchoolYearStart == schoolYearStart)
            .Select(c => c.SchoolId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var total = 0;
        foreach (var schoolId in schoolIds)
        {
            total += await SnapshotSchoolYearAsync(schoolId, schoolYearStart, cancellationToken);
        }

        return total;
    }

    public async Task<int> SnapshotSchoolYearAsync(
        Guid schoolId,
        int schoolYearStart,
        CancellationToken cancellationToken = default)
    {
        var classes = await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.SchoolId == schoolId && c.SchoolYearStart == schoolYearStart)
            .Include(c => c.PupilCodes)
            .ToListAsync(cancellationToken);

        var classIds = classes.Select(c => c.Id).ToList();
        var results = classIds.Count == 0
            ? []
            : await _db.PupilResults.AsNoTracking()
                .Where(r => classIds.Contains(r.SchoolClassId))
                .ToListAsync(cancellationToken);
        var resultsByClass = results.GroupBy(r => r.SchoolClassId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PupilResult>)g.ToList());

        var snapshotAt = _clock.GetUtcNow().UtcDateTime.Date; // date precision only
        var snapshotAtUtc = DateTime.SpecifyKind(snapshotAt, DateTimeKind.Utc);

        // Idempotent replace for this school+year
        var oldClass = await _db.SchoolClassAggregates
            .Where(a => a.SchoolId == schoolId && a.SchoolYearStart == schoolYearStart)
            .ToListAsync(cancellationToken);
        _db.SchoolClassAggregates.RemoveRange(oldClass);
        var oldYear = await _db.SchoolYearAggregates
            .Where(a => a.SchoolId == schoolId && a.SchoolYearStart == schoolYearStart)
            .ToListAsync(cancellationToken);
        _db.SchoolYearAggregates.RemoveRange(oldYear);

        var written = 0;
        var classBuckets = new List<ClassBucket>();

        foreach (var schoolClass in classes)
        {
            var classResults = resultsByClass.GetValueOrDefault(schoolClass.Id) ?? [];
            var codes = schoolClass.PupilCodes;
            var pupilCount = codes.Count;
            var startedCount = codes.Count(c => c.Status != PupilCodeStatus.NotStarted);
            var completedCount = classResults.Count;
            var raw = ClassResultsAggregator.CountRaw(classResults);
            var bucket = new ClassBucket(
                schoolClass.Name,
                schoolClass.Level,
                schoolClass.Year,
                pupilCount,
                startedCount,
                completedCount,
                raw);
            classBuckets.Add(bucket);

            if (completedCount < SchoolAnonymity.MinGroupSize)
            {
                continue;
            }

            _db.SchoolClassAggregates.Add(new SchoolClassAggregate
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                SchoolYearStart = schoolYearStart,
                ClassLabel = schoolClass.Name,
                Level = schoolClass.Level,
                Year = schoolClass.Year,
                PupilCount = pupilCount,
                StartedCount = startedCount,
                CompletedCount = completedCount,
                RiasecTop3CountsJson = ToJson(ClassResultsAggregator.Top3Riasec(raw.Riasec)),
                TopValueCountsJson = ToJson(TopN(raw.Values, 5)),
                TopCultureCountsJson = ToJson(TopN(raw.Cultures, 5)),
                CompetenceBandCountsJson = ToJson(raw.CompetenceBands),
                DreamJobCountsJson = ToJson(ClassResultsAggregator.CollapseSparseDreamJobs(raw.DreamJobs)),
                SnapshotAtUtc = snapshotAtUtc
            });
            written++;
        }

        var schoolCompleted = classBuckets.Sum(b => b.CompletedCount);
        if (schoolCompleted >= SchoolAnonymity.MinGroupSize)
        {
            // Class aggregates (≥5) + small classes merged into school total only when school ≥ 5.
            var includeAll = classBuckets;
            var pupilCount = includeAll.Sum(b => b.PupilCount);
            var startedCount = includeAll.Sum(b => b.StartedCount);
            var riasec = ClassResultsAggregator.MergeCounts(includeAll.Select(b => b.Raw.Riasec));
            var values = ClassResultsAggregator.MergeCounts(includeAll.Select(b => b.Raw.Values));
            var cultures = ClassResultsAggregator.MergeCounts(includeAll.Select(b => b.Raw.Cultures));
            var bands = ClassResultsAggregator.MergeCounts(includeAll.Select(b => b.Raw.CompetenceBands));
            var dreams = ClassResultsAggregator.MergeCounts(includeAll.Select(b => b.Raw.DreamJobs));

            _db.SchoolYearAggregates.Add(new SchoolYearAggregate
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                SchoolYearStart = schoolYearStart,
                PupilCount = pupilCount,
                StartedCount = startedCount,
                CompletedCount = schoolCompleted,
                RiasecTop3CountsJson = ToJson(ClassResultsAggregator.Top3Riasec(riasec)),
                TopValueCountsJson = ToJson(TopN(values, 5)),
                TopCultureCountsJson = ToJson(TopN(cultures, 5)),
                CompetenceBandCountsJson = ToJson(bands),
                DreamJobCountsJson = ToJson(ClassResultsAggregator.CollapseSparseDreamJobs(dreams)),
                SnapshotAtUtc = snapshotAtUtc
            });
            written++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        written += await RebuildPlatformYearAsync(schoolYearStart, snapshotAtUtc, cancellationToken);
        return written;
    }

    private async Task<int> RebuildPlatformYearAsync(
        int schoolYearStart,
        DateTime snapshotAtUtc,
        CancellationToken cancellationToken)
    {
        var schoolRows = await _db.SchoolYearAggregates
            .Where(a => a.SchoolYearStart == schoolYearStart && a.SchoolId != null)
            .ToListAsync(cancellationToken);

        var oldPlatform = await _db.SchoolYearAggregates
            .Where(a => a.SchoolYearStart == schoolYearStart && a.SchoolId == null)
            .ToListAsync(cancellationToken);
        _db.SchoolYearAggregates.RemoveRange(oldPlatform);

        if (schoolRows.Count == 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return 0;
        }

        var completed = schoolRows.Sum(r => r.CompletedCount);
        if (completed < SchoolAnonymity.MinGroupSize)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return 0;
        }

        var riasec = ClassResultsAggregator.MergeCounts(schoolRows.Select(r => FromJson(r.RiasecTop3CountsJson)));
        // Platform should reflect full dimension counts when possible; school rows store top-3 only for RIASEC.
        // Re-merge what we have (top-3 per school) — best available from aggregates-only storage.
        var values = ClassResultsAggregator.MergeCounts(schoolRows.Select(r => FromJson(r.TopValueCountsJson)));
        var cultures = ClassResultsAggregator.MergeCounts(schoolRows.Select(r => FromJson(r.TopCultureCountsJson)));
        var bands = ClassResultsAggregator.MergeCounts(schoolRows.Select(r => FromJson(r.CompetenceBandCountsJson)));
        var dreams = ClassResultsAggregator.MergeCounts(schoolRows.Select(r => FromJson(r.DreamJobCountsJson)));

        _db.SchoolYearAggregates.Add(new SchoolYearAggregate
        {
            Id = Guid.NewGuid(),
            SchoolId = null,
            SchoolYearStart = schoolYearStart,
            PupilCount = schoolRows.Sum(r => r.PupilCount),
            StartedCount = schoolRows.Sum(r => r.StartedCount),
            CompletedCount = completed,
            RiasecTop3CountsJson = ToJson(ClassResultsAggregator.Top3Riasec(riasec)),
            TopValueCountsJson = ToJson(TopN(values, 10)),
            TopCultureCountsJson = ToJson(TopN(cultures, 10)),
            CompetenceBandCountsJson = ToJson(bands),
            DreamJobCountsJson = ToJson(ClassResultsAggregator.CollapseSparseDreamJobs(dreams)),
            SnapshotAtUtc = snapshotAtUtc
        });
        await _db.SaveChangesAsync(cancellationToken);
        return 1;
    }

    private static IReadOnlyDictionary<string, int> TopN(IReadOnlyDictionary<string, int> source, int n)
        => source
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(n)
            .Where(kv => kv.Value > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

    private static string ToJson(IReadOnlyDictionary<string, int> counts)
        => JsonSerializer.Serialize(counts, JsonOpts);

    private static Dictionary<string, int> FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(json, JsonOpts)
                   ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private sealed record ClassBucket(
        string ClassLabel,
        SchoolLevel Level,
        int Year,
        int PupilCount,
        int StartedCount,
        int CompletedCount,
        RawResultCounts Raw);
}
