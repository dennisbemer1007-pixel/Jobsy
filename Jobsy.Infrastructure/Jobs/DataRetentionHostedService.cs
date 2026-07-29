using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Purges aged platform logs, cancelled registrations, old engagement events,
/// and unverified application drafts (AVG retention).
/// </summary>
public sealed class DataRetentionHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataRetentionHostedService> _logger;

    public DataRetentionHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<DataRetentionHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay first run so startup/seeding can finish.
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPurgeAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Data retention purge failed.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunPurgeAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var now = DateTime.UtcNow;

        var logCutoff = now.AddDays(-PrivacyConstants.PlatformLogRetentionDays);
        var logsRemoved = await db.PlatformLogs
            .Where(l => l.CreatedAt < logCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var regCutoff = now.AddDays(-PrivacyConstants.CancelledRegistrationRetentionDays);
        var regsRemoved = await db.CompanyRegistrations
            .Where(r => r.Status == CompanyRegistrationStatus.Cancelled && r.CreatedAt < regCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var engagementCutoff = now.AddDays(-PrivacyConstants.EngagementEventRetentionDays);
        var clicksRemoved = await db.VacancyClicks
            .Where(c => c.CreatedAt < engagementCutoff)
            .ExecuteDeleteAsync(cancellationToken);
        var sharesRemoved = await db.VacancyShares
            .Where(s => s.CreatedAt < engagementCutoff)
            .ExecuteDeleteAsync(cancellationToken);
        var impressionsRemoved = await db.VacancySearchImpressions
            .Where(i => i.CreatedAt < engagementCutoff)
            .ExecuteDeleteAsync(cancellationToken);
        var visitsRemoved = await db.SiteVisits
            .Where(v => v.CreatedAt < engagementCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        var draftCutoff = now.AddHours(-PrivacyConstants.UnverifiedApplicationRetentionHours);
        var unverifiedAppsRemoved = await db.Applications
            .Where(a => a.EmailVerifiedAt == null && a.CreatedAt < draftCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (logsRemoved + regsRemoved + clicksRemoved + sharesRemoved + impressionsRemoved + visitsRemoved
            + unverifiedAppsRemoved > 0)
        {
            _logger.LogInformation(
                "Retention purge: logs={Logs}, registrations={Regs}, clicks={Clicks}, shares={Shares}, impressions={Impressions}, visits={Visits}, unverifiedApps={UnverifiedApps}",
                logsRemoved, regsRemoved, clicksRemoved, sharesRemoved, impressionsRemoved, visitsRemoved,
                unverifiedAppsRemoved);
        }
    }
}
