using Jobsy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Daily (Europe/Amsterdam ~09:00) access-request reminder (working day 3),
/// escalation (working day 5) and expiry (calendar day 30).
/// </summary>
public sealed class AccessRequestEscalationHostedService : BackgroundService
{
    private static readonly TimeZoneInfo Dutch = ResolveDutch();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccessRequestEscalationHostedService> _logger;
    private readonly TimeProvider _clock;

    public AccessRequestEscalationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<AccessRequestEscalationHostedService> logger,
        TimeProvider clock)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _clock = clock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(4), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Access-request escalation job failed.");
            }

            await DelayUntilNextAmsterdamNineAsync(stoppingToken);
        }
    }

    internal async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var access = scope.ServiceProvider.GetRequiredService<ICompanyAccessRequestService>();
        var now = _clock.GetUtcNow().UtcDateTime;
        var (reminders, escalations, expiries) = await access.ProcessEscalationsAsync(now, cancellationToken);
        if (reminders + escalations + expiries > 0)
        {
            _logger.LogInformation(
                "Access-request job: reminders={Reminders}, escalations={Escalations}, expiries={Expiries}",
                reminders,
                escalations,
                expiries);
        }
    }

    private async Task DelayUntilNextAmsterdamNineAsync(CancellationToken stoppingToken)
    {
        var utcNow = _clock.GetUtcNow().UtcDateTime;
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, Dutch);
        var nextLocal = new DateTime(local.Year, local.Month, local.Day, 9, 0, 0, DateTimeKind.Unspecified);
        if (local >= nextLocal)
        {
            nextLocal = nextLocal.AddDays(1);
        }

        var nextUtc = TimeZoneInfo.ConvertTimeToUtc(nextLocal, Dutch);
        var delay = nextUtc - utcNow;
        if (delay < TimeSpan.FromMinutes(30))
        {
            delay = TimeSpan.FromHours(12);
        }

        await Task.Delay(delay, stoppingToken);
    }

    private static TimeZoneInfo ResolveDutch()
    {
        foreach (var id in new[] { "Europe/Amsterdam", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
