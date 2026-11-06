using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Scholen;

public sealed class SchoolRetentionService : ISchoolRetentionService
{
    public const int MaxClassesPerTransaction = 50;
    public const string ConfirmDeleteYearPhrase = "VERWIJDER";

    private static readonly TimeZoneInfo Amsterdam = ResolveAmsterdam();

    private readonly JobsyDbContext _db;
    private readonly ISchoolAggregateSnapshotter _snapshotter;
    private readonly IPlatformFeatureService _features;
    private readonly TimeProvider _clock;
    private readonly ILogger<SchoolRetentionService> _logger;

    public SchoolRetentionService(
        JobsyDbContext db,
        ISchoolAggregateSnapshotter snapshotter,
        IPlatformFeatureService features,
        ILogger<SchoolRetentionService> logger,
        TimeProvider? clock = null)
    {
        _db = db;
        _snapshotter = snapshotter;
        _features = features;
        _logger = logger;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<SchoolRetentionDryRunResult> DryRunAsync(CancellationToken cancellationToken = default)
    {
        var (today, cutoffMonth, cutoffDay) = await TodayAndCutoffAsync(cancellationToken);
        var eligible = await LoadEligibleClassesAsync(today, cutoffMonth, cutoffDay, cancellationToken);
        var bySchool = eligible.GroupBy(c => c.SchoolId).ToList();
        var schoolNames = await _db.Schools.AsNoTracking()
            .Where(s => bySchool.Select(g => g.Key).Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);

        var schools = new List<SchoolRetentionDryRunSchool>();
        var totalClasses = 0;
        var totalCodes = 0;
        var totalResults = 0;
        foreach (var g in bySchool)
        {
            var classIds = g.Select(c => c.Id).ToList();
            var codes = await _db.PupilCodes.CountAsync(c => classIds.Contains(c.SchoolClassId), cancellationToken);
            var results = await _db.PupilResults.CountAsync(r => classIds.Contains(r.SchoolClassId), cancellationToken);
            totalClasses += classIds.Count;
            totalCodes += codes;
            totalResults += results;
            schools.Add(new SchoolRetentionDryRunSchool(
                g.Key,
                schoolNames.GetValueOrDefault(g.Key, g.Key.ToString("D")),
                classIds.Count,
                codes,
                results,
                g.Select(c => c.SchoolYearStart).Distinct().OrderBy(y => y).ToList()));
        }

        return new SchoolRetentionDryRunResult(
            today,
            SafeCutoff(today.Year, cutoffMonth, cutoffDay),
            totalClasses,
            totalCodes,
            totalResults,
            schools);
    }

    public async Task<SchoolRetentionRunResult> RunAsync(CancellationToken cancellationToken = default)
    {
        // Intentionally ignores SchoolsEnabled — retention always runs (D11 / 07.3).
        var (today, cutoffMonth, cutoffDay) = await TodayAndCutoffAsync(cancellationToken);
        var cutoffDate = SafeCutoff(today.Year, cutoffMonth, cutoffDay);
        var eligible = await LoadEligibleClassesAsync(today, cutoffMonth, cutoffDay, cancellationToken);
        var bySchool = eligible.GroupBy(c => c.SchoolId).ToList();

        var classesDeleted = 0;
        var codesDeleted = 0;
        var resultsDeleted = 0;
        var aggregatesWritten = 0;
        var failures = 0;

        foreach (var g in bySchool)
        {
            try
            {
                var years = g.Select(c => c.SchoolYearStart).Distinct().ToList();
                foreach (var year in years)
                {
                    aggregatesWritten += await _snapshotter.SnapshotSchoolYearAsync(g.Key, year, cancellationToken);
                }

                var classList = g.ToList();
                for (var i = 0; i < classList.Count; i += MaxClassesPerTransaction)
                {
                    var batch = classList.Skip(i).Take(MaxClassesPerTransaction).ToList();
                    var stats = await DeleteClassesTransactionAsync(batch.Select(c => c.Id).ToList(), cancellationToken);
                    classesDeleted += stats.Classes;
                    codesDeleted += stats.Codes;
                    resultsDeleted += stats.Results;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures++;
                _logger.LogError(ex, "School retention failed for school {SchoolId}", g.Key);
                Audit("school.retention.school_failed", g.Key, new { error = ex.GetType().Name });
            }
        }

        var outcome = failures == 0 ? "ok" : bySchool.Count == failures ? "failed" : "partial";
        var run = new SchoolRetentionRun
        {
            Id = Guid.NewGuid(),
            RanAtUtc = _clock.GetUtcNow().UtcDateTime,
            CutoffDate = cutoffDate,
            ClassesDeleted = classesDeleted,
            CodesDeleted = codesDeleted,
            ResultsDeleted = resultsDeleted,
            AggregatesWritten = aggregatesWritten,
            Outcome = outcome
        };
        _db.SchoolRetentionRuns.Add(run);
        Audit("school.retention.run", null, new
        {
            run.Id,
            run.CutoffDate,
            run.ClassesDeleted,
            run.CodesDeleted,
            run.ResultsDeleted,
            run.AggregatesWritten,
            run.Outcome,
            failures
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new SchoolRetentionRunResult(
            run.Id,
            run.CutoffDate,
            run.ClassesDeleted,
            run.CodesDeleted,
            run.ResultsDeleted,
            run.AggregatesWritten,
            run.Outcome);
    }

    public async Task<SchoolEarlyDeleteResult> DeleteSchoolYearNowAsync(
        Guid schoolId,
        int schoolYearStart,
        CancellationToken cancellationToken = default)
    {
        var classes = await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.SchoolId == schoolId && c.SchoolYearStart == schoolYearStart)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var aggregatesWritten = await _snapshotter.SnapshotSchoolYearAsync(schoolId, schoolYearStart, cancellationToken);
        var stats = await DeleteClassesTransactionAsync(classes, cancellationToken);
        Audit("school.year.delete", schoolId, new
        {
            schoolYearStart,
            stats.Classes,
            stats.Codes,
            stats.Results,
            aggregatesWritten
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new SchoolEarlyDeleteResult(stats.Classes, stats.Codes, stats.Results, aggregatesWritten);
    }

    public async Task<SchoolEarlyDeleteResult> DeleteSchoolNowAsync(
        Guid schoolId,
        string confirmName,
        CancellationToken cancellationToken = default)
    {
        var school = await _db.Schools.FirstOrDefaultAsync(s => s.Id == schoolId, cancellationToken)
                     ?? throw new InvalidOperationException("not_found");

        if (!string.Equals(confirmName?.Trim(), school.Name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("confirm_name_mismatch");
        }

        var aggregatesWritten = await _snapshotter.SnapshotAllYearsForSchoolAsync(schoolId, cancellationToken);
        var classIds = await _db.SchoolClasses.AsNoTracking()
            .Where(c => c.SchoolId == schoolId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        var stats = await DeleteClassesTransactionAsync(classIds, cancellationToken);

        var staff = await _db.Users
            .Where(u => u.SchoolId == schoolId
                        && (u.Role == UserRole.SchoolAdmin || u.Role == UserRole.Teacher))
            .ToListAsync(cancellationToken);
        var staffCount = staff.Count;
        _db.Users.RemoveRange(staff);

        // Pending invites
        var invites = await _db.SchoolStaffInvites.Where(i => i.SchoolId == schoolId).ToListAsync(cancellationToken);
        _db.SchoolStaffInvites.RemoveRange(invites);

        school.IsActive = false;
        Audit("school.delete", schoolId, new
        {
            school.Name,
            stats.Classes,
            stats.Codes,
            stats.Results,
            staffRemoved = staffCount,
            aggregatesWritten
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new SchoolEarlyDeleteResult(stats.Classes, stats.Codes, stats.Results, aggregatesWritten);
    }

    public async Task<int> CountClassesImpactedByCutoffAsync(
        int cutoffMonth,
        int cutoffDay,
        CancellationToken cancellationToken = default)
    {
        SchoolYear.ValidateCutoff(cutoffMonth, cutoffDay);
        var today = TodayAmsterdam();
        var eligible = await LoadEligibleClassesAsync(today, cutoffMonth, cutoffDay, cancellationToken);
        return eligible.Count;
    }

    private async Task<(DateOnly Today, int Month, int Day)> TodayAndCutoffAsync(CancellationToken cancellationToken)
    {
        var snap = await _features.GetAsync(cancellationToken);
        var month = snap.SchoolRetentionCutoffMonth;
        var day = snap.SchoolRetentionCutoffDay;
        try
        {
            SchoolYear.ValidateCutoff(month, day);
        }
        catch (ArgumentOutOfRangeException)
        {
            month = SchoolYear.DefaultCutoffMonth;
            day = SchoolYear.DefaultCutoffDay;
        }

        return (TodayAmsterdam(), month, day);
    }

    private async Task<List<SchoolClass>> LoadEligibleClassesAsync(
        DateOnly today,
        int cutoffMonth,
        int cutoffDay,
        CancellationToken cancellationToken)
    {
        var classes = await _db.SchoolClasses.AsNoTracking().ToListAsync(cancellationToken);
        return classes
            .Where(c => SchoolYear.EndsOn(c.SchoolYearStart, cutoffMonth, cutoffDay) < today)
            .ToList();
    }

    private async Task<(int Classes, int Codes, int Results)> DeleteClassesTransactionAsync(
        IReadOnlyList<Guid> classIds,
        CancellationToken cancellationToken)
    {
        if (classIds.Count == 0)
        {
            return (0, 0, 0);
        }

        // InMemory provider used in tests does not support transactions.
        var useTx = _db.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        if (useTx)
        {
            tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var results = await _db.PupilResults
                .Where(r => classIds.Contains(r.SchoolClassId))
                .ToListAsync(cancellationToken);
            var resultCount = results.Count;
            _db.PupilResults.RemoveRange(results);

            var codeIds = await _db.PupilCodes
                .Where(c => classIds.Contains(c.SchoolClassId))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            var progresses = await _db.PupilProgresses
                .Where(p => codeIds.Contains(p.PupilCodeId))
                .ToListAsync(cancellationToken);
            _db.PupilProgresses.RemoveRange(progresses);

            var codes = await _db.PupilCodes
                .Where(c => classIds.Contains(c.SchoolClassId))
                .ToListAsync(cancellationToken);
            var codeCount = codes.Count;
            _db.PupilCodes.RemoveRange(codes);

            var assignments = await _db.TeacherClassAssignments
                .Where(a => classIds.Contains(a.SchoolClassId))
                .ToListAsync(cancellationToken);
            _db.TeacherClassAssignments.RemoveRange(assignments);

            var schoolClasses = await _db.SchoolClasses
                .Where(c => classIds.Contains(c.Id))
                .ToListAsync(cancellationToken);
            var classCount = schoolClasses.Count;
            _db.SchoolClasses.RemoveRange(schoolClasses);

            await _db.SaveChangesAsync(cancellationToken);
            if (tx is not null)
            {
                await tx.CommitAsync(cancellationToken);
            }

            return (classCount, codeCount, resultCount);
        }
        catch
        {
            if (tx is not null)
            {
                await tx.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (tx is not null)
            {
                await tx.DisposeAsync();
            }
        }
    }

    private void Audit(string action, Guid? schoolId, object details)
    {
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Scholen.Audit",
            Message = schoolId is Guid id ? $"{action} school={id:D}" : action,
            DetailsJson = JsonSerializer.Serialize(details),
            CreatedAt = _clock.GetUtcNow().UtcDateTime
        });
    }

    private DateOnly TodayAmsterdam()
    {
        var utc = _clock.GetUtcNow().UtcDateTime;
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Amsterdam);
        return DateOnly.FromDateTime(local);
    }

    private static DateOnly SafeCutoff(int year, int month, int day)
    {
        var days = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Min(day, days));
    }

    private static TimeZoneInfo ResolveAmsterdam()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }
}
