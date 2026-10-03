using System.Text;
using System.Text.Json;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Scholen;

/// <summary>
/// Admin Scholen-rapportage — reads <see cref="SchoolClassAggregate"/> / <see cref="SchoolYearAggregate"/> only.
/// Never queries live pupil tables.
/// </summary>
public interface ISchoolReportingService
{
    Task<SchoolReportViewDto> GetReportAsync(SchoolReportFilterDto filter, CancellationToken cancellationToken = default);

    Task<(byte[] Bytes, string FileName)> ExportCsvAsync(
        SchoolReportFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<SchoolRetentionStatusDto> GetRetentionStatusAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> ListSchoolYearsAsync(CancellationToken cancellationToken = default);
}

public sealed class SchoolReportingService : ISchoolReportingService
{
    public const string CsvHeader =
        "school_year;school_id;school_name;vragenlijst;level;year;metric;key;count_or_lt5";

    public const string CsvHeaderHelp =
        "CSV uit aggregaten (k≥5), één vragenlijst per export. Kolommen: school_year;school_id;school_name;vragenlijst;level;year;metric;key;count_or_lt5. "
        + "Waarde \"< 5\" wanneer de onderliggende telling onder de anonymiteitsdrempel ligt. Geen code- of leerling-id’s.";

    private static readonly JsonSerializerOptions JsonOpts = new();

    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;
    private readonly TimeProvider _clock;

    public SchoolReportingService(
        JobsyDbContext db,
        IPlatformFeatureService features,
        TimeProvider? clock = null)
    {
        _db = db;
        _features = features;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<int>> ListSchoolYearsAsync(CancellationToken cancellationToken = default)
    {
        var years = await _db.SchoolYearAggregates.AsNoTracking()
            .Select(a => a.SchoolYearStart)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(cancellationToken);
        if (years.Count > 0)
        {
            return years;
        }

        var snap = await _features.GetAsync(cancellationToken);
        var today = TodayAmsterdam();
        return [SchoolYear.Current(today, snap.SchoolRetentionCutoffMonth, snap.SchoolRetentionCutoffDay)];
    }

    public async Task<SchoolRetentionStatusDto> GetRetentionStatusAsync(CancellationToken cancellationToken = default)
    {
        var snap = await _features.GetAsync(cancellationToken);
        var today = TodayAmsterdam();
        var yearStart = SchoolYear.Current(today, snap.SchoolRetentionCutoffMonth, snap.SchoolRetentionCutoffDay);
        var next = SchoolYear.EndsOn(yearStart, snap.SchoolRetentionCutoffMonth, snap.SchoolRetentionCutoffDay);
        var runs = await _db.SchoolRetentionRuns.AsNoTracking()
            .OrderByDescending(r => r.RanAtUtc)
            .Take(25)
            .Select(r => new SchoolRetentionRunDto(
                r.Id,
                r.RanAtUtc,
                r.CutoffDate,
                r.ClassesDeleted,
                r.CodesDeleted,
                r.ResultsDeleted,
                r.AggregatesWritten,
                r.Outcome))
            .ToListAsync(cancellationToken);
        return new SchoolRetentionStatusDto(next, next.ToString("dd MMMM yyyy"), runs);
    }

    public async Task<SchoolReportViewDto> GetReportAsync(
        SchoolReportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        // Guard: only aggregate tables (repository contract for tests).
        var classAggs = await QueryClassAggregatesAsync(filter, cancellationToken);
        var yearAgg = await QueryYearAggregateAsync(filter, cancellationToken);

        var schoolName = filter.SchoolId is Guid sid
            ? await _db.Schools.AsNoTracking()
                .Where(s => s.Id == sid)
                .Select(s => s.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var hasClassFilter = filter.Level is not null || filter.Year is not null;
        var completed = hasClassFilter
            ? classAggs.Sum(a => a.CompletedCount)
            : yearAgg?.CompletedCount ?? classAggs.Sum(a => a.CompletedCount);
        var started = hasClassFilter
            ? classAggs.Sum(a => a.StartedCount)
            : yearAgg?.StartedCount ?? classAggs.Sum(a => a.StartedCount);
        var classCount = classAggs.Count;
        var masked = completed < SchoolAnonymity.MinGroupSize;

        int? activeSchools = null;
        if (filter.SchoolId is null && !hasClassFilter)
        {
            activeSchools = await _db.SchoolYearAggregates.AsNoTracking()
                .Where(a => a.SchoolYearStart == filter.SchoolYearStart
                            && a.SchoolId != null
                            && a.QuestionSet == filter.QuestionSet)
                .Select(a => a.SchoolId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        IReadOnlyDictionary<string, int> riasec;
        IReadOnlyDictionary<string, int> values;
        IReadOnlyDictionary<string, int> cultures;
        IReadOnlyDictionary<string, int> dreams;

        if (hasClassFilter || yearAgg is null)
        {
            riasec = MergeJson(classAggs.Select(a => a.RiasecTop3CountsJson));
            values = MergeJson(classAggs.Select(a => a.TopValueCountsJson));
            cultures = MergeJson(classAggs.Select(a => a.TopCultureCountsJson));
            dreams = MergeJson(classAggs.Select(a => a.DreamJobCountsJson));
        }
        else
        {
            riasec = FromJson(yearAgg.RiasecTop3CountsJson);
            values = FromJson(yearAgg.TopValueCountsJson);
            cultures = FromJson(yearAgg.TopCultureCountsJson);
            dreams = FromJson(yearAgg.DreamJobCountsJson);
        }

        var perLevel = classAggs
            .GroupBy(a => (a.Level, a.Year))
            .OrderBy(g => g.Key.Level)
            .ThenBy(g => g.Key.Year)
            .Select(g =>
            {
                var c = g.Sum(x => x.CompletedCount);
                var m = c < SchoolAnonymity.MinGroupSize;
                return new SchoolReportLevelYearRowDto(
                    g.Key.Level,
                    g.Key.Year,
                    m ? null : g.Count(),
                    m ? null : g.Sum(x => x.PupilCount),
                    m ? null : g.Sum(x => x.StartedCount),
                    m ? null : c,
                    m);
            })
            .ToList();

        double? pct = null;
        if (!masked && started > 0)
        {
            pct = Math.Round(100d * completed / Math.Max(started, completed), 1);
        }
        else if (!masked && yearAgg is { PupilCount: > 0 })
        {
            pct = Math.Round(100d * completed / yearAgg.PupilCount, 1);
        }

        return new SchoolReportViewDto(
            filter.SchoolYearStart,
            SchoolYear.Label(filter.SchoolYearStart),
            filter.SchoolId,
            schoolName,
            filter.Level,
            filter.Year,
            filter.QuestionSet,
            MaskInt(activeSchools, masked && filter.SchoolId is null),
            MaskInt(classCount, masked),
            MaskInt(started, masked),
            MaskInt(completed, masked),
            masked ? null : pct,
            ToNamed(riasec, masked, take: 3),
            ToNamed(values, masked, take: 5),
            ToNamed(cultures, masked, take: 5),
            ToNamed(ClassResultsAggregator.CollapseSparseDreamJobs(dreams), masked, take: 11),
            perLevel,
            CsvHeaderHelp);
    }

    public async Task<(byte[] Bytes, string FileName)> ExportCsvAsync(
        SchoolReportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var view = await GetReportAsync(filter, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine(CsvHeader);
        var vragenlijst = view.QuestionSet == PupilQuestionSet.Groep78 ? "groep78" : "vo";
        void Row(string metric, string key, int? count, bool masked)
        {
            var school = view.SchoolId?.ToString("D") ?? "";
            var schoolName = view.SchoolName ?? "";
            var level = view.Level?.ToString() ?? "";
            var year = view.Year?.ToString() ?? "";
            var val = masked || count is null ? "< 5" : count.Value.ToString();
            sb.Append(view.SchoolYearStart).Append(';')
                .Append(school).Append(';')
                .Append(Escape(schoolName)).Append(';')
                .Append(vragenlijst).Append(';')
                .Append(level).Append(';')
                .Append(year).Append(';')
                .Append(metric).Append(';')
                .Append(Escape(key)).Append(';')
                .Append(val)
                .AppendLine();
        }

        Row("kpi", "active_schools", view.ActiveSchools, view.ActiveSchools is null && view.SchoolId is null);
        Row("kpi", "classes", view.ClassCount, view.ClassCount is null);
        Row("kpi", "started", view.StartedCount, view.StartedCount is null);
        Row("kpi", "completed", view.CompletedCount, view.CompletedCount is null);
        foreach (var n in view.RiasecTop3)
        {
            Row("riasec", n.Key, n.Count, n.Masked);
        }

        foreach (var n in view.TopValues)
        {
            Row("value", n.Key, n.Count, n.Masked);
        }

        foreach (var n in view.TopCultures)
        {
            Row("culture", n.Key, n.Count, n.Masked);
        }

        foreach (var n in view.DreamJobsTop10)
        {
            Row("dreamjob", n.Key, n.Count, n.Masked);
        }

        foreach (var r in view.PerLevelYear)
        {
            var m = r.Masked;
            sb.Append(view.SchoolYearStart).Append(';')
                .Append(view.SchoolId?.ToString("D") ?? "").Append(';')
                .Append(Escape(view.SchoolName ?? "")).Append(';')
                .Append(vragenlijst).Append(';')
                .Append(r.Level).Append(';')
                .Append(r.Year).Append(';')
                .Append("level_year").Append(';')
                .Append("completed").Append(';')
                .Append(m || r.CompletedCount is null ? "< 5" : r.CompletedCount.Value.ToString())
                .AppendLine();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var name = $"scholen-rapportage-{view.SchoolYearStart}-{vragenlijst}.csv";
        return (bytes, name);
    }

    private async Task<List<SchoolClassAggregate>> QueryClassAggregatesAsync(
        SchoolReportFilterDto filter,
        CancellationToken cancellationToken)
    {
        var q = _db.SchoolClassAggregates.AsNoTracking()
            .Where(a => a.SchoolYearStart == filter.SchoolYearStart
                        && a.QuestionSet == filter.QuestionSet);
        if (filter.SchoolId is Guid sid)
        {
            q = q.Where(a => a.SchoolId == sid);
        }

        if (filter.Level is SchoolLevel level)
        {
            q = q.Where(a => a.Level == level);
        }

        if (filter.Year is int year)
        {
            q = q.Where(a => a.Year == year);
        }

        return await q.ToListAsync(cancellationToken);
    }

    private async Task<SchoolYearAggregate?> QueryYearAggregateAsync(
        SchoolReportFilterDto filter,
        CancellationToken cancellationToken)
    {
        // Prefer exact school year+test row; platform row when no school filter.
        return await _db.SchoolYearAggregates.AsNoTracking()
            .Where(a => a.SchoolYearStart == filter.SchoolYearStart
                        && a.SchoolId == filter.SchoolId
                        && a.QuestionSet == filter.QuestionSet)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private DateOnly TodayAmsterdam()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
            var local = TimeZoneInfo.ConvertTimeFromUtc(_clock.GetUtcNow().UtcDateTime, tz);
            return DateOnly.FromDateTime(local);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
        }
    }

    private static int? MaskInt(int? value, bool masked) => masked ? null : value;

    private static int? MaskInt(int value, bool masked) => masked ? null : value;

    private static IReadOnlyList<SchoolReportNamedCountDto> ToNamed(
        IReadOnlyDictionary<string, int> source,
        bool masked,
        int take)
    {
        return source
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(kv => new SchoolReportNamedCountDto(
                kv.Key,
                kv.Key,
                masked ? null : kv.Value,
                masked))
            .ToList();
    }

    private static Dictionary<string, int> MergeJson(IEnumerable<string> jsons)
        => ClassResultsAggregator.MergeCounts(jsons.Select(FromJson));

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

    private static string Escape(string value)
        => value.Contains(';') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
}
