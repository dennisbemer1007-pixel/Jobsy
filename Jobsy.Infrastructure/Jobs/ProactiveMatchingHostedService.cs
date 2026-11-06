using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>Daily proactive matching batch (Lobsy zoekt voor je).</summary>
public sealed class ProactiveMatchingHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProactiveMatchingHostedService> _logger;

    public ProactiveMatchingHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ProactiveMatchingHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(12), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Proactive matching job failed.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var flags = scope.ServiceProvider.GetRequiredService<IFeatureFlags>();
        if (!await flags.IsEnabledAsync(PlatformFeature.ProactiveMatching, cancellationToken))
        {
            return;
        }

        var matching = scope.ServiceProvider.GetRequiredService<IProactiveMatchingService>();
        await matching.RunDailyMatchingAsync(cancellationToken);
    }
}
