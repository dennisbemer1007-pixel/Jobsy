using Jobsy.Core.Entities;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Generation guard for career-plan AI/local builds (D10, §6). Scoped so one request sees one
/// consistent snapshot of the in-flight / idempotent / daily-limit windows.
/// </summary>
public sealed class CareerGenerationGuard
{
    /// <summary>Admin setting later; a flagged constant for now (D10).</summary>
    public const int DailyLimit = 5;

    private static readonly TimeSpan InFlightWindow = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan IdempotentWindow = TimeSpan.FromMinutes(10);

    private readonly JobsyDbContext _db;

    public CareerGenerationGuard(JobsyDbContext db)
    {
        _db = db;
    }

    public sealed record CheckResult(bool Allowed, bool Reused, string? Code, DateTime? RetryAfterUtc);

    /// <summary>
    /// Checks in-flight / idempotent / daily-limit before a generation starts.
    /// <paramref name="activePlanDreamKey"/>/<paramref name="activePlanCreatedAtUtc"/> come from the
    /// candidate's current active plan (if any) for the idempotency check.
    /// </summary>
    public async Task<CheckResult> CheckAsync(
        Guid userId,
        string dreamKey,
        string? activePlanDreamKey,
        DateTime? activePlanCreatedAtUtc,
        bool force,
        bool enforceDailyLimit,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var inFlightSince = now - InFlightWindow;
        var inFlight = await _db.CandidateCareerGenerations.AsNoTracking()
            .AnyAsync(
                g => g.UserId == userId && g.FinishedAtUtc == null && g.StartedAtUtc >= inFlightSince,
                cancellationToken);
        if (inFlight)
        {
            return new CheckResult(Allowed: false, Reused: false, CareerPlanErrorCodes.GenerationInProgress, null);
        }

        if (!force
            && activePlanDreamKey is not null
            && activePlanCreatedAtUtc is not null
            && string.Equals(activePlanDreamKey, dreamKey, StringComparison.Ordinal)
            && activePlanCreatedAtUtc.Value >= now - IdempotentWindow)
        {
            return new CheckResult(Allowed: true, Reused: true, null, null);
        }

        if (enforceDailyLimit)
        {
            var since = now.AddHours(-24);
            var recent = await _db.CandidateCareerGenerations.AsNoTracking()
                .Where(g => g.UserId == userId
                            && g.StartedAtUtc >= since
                            && (g.Outcome == CareerGenerationOutcomes.Ok || g.Outcome == CareerGenerationOutcomes.Failed))
                .OrderBy(g => g.StartedAtUtc)
                .Select(g => g.StartedAtUtc)
                .ToListAsync(cancellationToken);
            if (recent.Count >= DailyLimit)
            {
                var retryAfter = recent[0].AddHours(24);
                return new CheckResult(Allowed: false, Reused: false, CareerPlanErrorCodes.GenerationLimit, retryAfter);
            }
        }

        return new CheckResult(Allowed: true, Reused: false, null, null);
    }

    public async Task<CandidateCareerGeneration> BeginAsync(
        Guid userId,
        string dreamKey,
        CancellationToken cancellationToken = default)
    {
        var row = new CandidateCareerGeneration
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DreamKey = dreamKey,
            StartedAtUtc = DateTime.UtcNow,
            Outcome = CareerGenerationOutcomes.Ok
        };
        _db.CandidateCareerGenerations.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task CompleteAsync(
        CandidateCareerGeneration row,
        string outcome,
        CancellationToken cancellationToken = default)
    {
        row.FinishedAtUtc = DateTime.UtcNow;
        row.Outcome = outcome;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
