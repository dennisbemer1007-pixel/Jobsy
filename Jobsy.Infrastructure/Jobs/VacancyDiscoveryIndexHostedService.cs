using Jobsy.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Keeps the banenkaart index warm. First rebuild runs immediately; then every 15 seconds
/// so newly published vacancies appear without waiting for a visitor to trigger a cold query.
/// </summary>
public sealed class VacancyDiscoveryIndexHostedService : BackgroundService
{
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(15);

    private readonly IVacancyDiscoveryIndex _index;
    private readonly ILogger<VacancyDiscoveryIndexHostedService> _logger;

    public VacancyDiscoveryIndexHostedService(
        IVacancyDiscoveryIndex index,
        ILogger<VacancyDiscoveryIndexHostedService> logger)
    {
        _index = index;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Index immediately so the first banenkaart open does not wait on a DB include-query.
        await RefreshSafeAsync(stoppingToken);

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

            await RefreshSafeAsync(stoppingToken);
        }
    }

    private async Task RefreshSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _index.RefreshAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Scheduled banenkaart index refresh failed.");
        }
    }
}
