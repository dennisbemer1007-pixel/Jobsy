using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Recomputes recently used dashboard snapshots every 10 minutes so heavy aggregations stay warm.
/// Live operational fields are still overlaid on read.
/// Employer/sales/ambassadeur kinds pause when Employers is OFF; candidate metrics are elsewhere.
/// </summary>
public sealed class DashboardCacheRefreshHostedService : BackgroundService
{
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(10);

    private readonly EmployersJobGate _employersGate;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DashboardCacheRefreshHostedService> _logger;

    public DashboardCacheRefreshHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<DashboardCacheRefreshHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _employersGate = new EmployersJobGate(_logger, nameof(DashboardCacheRefreshHostedService));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(RefreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RefreshTrackedAsync(stoppingToken);
        }
    }

    private async Task RefreshTrackedAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var cache = scope.ServiceProvider.GetRequiredService<IDashboardCache>();
            var tracked = cache.GetTracked(TimeSpan.FromMinutes(30));
            if (tracked.Count == 0)
            {
                return;
            }

            var metrics = scope.ServiceProvider.GetRequiredService<IMetricsQueryService>();
            var sales = scope.ServiceProvider.GetRequiredService<ISalesManagerDashboardService>();
            var ambassadeurs = scope.ServiceProvider.GetRequiredService<IAmbassadeurDashboardService>();
            var employersOn = await _employersGate.ShouldRunAsync(scope.ServiceProvider, cancellationToken);

            foreach (var (key, descriptor) in tracked)
            {
                if (!employersOn && IsEmployerDashboard(descriptor.Kind))
                {
                    continue;
                }

                cache.Remove(key);
                try
                {
                    await RecomputeAsync(descriptor, metrics, sales, ambassadeurs, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Dashboard cache refresh failed for {CacheKey}.", key);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Scheduled dashboard cache refresh failed.");
        }
    }

    private static bool IsEmployerDashboard(DashboardCacheKind kind) => kind is
        DashboardCacheKind.MetricsSummary
        or DashboardCacheKind.VacancyPerformance
        or DashboardCacheKind.ClientPerformance
        or DashboardCacheKind.SalesDashboard
        or DashboardCacheKind.AmbassadeurDashboard;

    private static Task RecomputeAsync(
        DashboardCacheDescriptor descriptor,
        IMetricsQueryService metrics,
        ISalesManagerDashboardService sales,
        IAmbassadeurDashboardService ambassadeurs,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Guid>? companyIds = descriptor.CompanyIds;
        return descriptor.Kind switch
        {
            DashboardCacheKind.MetricsSummary => metrics
                .GetSummaryAsync(descriptor.IncludePlatformOnly, companyIds, descriptor.Period, cancellationToken)
                .AsTask(),
            DashboardCacheKind.VacancyPerformance => metrics
                .GetVacancyPerformanceAsync(companyIds, descriptor.Period, Math.Max(1, descriptor.Take), cancellationToken)
                .AsTask(),
            DashboardCacheKind.ClientPerformance => metrics
                .GetClientPerformanceAsync(companyIds, descriptor.Period, cancellationToken)
                .AsTask(),
            DashboardCacheKind.SalesDashboard when descriptor.UserId is Guid salesId => sales
                .GetDashboardAsync(salesId, cancellationToken)
                .AsTask(),
            DashboardCacheKind.AmbassadeurDashboard when descriptor.UserId is Guid ambassadeurId => ambassadeurs
                .GetDashboardAsync(ambassadeurId, cancellationToken)
                .AsTask(),
            _ => Task.CompletedTask
        };
    }
}

file static class DashboardCacheTaskExtensions
{
    public static async Task AsTask<T>(this Task<T> task) => await task;
}
