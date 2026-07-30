using System.Net;
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
/// Warns and deletes never-published Draft vacancies (30-day warning, 44-day delete).
/// Vacancies that were published at least once (PublishedAtUtc set) are never touched.
/// </summary>
public sealed class DraftVacancyCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DraftVacancyCleanupHostedService> _logger;

    public DraftVacancyCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<DraftVacancyCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Draft vacancy cleanup failed.");
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
        var now = DateTime.UtcNow;
        var warnBefore = now.AddDays(-DraftVacancyCleanupRules.WarningAfterDays);
        var deleteBefore = now.AddDays(-DraftVacancyCleanupRules.DeleteAfterDays);

        // Never-published drafts only.
        var toWarn = await db.Vacancies
            .Include(v => v.Company)
            .Where(v => v.Status == VacancyStatus.Draft
                        && v.PublishedAtUtc == null
                        && v.CreatedAtUtc <= warnBefore
                        && v.DraftCleanupWarningSentAtUtc == null)
            .OrderBy(v => v.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var warned = 0;
        var snap = await features.GetAsync(cancellationToken);
        var baseUrl = string.IsNullOrWhiteSpace(snap.PublicWebBaseUrl)
            ? "https://lobsy.nl"
            : snap.PublicWebBaseUrl.TrimEnd('/');

        foreach (var vacancy in toWarn)
        {
            var recipient = await ResolveCompanyEmailAsync(db, vacancy.CompanyId, cancellationToken);
            if (recipient is null)
            {
                vacancy.DraftCleanupWarningSentAtUtc = now; // avoid infinite retry without contact
                warned++;
                continue;
            }

            var deleteOn = vacancy.CreatedAtUtc.AddDays(DraftVacancyCleanupRules.DeleteAfterDays);
            await email.SendAsync(new EmailMessage(
                recipient,
                $"Concept-vacature '{vacancy.Title}' wordt over 14 dagen verwijderd",
                $"""
                 <p>Hallo,</p>
                 <p>Je concept-vacature <strong>{WebUtility.HtmlEncode(vacancy.Title)}</strong>
                 voor <strong>{WebUtility.HtmlEncode(vacancy.Company.Name)}</strong> staat al
                 {DraftVacancyCleanupRules.WarningAfterDays} dagen als concept en is nog nooit gepubliceerd.</p>
                 <p>Als je niets doet, ruimt Lobsy dit concept automatisch op op
                 <strong>{deleteOn:dd-MM-yyyy}</strong> (14 dagen vanaf deze mail).</p>
                 <p>Vacatures die je wél hebt gepubliceerd blijven altijd bewaard — ook na de deadline.</p>
                 <p><a href="{WebUtility.HtmlEncode(baseUrl)}/employer/vacancies"
                    style="display:inline-block;padding:0.75rem 1.25rem;background:#0b6e4f;color:#fff;text-decoration:none;border-radius:0.5rem;font-weight:600">
                    Publiceer of bewerk in Lobsy</a></p>
                 <p>Groetjes van de vrolijke kreeft 🦞<br/>Team Lobsy</p>
                 """,
                DraftVacancyCleanupRules.WarningEmailCategory), cancellationToken);

            vacancy.DraftCleanupWarningSentAtUtc = now;
            warned++;
        }

        if (warned > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        var toDelete = await db.Vacancies
            .Where(v => v.Status == VacancyStatus.Draft
                        && v.PublishedAtUtc == null
                        && v.CreatedAtUtc <= deleteBefore)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        var deleted = 0;
        if (toDelete.Count > 0)
        {
            deleted = await db.Vacancies
                .Where(v => toDelete.Contains(v.Id)
                            && v.Status == VacancyStatus.Draft
                            && v.PublishedAtUtc == null)
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (warned + deleted > 0)
        {
            _logger.LogInformation(
                "Draft cleanup: warned={Warned}, deleted={Deleted} (never-published drafts only).",
                warned, deleted);

            db.PlatformLogs.Add(new Core.Entities.PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "DraftVacancyCleanup",
                Message = $"Warned {warned}, deleted {deleted} never-published draft vacancies.",
                CreatedAt = now
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<string?> ResolveCompanyEmailAsync(
        JobsyDbContext db,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var company = await db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return null;
        }

        var orgId = company.ParentCompanyId ?? company.Id;
        var email = await db.Users.AsNoTracking()
            .Where(u => u.IsActive
                        && (u.CompanyId == orgId || u.CompanyId == companyId
                            || u.CompanyMemberships.Any(m => m.CompanyId == orgId || m.CompanyId == companyId))
                        && (u.Role == UserRole.EnterpriseManager || u.Role == UserRole.BranchManager || u.Role == UserRole.Admin))
            .OrderBy(u => u.Role == UserRole.EnterpriseManager ? 0 : u.Role == UserRole.BranchManager ? 1 : 2)
            .Select(u => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }
}
