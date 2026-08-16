using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>Polls Cursor for launched feedback agents when the webhook is missed.</summary>
public sealed class FeedbackAutomationPollHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeedbackAutomationPollHostedService> _logger;

    public FeedbackAutomationPollHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<FeedbackAutomationPollHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var feedback = scope.ServiceProvider.GetRequiredService<IFeedbackService>();
                var updated = await feedback.RefreshPendingAutomationsAsync(stoppingToken);
                if (updated > 0)
                {
                    _logger.LogInformation("Feedback automation poll updated {Count} items.", updated);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Feedback automation poll failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }
}
