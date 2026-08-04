using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Hard-deletes company/intermediary registrations that were not confirmed within the OTP window.
/// </summary>
public sealed class UnconfirmedRegistrationCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UnconfirmedRegistrationCleanupHostedService> _logger;

    public UnconfirmedRegistrationCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<UnconfirmedRegistrationCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var registration = scope.ServiceProvider.GetRequiredService<ICompanyRegistrationService>();
                var removed = await registration.PurgeExpiredUnconfirmedAsync(stoppingToken);
                if (removed > 0)
                {
                    _logger.LogInformation(
                        "Purged {Count} unconfirmed company registration(s).",
                        removed);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unconfirmed registration cleanup failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
