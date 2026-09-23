using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>Weekly ATS URL/content health + 30-day TTL enforcement.</summary>
public sealed class AtsVacancyHealthHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AtsVacancyHealthHostedService> _logger;

    public AtsVacancyHealthHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<AtsVacancyHealthHostedService> logger)
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
                await using var scope = _scopeFactory.CreateAsyncScope();
                var health = scope.ServiceProvider.GetRequiredService<IAtsVacancyHealthService>();
                var n = await health.RunHealthPassAsync(stoppingToken);
                _logger.LogInformation("ATS health cycle finished; changed={Count}.", n);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "ATS health cycle failed.");
            }

            await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
        }
    }
}
