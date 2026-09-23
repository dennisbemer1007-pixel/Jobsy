using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>Marks ContactUnlock requests past the 48h window as RefundEligible.</summary>
public sealed class TalentContactRefundHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TalentContactRefundHostedService> _logger;

    public TalentContactRefundHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<TalentContactRefundHostedService> logger)
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
                using var scope = _scopeFactory.CreateScope();
                var talent = scope.ServiceProvider.GetRequiredService<ITalentPoolService>();
                var marked = await talent.MarkExpiredAsRefundEligibleAsync(stoppingToken);
                if (marked > 0)
                {
                    _logger.LogInformation("Marked {Count} talent contact requests as RefundEligible.", marked);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Talent contact refund eligibility sweep failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
