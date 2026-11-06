using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// One-off: a deep compass without a completed deep career test is cleared
/// so the basic page does not keep the old "Jij bent het sterkst in…" text.
/// </summary>
public sealed class CareerCompassDeepResetHostedService : BackgroundService
{
    public const string MarkerCategory = "career.compass.deep-reset";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CareerCompassDeepResetHostedService> _logger;

    public CareerCompassDeepResetHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<CareerCompassDeepResetHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var cleared = await RunOnceAsync(db, stoppingToken);
            if (cleared >= 0)
            {
                _logger.LogInformation("Deep compass reset cleared {Count} rows.", cleared);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Deep compass reset failed.");
        }
    }

    public static async Task<int> RunOnceAsync(JobsyDbContext db, CancellationToken cancellationToken = default)
    {
        var already = await db.PlatformLogs.AsNoTracking()
            .AnyAsync(l => l.Category == MarkerCategory, cancellationToken);
        if (already)
        {
            return -1;
        }

        var completed = await db.CandidateDeepAnalyses.AsNoTracking()
            .Where(d => d.Kind == AssessmentKind.Career && d.Status == CandidateDeepAnalysisStatuses.Completed)
            .Select(d => d.UserId)
            .ToListAsync(cancellationToken);
        var done = completed.ToHashSet();
        var rows = await db.CandidateCareerInterests
            .Where(c => c.CompassJson.Contains("fromDeepAnalysis"))
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var cleared = 0;
        foreach (var row in rows)
        {
            if (done.Contains(row.UserId))
            {
                continue;
            }

            if (CareerInterestDeepReset.ClearDeepCompass(row, now))
            {
                cleared++;
            }
        }

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = MarkerCategory,
            Message = "Stale deep compass reset completed",
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
        return cleared;
    }
}
