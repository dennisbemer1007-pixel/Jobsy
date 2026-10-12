using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>Drains instant proactive matching jobs off the publish request path.</summary>
public sealed class ProactiveVacancyInstantMatchWorker : BackgroundService
{
    private readonly IProactiveVacancyInstantMatchQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ProactiveVacancyInstantMatchWorker> _logger;

    public ProactiveVacancyInstantMatchWorker(
        IProactiveVacancyInstantMatchQueue queue,
        IServiceScopeFactory scopes,
        ILogger<ProactiveVacancyInstantMatchWorker> logger)
    {
        _queue = queue;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid vacancyId;
            try
            {
                vacancyId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var matching = scope.ServiceProvider.GetRequiredService<IProactiveMatchingService>();
                await matching.RunInstantMatchingForVacancyAsync(vacancyId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Instant proactive matching failed for vacancy {VacancyId}.",
                    vacancyId);
            }
            finally
            {
                _queue.MarkCompleted(vacancyId);
            }
        }
    }
}
