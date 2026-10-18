using Jobsy.Core.Features;
using Jobsy.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>Sends at most one reminder per pending work-experience reference request.</summary>
public sealed class ReferenceVerificationReminderHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReferenceVerificationReminderHostedService> _logger;

    public ReferenceVerificationReminderHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReferenceVerificationReminderHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(11), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var flags = scope.ServiceProvider.GetRequiredService<IFeatureFlags>();
                if (await flags.IsEnabledAsync(PlatformFeature.ReferenceVerification, stoppingToken))
                {
                    var verification = scope.ServiceProvider.GetRequiredService<ReferenceVerificationService>();
                    var sent = await verification.SendDueRemindersAsync(stoppingToken);
                    if (sent > 0)
                    {
                        _logger.LogInformation("Sent {Count} work-experience reference reminders.", sent);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Work-experience reference reminder job failed.");
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }
}
