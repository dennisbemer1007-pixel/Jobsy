using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Jobs;

/// <summary>
/// Staged invite sends, invite reminders, inactivity checks — all no-ops when kandidatenkaart flag is off.
/// </summary>
public sealed class KandidatenkaartHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KandidatenkaartHostedService> _logger;

    public KandidatenkaartHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<KandidatenkaartHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Kandidatenkaart job tick failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var flags = scope.ServiceProvider.GetRequiredService<IFeatureFlags>();
        if (!await flags.IsEnabledAsync(PlatformFeature.Kandidatenkaart, cancellationToken))
        {
            return;
        }

        if (!await flags.IsEnabledAsync(PlatformFeature.Employers, cancellationToken))
        {
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var mail = scope.ServiceProvider.GetRequiredService<KandidatenkaartMailService>();
        var now = clock.GetUtcNow().UtcDateTime;

        await ProcessStagedInvitesAsync(db, mail, now, cancellationToken);
        await ProcessInviteRemindersAsync(db, mail, now, cancellationToken);
        await ProcessInactivityChecksAsync(db, mail, now, cancellationToken);
        await ProcessVacancyFullWithdrawalsAsync(db, mail, cancellationToken);
    }

    private static async Task ProcessStagedInvitesAsync(
        JobsyDbContext db,
        KandidatenkaartMailService mail,
        DateTime now,
        CancellationToken ct)
    {
        var campaigns = await db.KandidatenkaartInviteCampaigns
            .Include(c => c.Deliveries)
            .Where(c => c.Status == KandidatenkaartCampaignStatus.Sending
                        && c.NextRoundAtUtc != null
                        && c.NextRoundAtUtc <= now)
            .Take(5)
            .ToListAsync(ct);

        var sentDeliveries = new List<KandidatenkaartInviteDelivery>();

        foreach (var campaign in campaigns)
        {
            var batch = campaign.Deliveries
                .Where(d => d.Status == KandidatenkaartInviteDeliveryStatus.Queued && d.RoundNumber == campaign.CurrentRound)
                .Take(25)
                .ToList();

            foreach (var d in batch)
            {
                d.Status = KandidatenkaartInviteDeliveryStatus.Sent;
                d.SentAtUtc = now;
                sentDeliveries.Add(d);
            }

            var remaining = campaign.Deliveries.Any(d =>
                d.Status == KandidatenkaartInviteDeliveryStatus.Queued && d.RoundNumber == campaign.CurrentRound);
            if (remaining)
            {
                campaign.NextRoundAtUtc = now.AddHours(KandidatenkaartRules.StagedRoundDelayHours);
            }
            else if (campaign.Deliveries.Any(d => d.Status == KandidatenkaartInviteDeliveryStatus.Queued))
            {
                campaign.CurrentRound++;
                campaign.NextRoundAtUtc = now.AddHours(KandidatenkaartRules.StagedRoundDelayHours);
            }
            else
            {
                campaign.Status = KandidatenkaartCampaignStatus.Active;
                campaign.NextRoundAtUtc = null;
            }

            campaign.UpdatedAtUtc = now;
        }

        if (campaigns.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            await mail.SendBundledInvitesAsync(sentDeliveries, ct);
        }
    }

    private static async Task ProcessInviteRemindersAsync(
        JobsyDbContext db,
        KandidatenkaartMailService mail,
        DateTime now,
        CancellationToken ct)
    {
        var reminderCutoff = now.AddDays(-KandidatenkaartRules.InviteReminderDays);
        var autoCutoff = now.AddDays(-(KandidatenkaartRules.InviteReminderDays + KandidatenkaartRules.AutoUnavailableAfterReminderDays));

        var deliveries = await db.KandidatenkaartInviteDeliveries
            .Where(d => d.Status == KandidatenkaartInviteDeliveryStatus.Sent
                        && d.SentAtUtc <= reminderCutoff
                        && d.ReminderSentAtUtc == null)
            .Take(50)
            .ToListAsync(ct);

        foreach (var d in deliveries)
        {
            d.Status = KandidatenkaartInviteDeliveryStatus.Reminded;
            d.ReminderSentAtUtc = now;
            await mail.SendReminderAsync(d, ct);
        }

        var auto = await db.KandidatenkaartInviteDeliveries
            .Include(d => d.CandidateUser)
            .Where(d => d.Status == KandidatenkaartInviteDeliveryStatus.Reminded
                        && d.ReminderSentAtUtc <= autoCutoff)
            .Take(50)
            .ToListAsync(ct);

        foreach (var d in auto)
        {
            d.Status = KandidatenkaartInviteDeliveryStatus.AutoUnavailable;
            d.CandidateUser.OpenForWork = false;
            var settings = await db.CandidateKandidatenkaartSettings.FirstOrDefaultAsync(s => s.UserId == d.CandidateUserId, ct);
            if (settings is null)
            {
                settings = new Jobsy.Core.Entities.CandidateKandidatenkaartSettings { UserId = d.CandidateUserId };
                db.CandidateKandidatenkaartSettings.Add(settings);
            }

            settings.ReAvailabilityPromptAtUtc = now;
            settings.UpdatedAtUtc = now;
            await mail.SendMissedOutAsync(d, ct);
        }

        if (deliveries.Count + auto.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task ProcessInactivityChecksAsync(
        JobsyDbContext db,
        KandidatenkaartMailService mail,
        DateTime now,
        CancellationToken ct)
    {
        var inactiveSince = now.AddDays(-KandidatenkaartRules.InactivityLoginDays);
        var users = await db.Users
            .Where(u => u.Role == UserRole.Candidate
                        && u.OpenForWork
                        && u.LastLoginAtUtc != null
                        && u.LastLoginAtUtc <= inactiveSince)
            .Take(30)
            .ToListAsync(ct);

        foreach (var u in users)
        {
            var settings = await db.CandidateKandidatenkaartSettings.FirstOrDefaultAsync(s => s.UserId == u.Id, ct);
            if (settings?.InactivityCheckSentAtUtc is not null)
            {
                if (settings.InactivityCheckSentAtUtc <= now.AddDays(-KandidatenkaartRules.InactivityResponseDays))
                {
                    u.OpenForWork = false;
                    settings.ReAvailabilityPromptAtUtc = now;
                }

                continue;
            }

            settings ??= new Jobsy.Core.Entities.CandidateKandidatenkaartSettings { UserId = u.Id };
            if (settings.UserId == Guid.Empty)
            {
                settings.UserId = u.Id;
            }

            if (db.Entry(settings).State == EntityState.Detached)
            {
                db.CandidateKandidatenkaartSettings.Add(settings);
            }

            settings.InactivityCheckSentAtUtc = now;
            settings.UpdatedAtUtc = now;
            await mail.SendInactivityCheckAsync(u, ct);
        }

        if (users.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task ProcessVacancyFullWithdrawalsAsync(
        JobsyDbContext db,
        KandidatenkaartMailService mail,
        CancellationToken ct)
    {
        var fullVacancies = await db.Vacancies.AsNoTracking()
            .Where(v => v.PositionsTarget > 0 && v.PositionsFilled >= v.PositionsTarget)
            .Select(v => v.Id)
            .ToListAsync(ct);

        if (fullVacancies.Count == 0)
        {
            return;
        }

        var campaigns = await db.KandidatenkaartInviteCampaigns
            .Include(c => c.Deliveries)
            .Where(c => fullVacancies.Contains(c.VacancyId)
                        && c.Status != KandidatenkaartCampaignStatus.VacancyFull
                        && c.Status != KandidatenkaartCampaignStatus.Withdrawn)
            .ToListAsync(ct);

        var withdrawn = new List<KandidatenkaartInviteDelivery>();
        foreach (var c in campaigns)
        {
            c.Status = KandidatenkaartCampaignStatus.VacancyFull;
            foreach (var d in c.Deliveries.Where(d => d.Status is KandidatenkaartInviteDeliveryStatus.Queued
                         or KandidatenkaartInviteDeliveryStatus.Sent
                         or KandidatenkaartInviteDeliveryStatus.Reminded))
            {
                d.Status = KandidatenkaartInviteDeliveryStatus.Withdrawn;
                withdrawn.Add(d);
            }
        }

        if (campaigns.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            foreach (var d in withdrawn)
            {
                await mail.SendWithdrawnAsync(d, ct);
            }
        }
    }
}
