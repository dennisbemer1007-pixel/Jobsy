using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

public sealed class WorkInitiativeAutoDeclineHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkInitiativeAutoDeclineHostedService> _logger;

    public WorkInitiativeAutoDeclineHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkInitiativeAutoDeclineHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<ICompanyMapInitiativeService>();
                var count = await service.AutoDeclineOverdueAsync(stoppingToken);
                if (count > 0)
                {
                    _logger.LogInformation("Auto-declined {Count} overdue work initiatives.", count);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Work initiative auto-decline job failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
