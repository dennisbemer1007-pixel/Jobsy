using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Creates the monthly sales payout run on the 1st workday at ≥ 06:00 Europe/Amsterdam.
/// Wakes every 15 minutes; unique index makes multi-instance safe.
/// </summary>
public sealed class SalesPayoutRunHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SalesPayoutRunHostedService> _logger;

    public SalesPayoutRunHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SalesPayoutRunHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TryCreateAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Sales payout run job failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    /// <summary>Test hook: evaluate whether a scheduled run should be created for <paramref name="utcNow"/>.</summary>
    public static async Task<bool> TryCreateForClockAsync(
        ISalesPayoutRunService runs,
        JobsyDbContext db,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var local = SalesClock.ToLocal(utcNow);
        var today = DateOnly.FromDateTime(local.DateTime);
        var first = SalesClock.FirstWorkdayOfMonth(today.Year, today.Month);
        if (today != first)
        {
            return false;
        }

        if (local.TimeOfDay < TimeSpan.FromHours(6))
        {
            return false;
        }

        var exists = await db.SalesPayoutRuns.AsNoTracking()
            .AnyAsync(r => r.RunDate == first && !r.IsExtra, cancellationToken);
        if (exists)
        {
            return false;
        }

        var created = await runs.TryCreateScheduledRunAsync(first, utcNow, cancellationToken);
        return created is not null;
    }

    private async Task TryCreateAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var runs = scope.ServiceProvider.GetRequiredService<ISalesPayoutRunService>();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var created = await TryCreateForClockAsync(runs, db, DateTime.UtcNow, cancellationToken);
        if (created)
        {
            _logger.LogInformation("Created scheduled sales payout run for {Date}.", SalesClock.Today());
        }
    }
}
