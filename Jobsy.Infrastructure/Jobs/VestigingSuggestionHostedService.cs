using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Weekly scan: verified Organisation-scope roots vs KVK vestigingen → Te doen suggestions (11.5 / D7).
/// </summary>
public sealed class VestigingSuggestionHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VestigingSuggestionHostedService> _logger;

    public VestigingSuggestionHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<VestigingSuggestionHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(8), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IVestigingSuggestionService>();
                var found = await svc.RefreshSuggestionsAsync(stoppingToken);
                if (found > 0)
                {
                    _logger.LogInformation("Vestiging suggestion scan found {Count} open suggestions.", found);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Vestiging suggestion job failed.");
            }

            await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
        }
    }
}
