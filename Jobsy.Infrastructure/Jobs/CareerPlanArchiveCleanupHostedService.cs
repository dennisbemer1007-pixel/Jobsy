using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Hard-deletes archived career plans (and their step progress) older than 30 days (§2, D11).
/// Runs every 6 hours, batched.
/// </summary>
public sealed class CareerPlanArchiveCleanupHostedService : BackgroundService
{
    private const int RetentionDays = 30;
    private const int BatchSize = 200;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CareerPlanArchiveCleanupHostedService> _logger;

    public CareerPlanArchiveCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<CareerPlanArchiveCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Career plan archive cleanup failed.");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

        var expiredIds = await db.CandidateCareerPlans
            .Where(p => p.Status == CareerPlanStatuses.Archived
                        && p.ArchivedAtUtc != null
                        && p.ArchivedAtUtc < cutoff)
            .OrderBy(p => p.ArchivedAtUtc)
            .Take(BatchSize)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (expiredIds.Count == 0)
        {
            return;
        }

        await db.CandidateCareerStepProgress
            .Where(s => expiredIds.Contains(s.PlanId))
            .ExecuteDeleteAsync(cancellationToken);

        var deleted = await db.CandidateCareerPlans
            .Where(p => expiredIds.Contains(p.Id))
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            _logger.LogInformation("Career plan archive cleanup: deleted {Deleted} plans older than {RetentionDays} days.", deleted, RetentionDays);
        }
    }
}
