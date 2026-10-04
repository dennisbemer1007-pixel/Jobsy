using System.Security.Cryptography;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Core.Time;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class ReferenceConfirmationService(
    JobsyDbContext db,
    ITransactionalMailer mailer,
    IUserNotificationService notifications,
    IPushNotificationService push,
    IEmailLanguageResolver languages,
    IPlatformFeatureService platform)
{
    public async Task<IReadOnlyList<ReferenceConfirmationCandidateRow>> ListForCandidateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var references = await db.CandidateReferences.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.SortOrder)
            .Select(r => new { r.Id, r.EmployerName, r.ContactName, r.Email })
            .ToListAsync(cancellationToken);
        var ids = references.Select(r => r.Id).ToList();
        var rows = await db.ReferenceConfirmations.AsNoTracking()
            .Where(c => ids.Contains(c.CandidateReferenceId))
            .ToListAsync(cancellationToken);
        var confirmationIds = rows.Select(r => r.Id).ToList();
        var tokenRows = confirmationIds.Count == 0
            ? []
            : await db.ReferenceConfirmationTokens.AsNoTracking()
                .Where(t => confirmationIds.Contains(t.ReferenceConfirmationId))
                .Select(t => t.ReferenceConfirmationId)
                .ToListAsync(cancellationToken);
        var countById = tokenRows.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        var byRef = rows.ToDictionary(c => c.CandidateReferenceId);

        return references.Select(r =>
        {
            byRef.TryGetValue(r.Id, out var row);
            countById.TryGetValue(row?.Id ?? Guid.Empty, out var used);
            return ToCandidateRow(r.Id, r.EmployerName, r.ContactName, r.Email, row, used);
        }).ToList();
    }

    public async Task<(bool Ok, string? Error)> RequestAsync(
        Guid userId,
        Guid referenceId,
        string? roleTitle,
        bool consentAccepted,
        CancellationToken cancellationToken = default)
    {
        if (!consentAccepted)
        {
            return (false, "consent");
        }

        var role = ReferenceConfirmationRules.NormalizeRole(roleTitle);
        if (role is null)
        {
            return (false, "role");
        }

        var reference = await db.CandidateReferences
            .FirstOrDefaultAsync(r => r.Id == referenceId && r.UserId == userId, cancellationToken);
        if (reference is null || !CandidateReferenceRules.IsValidEmail(reference.Email))
        {
            return (false, "not_found");
        }

        var user = await db.Users.FirstAsync(u => u.Id == userId, cancellationToken);
        var now = DateTime.UtcNow;
        var confirmation = await db.ReferenceConfirmations
            .FirstOrDefaultAsync(c => c.CandidateReferenceId == reference.Id, cancellationToken);
        if (confirmation?.Status == ReferenceConfirmationStatus.Confirmed)
        {
            return (false, "already");
        }

        var monthStart = MonthStartUtc(now);
        var monthCount = await db.ReferenceConfirmationTokens
            .CountAsync(
                t => t.ReferenceConfirmation.UserId == userId && t.CreatedAtUtc >= monthStart,
                cancellationToken);
        if (monthCount >= ReferenceConfirmationRules.MaxRequestsPerCandidatePerMonth)
        {
            return (false, "limit_month");
        }

        if (confirmation is not null)
        {
            var used = await db.ReferenceConfirmationTokens
                .CountAsync(t => t.ReferenceConfirmationId == confirmation.Id, cancellationToken);
            if (used >= ReferenceConfirmationRules.MaxRequestsPerReference)
            {
                return (false, "limit_reference");
            }
        }
        else
        {
            confirmation = new ReferenceConfirmation
            {
                Id = Guid.NewGuid(),
                CandidateReferenceId = reference.Id,
                UserId = userId,
                CreatedAtUtc = now
            };
            db.ReferenceConfirmations.Add(confirmation);
        }

        confirmation.RoleTitle = role;
        confirmation.Status = ReferenceConfirmationStatus.Pending;
        confirmation.CandidateConsentAtUtc = now;
        confirmation.ConsentVersion = ReferenceConfirmationRules.ConsentVersion;
        confirmation.UpdatedAtUtc = now;
        confirmation.DeclinedAtUtc = null;

        var plaintext = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var token = new ReferenceConfirmationToken
        {
            Id = Guid.NewGuid(),
            ReferenceConfirmationId = confirmation.Id,
            TokenHash = VerificationCodes.Hash(plaintext),
            ExpiresAtUtc = now.AddDays(ReferenceConfirmationRules.TokenDays),
            CreatedAtUtc = now
        };

        var older = await db.ReferenceConfirmationTokens
            .Where(t => t.ReferenceConfirmationId == confirmation.Id && t.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var old in older)
        {
            old.UsedAtUtc = now;
        }

        db.ReferenceConfirmationTokens.Add(token);
        db.ReferenceConfirmationConsentLogs.Add(new ReferenceConfirmationConsentLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ReferenceConfirmationId = confirmation.Id,
            Actor = "candidate",
            Action = "requested",
            ConsentVersion = ReferenceConfirmationRules.ConsentVersion,
            Text = ReferenceConfirmationRules.CandidateConsentText,
            AtUtc = now
        });
        await db.SaveChangesAsync(cancellationToken);

        var snap = await platform.GetAsync(cancellationToken);
        var baseUrl = string.IsNullOrWhiteSpace(snap.PublicWebBaseUrl) ? "https://lobsy.nl" : snap.PublicWebBaseUrl;
        var link = EmailLinks.For(baseUrl).ReferenceConfirmation(plaintext);
        var candidateName = NameParts.FirstName(user.FirstName) ?? NameParts.FirstName(user.FullName) ?? "iemand";
        var mail = TransactionalEmails.ReferenceConfirmationRequest(
            baseUrl,
            reference.ContactName,
            candidateName,
            role,
            link);
        var sent = await mailer.SendAsync(
            mail,
            reference.Email,
            new EmailSendOptions(IdempotencyKey: "reference-confirm:" + token.Id.ToString("N")),
            cancellationToken);
        if (!sent.Sent)
        {
            db.ReferenceConfirmationTokens.Remove(token);
            await db.SaveChangesAsync(cancellationToken);
            return (false, "mail");
        }

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> SetShareAsync(
        Guid userId,
        Guid referenceId,
        ReferenceShareChoice choice,
        CancellationToken cancellationToken = default)
    {
        var confirmation = await db.ReferenceConfirmations
            .FirstOrDefaultAsync(
                c => c.CandidateReferenceId == referenceId && c.UserId == userId,
                cancellationToken);
        if (confirmation is null || confirmation.Status != ReferenceConfirmationStatus.Confirmed)
        {
            return (false, "not_confirmed");
        }

        confirmation.ShowOnPartnerPassport = choice.ShowOnPartnerPassport;
        confirmation.ShareWorkedHere = choice.ShareWorkedHere;
        confirmation.SharePeriod = choice.SharePeriod;
        confirmation.ShareDidWell = choice.ShareDidWell;
        confirmation.ShareWorkAgain = choice.ShareWorkAgain;
        confirmation.ShareExtra = choice.ShareExtra;
        confirmation.UpdatedAtUtc = DateTime.UtcNow;
        db.ReferenceConfirmationConsentLogs.Add(new ReferenceConfirmationConsentLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ReferenceConfirmationId = confirmation.Id,
            Actor = "candidate",
            Action = "share",
            ConsentVersion = ReferenceConfirmationRules.ConsentVersion,
            Text = choice.ShowOnPartnerPassport
                ? "De kandidaat zet de bevestiging aan voor het partnerpaspoort."
                : "De kandidaat zet de bevestiging uit voor het partnerpaspoort.",
            AtUtc = confirmation.UpdatedAtUtc
        });
        await db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<ReferencePublicView?> OpenAsync(string? token, CancellationToken cancellationToken = default)
    {
        var row = await FindTokenAsync(token, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var state = StateOf(row, now);
        if (state is not ("open" or "used" or "expired"))
        {
            return null;
        }

        var confirmation = row.ReferenceConfirmation;
        var reference = confirmation.CandidateReference;
        var user = confirmation.User;
        return new ReferencePublicView(
            state,
            NameParts.FirstName(user.FirstName) ?? NameParts.FirstName(user.FullName) ?? "deze persoon",
            confirmation.RoleTitle,
            reference.EmployerName,
            confirmation.Status == ReferenceConfirmationStatus.Confirmed);
    }

    public async Task<string> SubmitAsync(
        string? token,
        bool? workedHere,
        string? period,
        string? didWell,
        string? workAgain,
        string? extra,
        CancellationToken cancellationToken = default)
    {
        var row = await FindTokenAsync(token, cancellationToken);
        if (row is null)
        {
            return "invalid";
        }

        var now = DateTime.UtcNow;
        var state = StateOf(row, now);
        if (state != "open")
        {
            return state;
        }

        var confirmation = row.ReferenceConfirmation;
        if (confirmation.Status == ReferenceConfirmationStatus.Confirmed)
        {
            row.UsedAtUtc = now;
            await db.SaveChangesAsync(cancellationToken);
            return "used";
        }

        if (workedHere is null)
        {
            return "answers";
        }

        var periodText = ReferenceConfirmationRules.NormalizePeriod(period);
        var well = ReferenceConfirmationRules.NormalizeDidWell(didWell);
        var again = ReferenceConfirmationRules.NormalizeWorkAgain(workAgain);
        if (periodText is null || well is null || again is null)
        {
            return "answers";
        }

        confirmation.WorkedHere = workedHere;
        confirmation.PeriodText = periodText;
        confirmation.DidWell = well;
        confirmation.WorkAgain = again;
        confirmation.ExtraText = ReferenceConfirmationRules.NormalizeExtra(extra);
        confirmation.Status = ReferenceConfirmationStatus.Confirmed;
        confirmation.ConfirmedAtUtc = now;
        confirmation.UpdatedAtUtc = now;
        row.UsedAtUtc = now;
        db.ReferenceConfirmationConsentLogs.Add(new ReferenceConfirmationConsentLog
        {
            Id = Guid.NewGuid(),
            UserId = confirmation.UserId,
            ReferenceConfirmationId = confirmation.Id,
            Actor = "referee",
            Action = "submitted",
            ConsentVersion = ReferenceConfirmationRules.ConsentVersion,
            Text = ReferenceConfirmationRules.RefereePrivacyText,
            AtUtc = now
        });
        await db.SaveChangesAsync(cancellationToken);
        await NotifyAsync(confirmation, confirmed: true, cancellationToken);
        return "ok";
    }

    public async Task<string> DeclineAsync(string? token, CancellationToken cancellationToken = default)
    {
        var row = await FindTokenAsync(token, cancellationToken);
        if (row is null)
        {
            return "invalid";
        }

        var now = DateTime.UtcNow;
        var state = StateOf(row, now);
        if (state != "open")
        {
            return state;
        }

        var confirmation = row.ReferenceConfirmation;
        if (confirmation.Status != ReferenceConfirmationStatus.Confirmed)
        {
            confirmation.Status = ReferenceConfirmationStatus.Declined;
            confirmation.DeclinedAtUtc = now;
            confirmation.UpdatedAtUtc = now;
        }

        row.UsedAtUtc = now;
        db.ReferenceConfirmationConsentLogs.Add(new ReferenceConfirmationConsentLog
        {
            Id = Guid.NewGuid(),
            UserId = confirmation.UserId,
            ReferenceConfirmationId = confirmation.Id,
            Actor = "referee",
            Action = "declined",
            ConsentVersion = ReferenceConfirmationRules.ConsentVersion,
            Text = ReferenceConfirmationRules.RefereePrivacyText,
            AtUtc = now
        });
        await db.SaveChangesAsync(cancellationToken);
        if (confirmation.Status == ReferenceConfirmationStatus.Declined)
        {
            await NotifyAsync(confirmation, confirmed: false, cancellationToken);
        }

        return "ok";
    }

    public async Task<string> ReportMisuseAsync(
        string? token,
        string? message,
        CancellationToken cancellationToken = default)
    {
        var row = await FindTokenAsync(token, cancellationToken);
        if (row is null)
        {
            return "invalid";
        }

        var confirmation = row.ReferenceConfirmation;
        var already = await db.ReferenceMisuseReports
            .AnyAsync(r => r.ReferenceConfirmationId == confirmation.Id, cancellationToken);
        if (already)
        {
            return "ok";
        }

        db.ReferenceMisuseReports.Add(new ReferenceMisuseReport
        {
            Id = Guid.NewGuid(),
            UserId = confirmation.UserId,
            ReferenceConfirmationId = confirmation.Id,
            Message = ReferenceConfirmationRules.NormalizeMisuse(message),
            CreatedAtUtc = DateTime.UtcNow
        });
        db.ReferenceConfirmationConsentLogs.Add(new ReferenceConfirmationConsentLog
        {
            Id = Guid.NewGuid(),
            UserId = confirmation.UserId,
            ReferenceConfirmationId = confirmation.Id,
            Actor = "referee",
            Action = "misuse",
            ConsentVersion = ReferenceConfirmationRules.ConsentVersion,
            Text = ReferenceConfirmationRules.RefereePrivacyText,
            AtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return "ok";
    }

    public static void Rebind(
        IReadOnlyList<CandidateReference> removed,
        IReadOnlyList<CandidateReference> added,
        IReadOnlyList<ReferenceConfirmation> confirmations)
    {
        if (confirmations.Count == 0 || removed.Count == 0)
        {
            return;
        }

        var addedByKey = new Dictionary<string, Queue<CandidateReference>>(StringComparer.Ordinal);
        foreach (var row in added)
        {
            var key = MatchKey(row.EmployerName, row.Email);
            if (!addedByKey.TryGetValue(key, out var queue))
            {
                queue = new Queue<CandidateReference>();
                addedByKey[key] = queue;
            }

            queue.Enqueue(row);
        }

        var removedById = removed.ToDictionary(r => r.Id);
        foreach (var confirmation in confirmations)
        {
            if (!removedById.TryGetValue(confirmation.CandidateReferenceId, out var old))
            {
                continue;
            }

            var key = MatchKey(old.EmployerName, old.Email);
            if (addedByKey.TryGetValue(key, out var queue) && queue.Count > 0)
            {
                confirmation.CandidateReferenceId = queue.Dequeue().Id;
            }
        }
    }

    private async Task NotifyAsync(
        ReferenceConfirmation confirmation,
        bool confirmed,
        CancellationToken cancellationToken)
    {
        var reference = confirmation.CandidateReference
            ?? await db.CandidateReferences.FirstAsync(r => r.Id == confirmation.CandidateReferenceId, cancellationToken);
        var user = confirmation.User
            ?? await db.Users.FirstAsync(u => u.Id == confirmation.UserId, cancellationToken);
        var culture = await languages.ResolveAsync(new EmailRecipient.User(user.Id), cancellationToken);
        var prefix = confirmed ? "Email.ReferenceConfirmed" : "Email.ReferenceDeclined";
        var title = EmailStrings.Get(culture, prefix + ".PushTitle");
        var body = EmailStrings.FormatRaw(culture, prefix + ".PushBody", reference.ContactName);
        const string link = "/candidate/paspoort?tab=proof";
        await notifications.CreateAsync(
            new NotificationCreateRequest(
                user.Id,
                title,
                body,
                confirmed ? "ReferenceConfirmed" : "ReferenceDeclined",
                link,
                title,
                link,
                nameof(ReferenceConfirmation),
                confirmation.Id),
            cancellationToken);
        await push.SendAsync(
            new PushMessage(user.Email, title, body, link, confirmed ? "ReferenceConfirmed" : "ReferenceDeclined"),
            cancellationToken);
    }

    private async Task<ReferenceConfirmationToken?> FindTokenAsync(
        string? token,
        CancellationToken cancellationToken)
    {
        var normalized = (token ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(c => c is (< '0' or > '9') and (< 'a' or > 'f')))
        {
            return null;
        }

        var hash = VerificationCodes.Hash(normalized);
        return await db.ReferenceConfirmationTokens
            .Include(t => t.ReferenceConfirmation)
            .ThenInclude(c => c.CandidateReference)
            .Include(t => t.ReferenceConfirmation)
            .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
    }

    private static string StateOf(ReferenceConfirmationToken row, DateTime now)
    {
        if (row.UsedAtUtc is not null)
        {
            return "used";
        }

        if (row.ExpiresAtUtc < now)
        {
            return "expired";
        }

        return "open";
    }

    private static DateTime MonthStartUtc(DateTime utc)
    {
        var local = AmsterdamTime.ToLocal(utc);
        var start = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return AmsterdamTime.ToUtc(start);
    }

    private static string MatchKey(string employer, string email)
        => employer.Trim().ToUpperInvariant() + "|" + email.Trim().ToUpperInvariant();

    private static ReferenceConfirmationCandidateRow ToCandidateRow(
        Guid referenceId,
        string employer,
        string contact,
        string email,
        ReferenceConfirmation? row,
        int requestsUsed)
        => new(
            referenceId,
            employer,
            contact,
            email,
            row?.Status ?? "none",
            row?.RoleTitle,
            row?.ConfirmedAtUtc,
            row?.DeclinedAtUtc,
            row?.WorkedHere,
            row?.PeriodText,
            row?.DidWell,
            row?.WorkAgain,
            row?.ExtraText,
            row?.ShowOnPartnerPassport ?? false,
            row?.ShareWorkedHere ?? false,
            row?.SharePeriod ?? false,
            row?.ShareDidWell ?? false,
            row?.ShareWorkAgain ?? false,
            row?.ShareExtra ?? false,
            requestsUsed);
}

public sealed record ReferenceShareChoice(
    bool ShowOnPartnerPassport,
    bool ShareWorkedHere,
    bool SharePeriod,
    bool ShareDidWell,
    bool ShareWorkAgain,
    bool ShareExtra);

public sealed record ReferenceConfirmationCandidateRow(
    Guid ReferenceId,
    string EmployerName,
    string ContactName,
    string Email,
    string Status,
    string? RoleTitle,
    DateTime? ConfirmedAtUtc,
    DateTime? DeclinedAtUtc,
    bool? WorkedHere,
    string? Period,
    string? DidWell,
    string? WorkAgain,
    string? Extra,
    bool ShowOnPartnerPassport,
    bool ShareWorkedHere,
    bool SharePeriod,
    bool ShareDidWell,
    bool ShareWorkAgain,
    bool ShareExtra,
    int RequestsUsed);

public sealed record ReferencePublicView(
    string State,
    string CandidateName,
    string RoleTitle,
    string EmployerName,
    bool AlreadyConfirmed);
