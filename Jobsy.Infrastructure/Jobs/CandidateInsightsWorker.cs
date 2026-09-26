using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>Drains <see cref="ICandidateInsightsQueue"/> and recomputes derived insights per user.</summary>
public sealed class CandidateInsightsWorker : BackgroundService
{
    private readonly ICandidateInsightsQueue _queue;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<CandidateInsightsWorker> _logger;

    public CandidateInsightsWorker(
        ICandidateInsightsQueue queue,
        IServiceScopeFactory scopes,
        ILogger<CandidateInsightsWorker> logger)
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
            try
            {
                userId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var computer = scope.ServiceProvider.GetRequiredService<ICandidateInsightsComputer>();
                await computer.RecomputeAsync(userId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Candidate insights recompute failed for {UserId}.", userId);
            }
            finally
            {
                _queue.MarkCompleted(userId);
            }
        }
    }
}
