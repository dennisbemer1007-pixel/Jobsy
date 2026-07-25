using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Jobs;

/// <summary>
/// Stub hosted job: on the 1st of January and July (Europe/Amsterdam), logs that a WML update is due.
/// Actual rate import remains a manual admin action (POST api/wages/semi-annual-update).
/// </summary>
public sealed class MinimumWageUpdateHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MinimumWageUpdateHostedService> _logger;

    public MinimumWageUpdateHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<MinimumWageUpdateHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "WML update stub check failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CheckAsync(CancellationToken ct)
    {
        var today = DutchToday();
        if (today.Day != 1 || today.Month is not (1 or 7))
        {
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();

        var marker = $"due {today:yyyy-MM-dd}";
        var alreadyLogged = await db.PlatformLogs.AsNoTracking().AnyAsync(
            l => l.Category == "Wages" && l.Message.Contains(marker),
            ct);
        if (alreadyLogged)
        {
            return;
        }

        db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "Wages",
            Message = $"Semi-annual WML update due {today:yyyy-MM-dd} (stub — run POST api/wages/semi-annual-update).",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("WML semi-annual update reminder logged for {Date}.", today);
    }

    public static DateOnly DutchToday(DateTime? utcNow = null)
    {
        var utc = utcNow ?? DateTime.UtcNow;
        var tz = ResolveDutchTimeZone();
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);
        return DateOnly.FromDateTime(local);
    }

    private static TimeZoneInfo ResolveDutchTimeZone()
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
