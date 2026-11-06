using Jobsy.Core.Scholen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Daily school-year retention (~03:00 Europe/Amsterdam). Always runs, even when SchoolsEnabled=false.
/// </summary>
public sealed class SchoolRetentionHostedService : BackgroundService
{
    private static readonly TimeZoneInfo Amsterdam = ResolveAmsterdam();
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(3);

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _clock;
    private readonly ILogger<SchoolRetentionHostedService> _logger;

    public SchoolRetentionHostedService(
        IServiceScopeFactory scopes,
        ILogger<SchoolRetentionHostedService> logger,
        TimeProvider? clock = null)
    {
        _scopes = scopes;
        _logger = logger;
        _clock = clock ?? TimeProvider.System;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var retention = scope.ServiceProvider.GetRequiredService<ISchoolRetentionService>();
                var result = await retention.RunAsync(stoppingToken);
                if (result.ClassesDeleted > 0 || result.AggregatesWritten > 0)
                {
                    _logger.LogInformation(
                        "School retention: classes={Classes}, codes={Codes}, results={Results}, aggregates={Aggregates}, outcome={Outcome}",
                        result.ClassesDeleted,
                        result.CodesDeleted,
                        result.ResultsDeleted,
                        result.AggregatesWritten,
                        result.Outcome);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "School retention job failed.");
            }

            try
            {
                await Task.Delay(DelayUntilNextRun(), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private TimeSpan DelayUntilNextRun()
    {
        var utcNow = _clock.GetUtcNow().UtcDateTime;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), Amsterdam);
        var nextLocal = new DateTime(localNow.Year, localNow.Month, localNow.Day, 3, 0, 0, DateTimeKind.Unspecified);
        if (localNow >= nextLocal)
        {
            nextLocal = nextLocal.AddDays(1);
        }

        var nextUtc = TimeZoneInfo.ConvertTimeToUtc(nextLocal, Amsterdam);
        var delay = nextUtc - utcNow;
        return delay < TimeSpan.FromMinutes(1) ? TimeSpan.FromHours(24) : delay;
    }

    private static TimeZoneInfo ResolveAmsterdam()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }
}
