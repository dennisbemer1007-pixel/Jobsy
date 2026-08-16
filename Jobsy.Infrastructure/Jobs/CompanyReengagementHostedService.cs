using System.Net;
using Jobsy.Core.Email;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Sends a one-time "We missen je" re-engagement e-mail to inactive organisations.
/// Hard stop: never more than once per company account unless an admin clears ReengagementEmailSentAtUtc.
/// </summary>
public sealed class CompanyReengagementHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CompanyReengagementHostedService> _logger;

    public CompanyReengagementHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<CompanyReengagementHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
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
                _logger.LogError(ex, "Company re-engagement job failed.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var features = scope.ServiceProvider.GetRequiredService<IPlatformFeatureService>();

        var snap = await features.GetAsync(cancellationToken);
        var inactiveDays = Math.Clamp(snap.InactiveCompanyDays, 30, 730);
        var cutoff = DateTime.UtcNow.AddDays(-inactiveDays);
        var baseUrl = string.IsNullOrWhiteSpace(snap.PublicWebBaseUrl)
            ? "https://lobsy.nl"
            : snap.PublicWebBaseUrl.TrimEnd('/');

        // Organisation roots only (parent null). Hard stop: never sent before.
        var candidates = await db.Companies.AsNoTracking()
            .Where(c => c.ParentCompanyId == null
                        && c.ReengagementEmailSentAtUtc == null
                        && c.Type == CompanyType.Employer)
            .Select(c => new { c.Id, c.Name, c.LastCsvImportAtUtc })
            .Take(500)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var org in candidates)
        {
            var orgIds = await db.Companies.AsNoTracking()
                .Where(c => c.Id == org.Id || c.ParentCompanyId == org.Id)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var hasActiveVacancy = await db.Vacancies.AsNoTracking()
                .AnyAsync(v => orgIds.Contains(v.CompanyId) && v.Status == VacancyStatus.Active, cancellationToken);
            if (hasActiveVacancy)
            {
                continue;
            }

            var lastLogin = await db.Users.AsNoTracking()
                .Where(u => u.IsActive
                            && (u.CompanyId != null && orgIds.Contains(u.CompanyId.Value)
                                || u.CompanyMemberships.Any(m => orgIds.Contains(m.CompanyId))))
                .Select(u => u.LastLoginAtUtc)
                .MaxAsync(cancellationToken);

            var lastApi = await db.ApiKeys.AsNoTracking()
                .Where(k => orgIds.Contains(k.CompanyId))
                .Select(k => k.LastUsedAt)
                .MaxAsync(cancellationToken);

            var lastCsv = await db.Companies.AsNoTracking()
                .Where(c => orgIds.Contains(c.Id))
                .Select(c => c.LastCsvImportAtUtc)
                .MaxAsync(cancellationToken);

            var lastVacancyCreate = await db.Vacancies.AsNoTracking()
                .Where(v => orgIds.Contains(v.CompanyId))
                .Select(v => (DateTime?)v.CreatedAtUtc)
                .MaxAsync(cancellationToken);

            var lastActivity = MaxDate(lastLogin, lastApi, lastCsv, lastVacancyCreate);
            if (lastActivity is null || lastActivity > cutoff)
            {
                // Never active enough history, or still recently active.
                if (lastActivity is null)
                {
                    // Brand-new orgs without any activity: skip until they had some signal then went cold.
                    continue;
                }

                continue;
            }

            var recipient = await ResolveOrgEmailAsync(db, org.Id, orgIds, cancellationToken);
            if (recipient is null)
            {
                continue;
            }

            var mail = TransactionalEmails.CompanyReEngagement(baseUrl, org.Name);
            await email.SendAsync(new EmailMessage(
                recipient,
                mail.Subject,
                mail.Html,
                mail.Category), cancellationToken);

            var tracked = await db.Companies.FirstAsync(c => c.Id == org.Id, cancellationToken);
            tracked.ReengagementEmailSentAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            sent++;
        }

        if (sent > 0)
        {
            _logger.LogInformation("Re-engagement: sent {Sent} one-time e-mails (inactive ≥ {Days} days).", sent, inactiveDays);
            db.PlatformLogs.Add(new Core.Entities.PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "CompanyReEngagement",
                Message = $"Sent {sent} re-engagement e-mails (inactive ≥ {inactiveDays} days).",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static DateTime? MaxDate(params DateTime?[] values)
    {
        DateTime? max = null;
        foreach (var v in values)
        {
            if (v is null)
            {
                continue;
            }

            if (max is null || v > max)
            {
                max = v;
            }
        }

        return max;
    }

    private static async Task<string?> ResolveOrgEmailAsync(
        JobsyDbContext db,
        Guid orgId,
        IReadOnlyList<Guid> orgIds,
        CancellationToken cancellationToken)
    {
        var email = await db.Users.AsNoTracking()
            .Where(u => u.IsActive
                        && (u.CompanyId != null && orgIds.Contains(u.CompanyId.Value)
                            || u.CompanyMemberships.Any(m => orgIds.Contains(m.CompanyId)))
                        && (u.Role == UserRole.EnterpriseManager || u.Role == UserRole.Admin || u.Role == UserRole.BranchManager))
            .OrderBy(u => u.Role == UserRole.EnterpriseManager ? 0 : u.Role == UserRole.Admin ? 1 : 2)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }
}
