using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>Drains <see cref="ICultureFitRefineQueue"/> and runs OpenAI culture-fit refine off the request path.</summary>
public sealed class CultureFitRefineWorker : BackgroundService
{
    private readonly ICultureFitRefineQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<CultureFitRefineWorker> _logger;

    public CultureFitRefineWorker(
        ICultureFitRefineQueue queue,
        IServiceScopeFactory scopes,
        ILogger<CultureFitRefineWorker> logger)
    {
        _queue = queue;
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid userId;
            Guid vacancyId;
            try
            {
                (userId, vacancyId) = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<ICandidateVacancyCultureFitService>();
                await service.RefineAsync(userId, vacancyId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Culture-fit refine failed for user {UserId} vacancy {VacancyId}.",
                    userId,
                    vacancyId);
            }
            finally
            {
                _queue.MarkCompleted(userId, vacancyId);
            }
        }
    }
}
