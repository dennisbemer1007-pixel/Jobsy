using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Retries KVK verification for companies registered while the KVK API was unavailable.
/// </summary>
public sealed class KvkVerificationRetryHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KvkVerificationRetryHostedService> _logger;

    public KvkVerificationRetryHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<KvkVerificationRetryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var retry = scope.ServiceProvider.GetRequiredService<IKvkVerificationRetryService>();
                var verified = await retry.RetryPendingAsync(stoppingToken);
                if (verified > 0)
                {
                    _logger.LogInformation("KVK retry job verified {Count} companies.", verified);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "KVK verification retry job failed.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
