using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Scholen;

/// <summary>
/// Hourly job: closes open test windows whose ClosesOn date has passed (Europe/Amsterdam).
/// Also invoked on-read via <see cref="ISchoolPortalService.EnsureTestWindowsClosedAsync"/>.
/// </summary>
public sealed class TestWindowAutoCloser : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeZoneInfo Amsterdam = ResolveAmsterdam();

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

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<TestWindowAutoCloser> _logger;

    public TestWindowAutoCloser(IServiceScopeFactory scopes, ILogger<TestWindowAutoCloser> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CloseExpiredAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "TestWindowAutoCloser failed");
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

    public async Task<int> CloseExpiredAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var snapshotter = scope.ServiceProvider.GetRequiredService<ISchoolAggregateSnapshotter>();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Amsterdam));
        var open = await db.SchoolClasses
            .Where(c => c.TestWindow == TestWindowState.Open
                        && c.TestWindowClosesOn != null
                        && c.TestWindowClosesOn < today)
            .ToListAsync(cancellationToken);
        if (open.Count == 0)
        {
            return 0;
        }

        foreach (var c in open)
        {
            c.TestWindow = TestWindowState.Closed;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var group in open.GroupBy(c => (c.SchoolId, c.SchoolYearStart)))
        {
            await snapshotter.SnapshotSchoolYearAsync(group.Key.SchoolId, group.Key.SchoolYearStart, cancellationToken);
        }

        _logger.LogInformation("Closed {Count} expired school test windows", open.Count);
        return open.Count;
    }
}
