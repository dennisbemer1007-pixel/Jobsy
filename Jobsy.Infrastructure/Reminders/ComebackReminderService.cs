using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Reminders;
using Jobsy.Core.Rules;
using Jobsy.Core.Time;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jobsy.Infrastructure.Reminders;

public sealed record ComebackReminderRunResult(int Sent, bool Deferred);

public sealed record WhatsAppReminderView(bool Available, bool OptedIn, string? Phone);

public sealed class ComebackReminderService(
    JobsyDbContext db,
    IEnumerable<IReminderChannel> channels,
    IEmailPreferenceService preferences,
    IEmailLanguageResolver languages,
    IPlatformFeatureService platform,
    IFeatureFlags features,
    IOptions<WhatsAppReminderOptions> whatsAppOptions,
    ILogger<ComebackReminderService> logger)
{
    public const int MaxPerMonth = 2;
    public const int BasicTestsAfterDays = 7;
    public const int InactivityDays = 28;
    private const int PageSize = 200;
    private const int MaxPages = 20;

    public bool WhatsAppAvailable(FeatureFlagSnapshot flags)
        => flags.WhatsAppRemindersEnabled && whatsAppOptions.Value.IsConfigured;

    public async Task<WhatsAppReminderView> GetWhatsAppAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var flags = await features.GetAsync(cancellationToken);
        if (!WhatsAppAvailable(flags))
        {
            return new WhatsAppReminderView(false, false, null);
        }

        var row = await db.CandidateReminderPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        return new WhatsAppReminderView(
            true,
            row?.WhatsAppOptedInAtUtc is not null,
            row?.WhatsAppPhone);
    }

    public async Task<(bool Ok, string? Error, WhatsAppReminderView View)> SetWhatsAppAsync(
        Guid userId,
        bool optedIn,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        var flags = await features.GetAsync(cancellationToken);
        if (!WhatsAppAvailable(flags))
        {
            return (false, "unavailable", new WhatsAppReminderView(false, false, null));
        }

        var row = await db.CandidateReminderPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (!optedIn)
        {
            if (row is not null)
            {
                row.WhatsAppOptedInAtUtc = null;
                row.WhatsAppPhone = null;
                row.UpdatedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

            return (true, null, new WhatsAppReminderView(true, false, null));
        }

        var normalized = CandidatePhoneRules.Normalize(phone);
        if (string.IsNullOrWhiteSpace(normalized)
            || !CandidatePhoneRules.IsValid(normalized)
            || CandidatePhoneRules.ToWhatsAppE164Digits(normalized) is null)
        {
            return (false, "phone", new WhatsAppReminderView(true, false, null));
        }

        if (row is null)
        {
            row = new CandidateReminderPreference { UserId = userId };
            db.CandidateReminderPreferences.Add(row);
        }

        row.WhatsAppPhone = normalized;
        row.WhatsAppOptedInAtUtc ??= DateTime.UtcNow;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return (true, null, new WhatsAppReminderView(true, true, row.WhatsAppPhone));
    }

    public async Task<ComebackReminderStats> GetAnonymousStatsAsync(CancellationToken cancellationToken = default)
    {
        var logs = await db.ComebackReminderLogs.AsNoTracking()
            .Select(l => new { l.SentAtUtc, l.UserId })
            .ToListAsync(cancellationToken);
        var userIds = logs.Where(l => l.UserId is not null).Select(l => l.UserId!.Value).Distinct().ToList();
        var logins = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.LastLoginAtUtc })
            .ToDictionaryAsync(u => u.Id, u => u.LastLoginAtUtc, cancellationToken);

        var within7 = 0;
        var within30 = 0;
        foreach (var log in logs)
        {
            if (log.UserId is null || !logins.TryGetValue(log.UserId.Value, out var login) || login is null)
            {
                continue;
            }

            if (login <= log.SentAtUtc)
            {
                continue;
            }

            if (login <= log.SentAtUtc.AddDays(7))
            {
                within7++;
            }

            if (login <= log.SentAtUtc.AddDays(30))
            {
                within30++;
            }
        }

        return new ComebackReminderStats(logs.Count, within7, within30);
    }

    public async Task<ComebackReminderRunResult> RunAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var now = utcNow.Kind == DateTimeKind.Utc ? utcNow : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        if (!AmsterdamClock.IsSendWindow(now))
        {
            return new ComebackReminderRunResult(0, Deferred: true);
        }

        var snap = await platform.GetAsync(cancellationToken);
        var flags = await features.GetAsync(cancellationToken);
        var whatsAppOn = WhatsAppAvailable(flags);
        var baseUrl = string.IsNullOrWhiteSpace(snap.PublicWebBaseUrl) ? "https://lobsy.nl" : snap.PublicWebBaseUrl;
        var monthStart = AmsterdamClock.MonthStartUtc(now);
        var testsCutoff = now.AddDays(-BasicTestsAfterDays);
        var inactiveCutoff = now.AddDays(-InactivityDays);
        var sent = 0;

        for (var page = 0; page < MaxPages; page++)
        {
            var users = await db.Users.AsNoTracking()
                .Where(u => u.IsActive && u.Role == UserRole.Candidate)
                .Where(u =>
                    (u.TermsAcceptedAt != null && u.TermsAcceptedAt <= testsCutoff)
                    || (u.EmailVerifiedAtUtc != null && u.EmailVerifiedAtUtc <= testsCutoff)
                    || (u.LastLoginAtUtc != null && u.LastLoginAtUtc <= inactiveCutoff))
                .OrderBy(u => u.Id)
                .Skip(page * PageSize)
                .Take(PageSize)
                .Select(u => new CandidateRow(
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.TermsAcceptedAt,
                    u.EmailVerifiedAtUtc,
                    u.LastLoginAtUtc,
                    u.ReminderEmailsEnabled))
                .ToListAsync(cancellationToken);
            if (users.Count == 0)
            {
                break;
            }

            sent += await SendPageAsync(users, now, monthStart, testsCutoff, inactiveCutoff, baseUrl, whatsAppOn, cancellationToken);
        }

        if (sent > 0)
        {
            logger.LogInformation("Come-back reminders sent: {Sent}.", sent);
        }

        return new ComebackReminderRunResult(sent, Deferred: false);
    }

    private async Task<int> SendPageAsync(
        List<CandidateRow> users,
        DateTime now,
        DateTime monthStart,
        DateTime testsCutoff,
        DateTime inactiveCutoff,
        string baseUrl,
        bool whatsAppOn,
        CancellationToken cancellationToken)
    {
        var ids = users.Select(u => u.Id).ToList();
        var prefs = await db.CandidateReminderPreferences.AsNoTracking()
            .Where(p => ids.Contains(p.UserId))
            .ToListAsync(cancellationToken);
        var prefByUser = prefs.ToDictionary(p => p.UserId);
        var logs = await db.ComebackReminderLogs.AsNoTracking()
            .Where(l => l.UserId != null && ids.Contains(l.UserId.Value))
            .Select(l => new { l.UserId, l.Kind, l.SentAtUtc })
            .ToListAsync(cancellationToken);
        var competence = await CompletedIds(db.CandidateCompetencies.Where(c => ids.Contains(c.UserId) && c.CompletedAtUtc != null).Select(c => c.UserId), cancellationToken);
        var career = await CompletedIds(db.CandidateCareerInterests.Where(c => ids.Contains(c.UserId) && c.CompletedAtUtc != null).Select(c => c.UserId), cancellationToken);
        var cultureDone = await CompletedIds(db.CandidateCulturePersonalityProfiles.Where(c => ids.Contains(c.UserId) && c.CompletedAtUtc != null).Select(c => c.UserId), cancellationToken);
        var values = await CompletedIds(db.CandidateValuesProfiles.Where(c => ids.Contains(c.UserId) && c.CompletedAtUtc != null).Select(c => c.UserId), cancellationToken);
        var pushIds = await db.WebPushSubscriptions.AsNoTracking()
            .Where(s => ids.Contains(s.UserId))
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var pushSet = pushIds.ToHashSet();

        var sent = 0;
        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                continue;
            }

            prefByUser.TryGetValue(user.Id, out var pref);
            var userLogs = logs.Where(l => l.UserId == user.Id).ToList();
            if (userLogs.Count(l => l.SentAtUtc >= monthStart) >= MaxPerMonth)
            {
                continue;
            }

            var signup = user.TermsAcceptedAt ?? user.EmailVerifiedAtUtc;
            var testsDone = competence.Contains(user.Id)
                            && career.Contains(user.Id)
                            && cultureDone.Contains(user.Id)
                            && values.Contains(user.Id);
            string? kind = null;
            if (signup is not null
                && signup <= testsCutoff
                && !testsDone
                && userLogs.All(l => l.Kind != ComebackReminderKinds.BasicTests))
            {
                kind = ComebackReminderKinds.BasicTests;
            }
            else
            {
                var lastActivity = user.LastLoginAtUtc ?? signup;
                if (lastActivity is not null
                    && lastActivity <= inactiveCutoff
                    && userLogs.All(l => l.Kind != ComebackReminderKinds.LookAgain || l.SentAtUtc < lastActivity))
                {
                    kind = ComebackReminderKinds.LookAgain;
                }
            }

            if (kind is null)
            {
                continue;
            }

            var emailOn = user.ReminderEmailsEnabled
                          && pref?.EmailOptedInAtUtc is not null
                          && !await preferences.IsOptedOutAsync(user.Email, EmailOptionalCategories.ComebackReminder, cancellationToken);
            var pushOn = pushSet.Contains(user.Id);
            var whatsApp = whatsAppOn
                           && pref?.WhatsAppOptedInAtUtc is not null
                           && !string.IsNullOrWhiteSpace(pref.WhatsAppPhone);
            if (!emailOn && !pushOn && !whatsApp)
            {
                continue;
            }

            var culture = await languages.ResolveAsync(new EmailRecipient.User(user.Id), cancellationToken);
            var dispatch = new ReminderDispatch(
                user.Id,
                user.Email,
                user.FullName,
                kind,
                culture,
                baseUrl,
                now,
                emailOn,
                pushOn,
                whatsApp,
                pref?.WhatsAppPhone);
            var delivered = new List<string>();
            foreach (var channel in channels)
            {
                var result = await channel.SendAsync(dispatch, cancellationToken);
                if (result.Sent)
                {
                    delivered.Add(channel.Name);
                }
            }

            if (delivered.Count == 0)
            {
                continue;
            }

            db.ComebackReminderLogs.Add(new ComebackReminderLog
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Kind = kind,
                SentAtUtc = now,
                Channels = string.Join(',', delivered)
            });
            await db.SaveChangesAsync(cancellationToken);
            sent++;
        }

        return sent;
    }

    private static async Task<HashSet<Guid>> CompletedIds(IQueryable<Guid> query, CancellationToken cancellationToken)
    {
        var ids = await query.ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    private sealed record CandidateRow(
        Guid Id,
        string Email,
        string FullName,
        DateTime? TermsAcceptedAt,
        DateTime? EmailVerifiedAtUtc,
        DateTime? LastLoginAtUtc,
        bool ReminderEmailsEnabled);
}

public sealed record ComebackReminderStats(int Sent, int ReturnedWithin7Days, int ReturnedWithin30Days);
