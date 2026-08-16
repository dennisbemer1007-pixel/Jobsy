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
/// After 14 days Active, e-mails + notifies the entrepreneur with engagement stats and an AI tip.
/// </summary>
public sealed class VacancyEngagementReminderHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VacancyEngagementReminderHostedService> _logger;

    public VacancyEngagementReminderHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<VacancyEngagementReminderHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(6), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Vacancy engagement reminder job failed.");
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var notifications = scope.ServiceProvider.GetRequiredService<IUserNotificationService>();
        var features = scope.ServiceProvider.GetRequiredService<IPlatformFeatureService>();

        var snap = await features.GetAsync(cancellationToken);
        var baseUrl = string.IsNullOrWhiteSpace(snap.PublicWebBaseUrl)
            ? "https://lobsy.nl"
            : snap.PublicWebBaseUrl;

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-VacancyEngagementReminderRules.OpenDaysBeforeReminder);

        var vacancies = await db.Vacancies
            .Include(v => v.Company)
            .Where(v => v.Status == VacancyStatus.Active
                        && v.EngagementReminderSentAtUtc == null
                        && v.PublishedAtUtc != null
                        && v.PublishedAtUtc <= cutoff)
            .OrderBy(v => v.PublishedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var vacancy in vacancies)
        {
            var vacancyId = vacancy.Id;
            var impressions = await db.VacancySearchImpressions.AsNoTracking()
                .CountAsync(i => i.VacancyId == vacancyId, cancellationToken);
            var views = await db.VacancyClicks.AsNoTracking()
                .CountAsync(c => c.VacancyId == vacancyId, cancellationToken);
            var shares = await db.VacancyShares.AsNoTracking()
                .CountAsync(s => s.VacancyId == vacancyId, cancellationToken);
            var saved = await db.VacancyLikes.AsNoTracking()
                .CountAsync(l => l.VacancyId == vacancyId, cancellationToken);
            var applications = await db.Applications.AsNoTracking()
                .CountAsync(
                    a => a.VacancyId == vacancyId
                         && a.EmailVerifiedAt != null
                         && a.Status != ApplicationStatus.Withdrawn,
                    cancellationToken);

            var tip = VacancyEngagementReminderRules.BuildHeuristicTip(
                impressions, views, shares, saved, applications);

            vacancy.EngagementReminderTip = tip.Length <= 2000 ? tip : tip[..2000];
            vacancy.EngagementReminderSentAtUtc = now;

            var mail = TransactionalEmails.VacancyEngagementReminder(
                baseUrl,
                vacancy.Title,
                vacancy.Id,
                impressions,
                views,
                shares,
                saved,
                applications,
                tip,
                vacancy.Company.Name);
            var bodyHtml = mail.Html;

            var notifyTitle = mail.Subject;
            var notifyBody =
                $"Zoek: {impressions} · bekeken: {views} · gedeeld: {shares} · bewaard: {saved} · sollicitaties: {applications}. Tip: {tip}";

            var contacts = await db.Users.AsNoTracking()
                .Where(u => u.IsActive
                            && u.Role != UserRole.Candidate
                            && (u.CompanyId == vacancy.CompanyId
                                || u.CompanyMemberships.Any(m => m.CompanyId == vacancy.CompanyId)
                                || (vacancy.Company.ParentCompanyId != null
                                    && (u.CompanyId == vacancy.Company.ParentCompanyId
                                        || u.CompanyMemberships.Any(m => m.CompanyId == vacancy.Company.ParentCompanyId.Value)))))
                .Select(u => new { u.Id, u.Email })
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var contact in contacts)
            {
                await email.SendAsync(new EmailMessage(
                    contact.Email,
                    notifyTitle,
                    bodyHtml,
                    "VacancyEngagementReminder"), cancellationToken);

                await notifications.CreateAsync(
                    new NotificationCreateRequest(
                        contact.Id,
                        notifyTitle,
                        notifyBody,
                        "VacatureEngagementReminder",
                        $"/branch/vacancies/new?edit={vacancy.Id}",
                        "Vacature nu verbeteren",
                        $"/branch/vacancies/new?edit={vacancy.Id}",
                        "Vacancy",
                        vacancy.Id),
                    cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
            sent++;
        }

        if (sent > 0)
        {
            _logger.LogInformation("Vacancy engagement reminders sent: {Sent}", sent);
            db.PlatformLogs.Add(new Core.Entities.PlatformLog
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Level = PlatformLogLevel.Info,
                Category = "VacancyEngagementReminder",
                Message = $"Sent {sent} engagement reminder(s).",
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
