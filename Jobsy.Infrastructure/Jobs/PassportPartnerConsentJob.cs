using Jobsy.Core.Email;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

public sealed class PassportPartnerConsentJob
{
    private readonly JobsyDbContext _db;
    private readonly ITransactionalMailer _mailer;
    private readonly IPlatformFeatureService _features;

    public PassportPartnerConsentJob(
        JobsyDbContext db,
        ITransactionalMailer mailer,
        IPlatformFeatureService features)
    {
        _db = db;
        _mailer = mailer;
        _features = features;
    }

    public async Task RunAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var features = await _features.GetAsync(cancellationToken);
        if (features.PassportPartnersEnabled)
        {
            var reminderUntil = utcNow.Add(PassportPartnerRules.ReconfirmReminderLead);
            var due = await _db.PassportPartnerCandidateLinks
                .Include(l => l.PassportPartner)
                .Where(l => l.ConsentGivenAtUtc != null
                            && l.RevokedAtUtc == null
                            && l.SuspendedAtUtc == null
                            && l.ReconfirmReminderSentAtUtc == null
                            && l.ReconfirmDueAtUtc != null
                            && l.ReconfirmDueAtUtc > utcNow
                            && l.ReconfirmDueAtUtc <= reminderUntil)
                .ToListAsync(cancellationToken);

            if (due.Count > 0)
            {
                var userIds = due.Select(l => l.CandidateUserId).Distinct().ToList();
                var users = await _db.Users.Where(u => userIds.Contains(u.Id) && u.IsActive).ToListAsync(cancellationToken);
                foreach (var link in due)
                {
                    var user = users.FirstOrDefault(u => u.Id == link.CandidateUserId);
                    if (user is null || string.IsNullOrWhiteSpace(user.Email) || link.PassportPartner is null)
                    {
                        continue;
                    }

                    var mail = TransactionalEmails.PartnerConsentReconfirmReminder(
                        features.PublicWebBaseUrl,
                        user.FirstName ?? user.FullName,
                        link.PassportPartner.DisplayName);
                    await _mailer.SendAsync(mail, user.Email, cancellationToken: cancellationToken);
                    link.ReconfirmReminderSentAtUtc = utcNow;
                }
            }

            var suspend = await _db.PassportPartnerCandidateLinks
                .Where(l => l.ConsentGivenAtUtc != null
                            && l.RevokedAtUtc == null
                            && l.SuspendedAtUtc == null
                            && l.ReconfirmDueAtUtc != null
                            && l.ReconfirmDueAtUtc <= utcNow)
                .ToListAsync(cancellationToken);
            foreach (var link in suspend)
            {
                link.SuspendedAtUtc = utcNow;
            }
        }

        var cutoff = utcNow.Add(-PassportPartnerRules.AccessLogRetention);
        var oldLogs = await _db.PassportAccessLogs
            .Where(l => l.OccurredAtUtc < cutoff)
            .ToListAsync(cancellationToken);
        if (oldLogs.Count > 0)
        {
            _db.PassportAccessLogs.RemoveRange(oldLogs);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class PassportPartnerConsentHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PassportPartnerConsentHostedService> _logger;

    public PassportPartnerConsentHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<PassportPartnerConsentHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<PassportPartnerConsentJob>();
                await job.RunAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Passport partner consent job failed.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
