using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Keeps the banenkaart index warm. First rebuild runs immediately; then every 60 seconds
/// so newly published vacancies appear without waiting for a visitor to trigger a cold query.
/// Writes still call <see cref="IVacancyDiscoveryIndex.Invalidate"/> immediately.
/// </summary>
public sealed class VacancyDiscoveryIndexHostedService : BackgroundService
{
    public static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(60);

    private readonly EmployersJobGate _employersGate;
    private readonly IVacancyDiscoveryIndex _index;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VacancyDiscoveryIndexHostedService> _logger;

    public VacancyDiscoveryIndexHostedService(
        IVacancyDiscoveryIndex index,
        IServiceScopeFactory scopeFactory,
        ILogger<VacancyDiscoveryIndexHostedService> logger)
    {
        _index = index;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _employersGate = new EmployersJobGate(_logger, nameof(VacancyDiscoveryIndexHostedService));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
            if (!await _employersGate.ShouldRunAsync(_scopeFactory, cancellationToken))
            {
                return;
            }

            await _index.RefreshAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Scheduled banenkaart index refresh failed.");
        }
    }
}
