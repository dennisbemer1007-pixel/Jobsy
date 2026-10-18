using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Localization;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Passport;
using Jobsy.Core.Rules;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class ReferenceVerificationService(
    JobsyDbContext db,
    ITransactionalMailer mailer,
    IUserNotificationService notifications,
    IPushNotificationService push,
    IEmailLanguageResolver languages,
    IPlatformFeatureService platform,
    IPassportDnaReader dnaReader,
    ICandidateCompetencyService competencies)
{
    public async Task<bool> IsFeatureEnabledAsync(CancellationToken cancellationToken = default)
    {
        var snap = await platform.GetAsync(cancellationToken);
        return snap.ReferenceVerificationEnabled;
    }

    public async Task<bool> IsEligibleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await IsFeatureEnabledAsync(cancellationToken))
        {
            return false;
        }

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var prefs = ParsePreferences(user.PreferencesJson);
        var layers = await dnaReader.ReadAsync(userId, JobsyLanguages.Normalize(prefs.Language), cancellationToken);
        return ReferenceVerificationRules.FeatureEligible(prefs, PassportDnaCompletion.AllLayersCompleted(layers));
    }

    public async Task<IReadOnlyList<ReferenceVerificationListRow>> ListForCandidateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var rows = await db.WorkExperienceReferenceRequests.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        var ids = rows.Select(r => r.Id).ToList();
        var tokens = ids.Count == 0
            ? []
            : await db.WorkExperienceReferenceTokens.AsNoTracking()
                .Where(t => ids.Contains(t.RequestId))
                .ToListAsync(cancellationToken);
        var latestTokenByRequest = tokens
            .GroupBy(t => t.RequestId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.CreatedAtUtc).First());

        return rows.Select(r =>
        {
            latestTokenByRequest.TryGetValue(r.Id, out var token);
            var display = ReferenceVerificationRules.DisplayStatus(r);
            DateTime? sentAt = token?.CreatedAtUtc;
            DateTime? expiresAt = token?.ExpiresAtUtc;
            if (display == "sent" && token is not null && token.UsedAtUtc is null && token.ExpiresAtUtc < now)
            {
                display = "expired";
            }

            int? daysLeft = null;
            if (display == "sent" && expiresAt is DateTime exp)
            {
                daysLeft = Math.Max(0, (int)Math.Ceiling((exp - now).TotalDays));
            }

            return new ReferenceVerificationListRow(
                r.Id,
                r.WorkExperienceEntryId,
                r.EmployerName,
                r.RoleTitle,
                display,
                r.ReferentName,
                r.Relation,
                r.CompletedAtUtc,
                sentAt,
                expiresAt,
                daysLeft,
                ReferenceVerificationRules.IsActiveVerification(r),
                ReferenceVerificationRules.ParseEndorsedCodes(r.EndorsedCompetenceCodesJson).Count);
        }).ToList();
    }

    public async Task<(bool Ok, string? Error)> RequestAsync(
        Guid userId,
        Guid workExperienceEntryId,
        string? referentName,
        string? relation,
        string? email,
        bool consentAccepted,
        CancellationToken cancellationToken = default)
    {
        if (!await IsFeatureEnabledAsync(cancellationToken))
        {
            return (false, "disabled");
        }

        if (!consentAccepted)
        {
            return (false, "consent");
        }

        var name = ReferenceVerificationRules.NormalizeName(referentName);
        var rel = ReferenceVerificationRules.NormalizeRelation(relation);
        var mail = (email ?? string.Empty).Trim();
        if (name is null || rel is null || !ReferenceVerificationRules.IsValidEmail(mail))
        {
            return (false, "referent");
        }

        var user = await db.Users.FirstAsync(u => u.Id == userId, cancellationToken);
        var prefs = ParsePreferences(user.PreferencesJson);
        var employers = WorkExperienceEntryIds.EnsureIds(prefs.Employers);
        var entry = employers.FirstOrDefault(e => WorkExperienceEntryIds.ResolveEntryId(e) == workExperienceEntryId);
        if (entry is null || string.IsNullOrWhiteSpace(entry.EmployerName))
        {
            return (false, "not_found");
        }

        var layers = await dnaReader.ReadAsync(userId, JobsyLanguages.Normalize(prefs.Language), cancellationToken);
        if (!ReferenceVerificationRules.FeatureEligible(prefs with { Employers = employers }, PassportDnaCompletion.AllLayersCompleted(layers)))
        {
            return (false, "not_eligible");
        }

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthCount = await db.WorkExperienceReferenceTokens
            .CountAsync(t => t.Request.UserId == userId && t.CreatedAtUtc >= monthStart, cancellationToken);
        if (monthCount >= ReferenceVerificationRules.MaxRequestsPerCandidatePerMonth)
        {
            return (false, "limit_month");
        }

        var existing = await db.WorkExperienceReferenceRequests
            .FirstOrDefaultAsync(
                r => r.UserId == userId && r.WorkExperienceEntryId == workExperienceEntryId && !r.RemovedByCandidate,
                cancellationToken);
        if (existing?.Status == WorkExperienceReferenceStatus.Confirmed && ReferenceVerificationRules.IsActiveVerification(existing))
        {
            return (false, "already");
        }

        if (existing is not null)
        {
            var entryMonthCount = await db.WorkExperienceReferenceTokens
                .CountAsync(t => t.RequestId == existing.Id && t.CreatedAtUtc >= monthStart, cancellationToken);
            if (entryMonthCount >= ReferenceVerificationRules.MaxRequestsPerEntryPerMonth)
            {
                return (false, "limit_entry");
            }
        }

        var request = existing ?? new WorkExperienceReferenceRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WorkExperienceEntryId = workExperienceEntryId,
            CreatedAtUtc = now
        };

        request.ReferentName = name;
        request.Relation = rel;
        request.ReferentEmail = mail;
        request.Status = WorkExperienceReferenceStatus.Pending;
        request.EmployerName = entry.EmployerName.Trim();
        request.RoleTitle = string.IsNullOrWhiteSpace(entry.Role) ? "medewerker" : entry.Role.Trim();
        request.StartMonth = entry.StartMonth;
        request.EndMonth = entry.EndMonth;
        request.IsCurrent = entry.IsCurrent == true
                            || (entry.IsCurrent is null && string.IsNullOrWhiteSpace(entry.EndMonth));
        request.DutiesText = ReferenceVerificationRules.NormalizeDuties(entry.Description);
        request.CandidateConsentAtUtc = now;
        request.ConsentVersion = ReferenceVerificationRules.ConsentVersion;
        request.UpdatedAtUtc = now;
        request.RemovedByCandidate = false;
        request.CompletedAtUtc = null;
        request.ConfirmedEmployer = null;
        request.ConfirmedPeriod = null;
        request.ConfirmedRole = null;
        request.ConfirmedDuties = null;
        request.ReferentComment = null;
        request.UnknownPersonReported = false;
        request.EndorsedCompetenceCodesJson = null;

        if (existing is null)
        {
            db.WorkExperienceReferenceRequests.Add(request);
        }

        var plaintext = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var token = new WorkExperienceReferenceToken
        {
            Id = Guid.NewGuid(),
            RequestId = request.Id,
            TokenHash = VerificationCodes.Hash(plaintext),
            ExpiresAtUtc = now.AddDays(ReferenceVerificationRules.TokenDays),
            CreatedAtUtc = now
        };

        var older = await db.WorkExperienceReferenceTokens
            .Where(t => t.RequestId == request.Id && t.UsedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var old in older)
        {
            old.UsedAtUtc = now;
        }

        db.WorkExperienceReferenceTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);

        var snap = await platform.GetAsync(cancellationToken);
        var baseUrl = string.IsNullOrWhiteSpace(snap.PublicWebBaseUrl) ? "https://lobsy.nl" : snap.PublicWebBaseUrl;
        var link = EmailLinks.For(baseUrl).WorkExperienceReference(plaintext);
        var candidateName = NameParts.FirstName(user.FirstName) ?? NameParts.FirstName(user.FullName) ?? "iemand";
        var message = TransactionalEmails.WorkExperienceReferenceRequest(
            baseUrl,
            name,
            candidateName,
            request.EmployerName,
            request.RoleTitle,
            link);
        var sent = await mailer.SendAsync(
            message,
            mail,
            new EmailSendOptions(IdempotencyKey: "work-exp-ref:" + token.Id.ToString("N")),
            cancellationToken);
        if (!sent.Sent)
        {
            db.WorkExperienceReferenceTokens.Remove(token);
            await db.SaveChangesAsync(cancellationToken);
            return (false, "mail");
        }

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> RevokeAsync(
        Guid userId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var row = await db.WorkExperienceReferenceRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId, cancellationToken);
        if (row is null)
        {
            return (false, "not_found");
        }

        row.Status = WorkExperienceReferenceStatus.Revoked;
        row.RemovedByCandidate = true;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<ReferenceVerificationPublicView?> OpenAsync(string? token, CancellationToken cancellationToken = default)
    {
        var row = await FindTokenAsync(token, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var state = TokenState(row, now);
        if (state is not ("open" or "used" or "expired"))
        {
            return null;
        }

        var request = row.Request;
        var user = request.User;
        var language = JobsyLanguages.Normalize(ParsePreferences(user.PreferencesJson).Language);
        var scores = await competencies.GetCompletedScoresAsync(user.Id, cancellationToken);
        var competenceItems = PassportDnaWords.CompetencyItems(scores, language, ReferenceVerificationRules.MaxCompetencesOffered)
            .Select(x => new ReferenceVerificationCompetenceOffer(
                x.Code,
                x.Label,
                ReferenceVerificationCompetenceCopy.Description(x.Code, language)))
            .ToList();

        return new ReferenceVerificationPublicView(
            state,
            NameParts.FirstName(user.FirstName) ?? NameParts.FirstName(user.FullName) ?? "deze persoon",
            request.EmployerName,
            request.RoleTitle,
            request.StartMonth,
            request.EndMonth,
            request.IsCurrent,
            request.DutiesText,
            competenceItems,
            request.Status == WorkExperienceReferenceStatus.Confirmed);
    }

    public async Task<string> SubmitAsync(
        string? token,
        bool? confirmedEmployer,
        bool? confirmedPeriod,
        bool? confirmedRole,
        bool? confirmedDuties,
        IReadOnlyList<string>? endorsedCodes,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var row = await FindTokenAsync(token, cancellationToken);
        if (row is null)
        {
            return "invalid";
        }

        var now = DateTime.UtcNow;
        if (TokenState(row, now) != "open")
        {
            return TokenState(row, now);
        }

        var request = row.Request;
        if (confirmedEmployer is null || confirmedPeriod is null || confirmedRole is null)
        {
            return "answers";
        }

        if (confirmedEmployer == false || confirmedPeriod == false || confirmedRole == false)
        {
            request.Status = WorkExperienceReferenceStatus.Declined;
            request.ConfirmedEmployer = confirmedEmployer;
            request.ConfirmedPeriod = confirmedPeriod;
            request.ConfirmedRole = confirmedRole;
            request.CompletedAtUtc ??= now;
            row.UsedAtUtc = now;
            await db.SaveChangesAsync(cancellationToken);
            await NotifyCandidateAsync(request, confirmed: false, cancellationToken);
            return "ok";
        }

        var allowed = (await OpenAsync(token, cancellationToken))?.Competences.Select(c => c.Code).ToHashSet(StringComparer.Ordinal)
                      ?? [];
        var endorsed = (endorsedCodes ?? [])
            .Where(c => allowed.Contains(c))
            .Distinct(StringComparer.Ordinal)
            .Take(ReferenceVerificationRules.MaxCompetencesOffered)
            .ToList();

        request.ConfirmedEmployer = true;
        request.ConfirmedPeriod = true;
        request.ConfirmedRole = true;
        request.ConfirmedDuties = confirmedDuties;
        request.ReferentComment = ReferenceVerificationRules.NormalizeComment(comment);
        request.EndorsedCompetenceCodesJson = endorsed.Count > 0
            ? ReferenceVerificationRules.SerializeEndorsedCodes(endorsed)
            : null;
        request.Status = WorkExperienceReferenceStatus.Confirmed;
        request.CompletedAtUtc = now;
        request.UpdatedAtUtc = now;
        row.UsedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        await NotifyCandidateAsync(request, confirmed: true, cancellationToken);
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
        if (TokenState(row, now) != "open")
        {
            return TokenState(row, now);
        }

        var request = row.Request;
        request.Status = WorkExperienceReferenceStatus.Declined;
        request.UpdatedAtUtc = now;
        request.CompletedAtUtc ??= now;
        row.UsedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        await NotifyCandidateAsync(request, confirmed: false, cancellationToken);
        return "ok";
    }

    public async Task<string> ReportUnknownAsync(string? token, CancellationToken cancellationToken = default)
    {
        var row = await FindTokenAsync(token, cancellationToken);
        if (row is null)
        {
            return "invalid";
        }

        var now = DateTime.UtcNow;
        if (TokenState(row, now) != "open")
        {
            return TokenState(row, now);
        }

        var request = row.Request;
        request.UnknownPersonReported = true;
        request.Status = WorkExperienceReferenceStatus.Declined;
        request.UpdatedAtUtc = now;
        request.CompletedAtUtc ??= now;
        row.UsedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        await NotifyCandidateAsync(request, confirmed: false, cancellationToken);
        return "ok";
    }

    public async Task<IReadOnlyList<VerifiedCompetenceFact>> VerifiedCompetencesForCandidateAsync(
        Guid userId,
        string language,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.WorkExperienceReferenceRequests.AsNoTracking()
            .Where(r => r.UserId == userId
                        && r.Status == WorkExperienceReferenceStatus.Confirmed
                        && !r.RemovedByCandidate
                        && r.CompletedAtUtc != null)
            .ToListAsync(cancellationToken);
        var result = new List<VerifiedCompetenceFact>();
        foreach (var row in rows.Where(ReferenceVerificationRules.IsActiveVerification))
        {
            foreach (var code in ReferenceVerificationRules.ParseEndorsedCodes(row.EndorsedCompetenceCodesJson))
            {
                var label = DimensionLabels.For(code, language);
                if (string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                result.Add(new VerifiedCompetenceFact(
                    code,
                    label,
                    ReferenceVerificationRules.EndorsementLine(language, row.ReferentName, row.Relation, row.EmployerName),
                    row.CompletedAtUtc!.Value));
            }
        }

        return result
            .GroupBy(f => f.Code, StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(x => x.CompletedAtUtc).First())
            .ToList();
    }

    public async Task<int> SendDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsFeatureEnabledAsync(cancellationToken))
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        var remindAfter = now.AddDays(-7);
        var tokens = await db.WorkExperienceReferenceTokens
            .Include(t => t.Request)
            .ThenInclude(r => r.User)
            .Where(t => t.UsedAtUtc == null
                        && t.ReminderSentAtUtc == null
                        && t.ExpiresAtUtc > now
                        && t.CreatedAtUtc <= remindAfter
                        && t.Request.Status == WorkExperienceReferenceStatus.Pending
                        && !db.WorkExperienceReferenceTokens.Any(other =>
                            other.RequestId == t.RequestId && other.ReminderSentAtUtc != null))
            .Take(50)
            .ToListAsync(cancellationToken);
        var sent = 0;
        foreach (var token in tokens)
        {
            var request = token.Request;
            var snap = await platform.GetAsync(cancellationToken);
            var baseUrl = string.IsNullOrWhiteSpace(snap.PublicWebBaseUrl) ? "https://lobsy.nl" : snap.PublicWebBaseUrl;
            var (plaintext, reminderToken) = await ReissuePlaintextForReminderAsync(token, cancellationToken);
            if (plaintext is null || reminderToken is null)
            {
                continue;
            }

            var link = EmailLinks.For(baseUrl).WorkExperienceReference(plaintext);
            var candidateName = NameParts.FirstName(request.User.FirstName)
                                ?? NameParts.FirstName(request.User.FullName)
                                ?? "iemand";
            var mail = TransactionalEmails.WorkExperienceReferenceReminder(
                baseUrl,
                request.ReferentName,
                candidateName,
                request.EmployerName,
                link);
            var result = await mailer.SendAsync(
                mail,
                request.ReferentEmail,
                new EmailSendOptions(IdempotencyKey: "work-exp-ref-reminder:" + token.Id.ToString("N")),
                cancellationToken);
            if (!result.Sent)
            {
                continue;
            }

            reminderToken.ReminderSentAtUtc = now;
            sent++;
        }

        if (sent > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return sent;
    }

    private async Task<(string? Plaintext, WorkExperienceReferenceToken? Token)> ReissuePlaintextForReminderAsync(
        WorkExperienceReferenceToken token,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        token.UsedAtUtc = now;
        var plaintext = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var replacement = new WorkExperienceReferenceToken
        {
            Id = Guid.NewGuid(),
            RequestId = token.RequestId,
            TokenHash = VerificationCodes.Hash(plaintext),
            ExpiresAtUtc = token.ExpiresAtUtc,
            CreatedAtUtc = now
        };
        db.WorkExperienceReferenceTokens.Add(replacement);
        await db.SaveChangesAsync(cancellationToken);
        return (plaintext, replacement);
    }

    public async Task<IReadOnlyList<VerifiedWorkExperienceFact>> VerifiedWorkExperiencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.WorkExperienceReferenceRequests.AsNoTracking()
            .Where(r => r.UserId == userId
                        && r.Status == WorkExperienceReferenceStatus.Confirmed
                        && !r.RemovedByCandidate)
            .ToListAsync(cancellationToken);
        return rows
            .Where(ReferenceVerificationRules.IsActiveVerification)
            .Select(r => new VerifiedWorkExperienceFact(
                r.WorkExperienceEntryId,
                r.EmployerName,
                r.CompletedAtUtc))
            .ToList();
    }

    private async Task NotifyCandidateAsync(
        WorkExperienceReferenceRequest request,
        bool confirmed,
        CancellationToken cancellationToken)
    {
        var user = request.User
            ?? await db.Users.FirstAsync(u => u.Id == request.UserId, cancellationToken);
        var culture = await languages.ResolveAsync(new EmailRecipient.User(user.Id), cancellationToken);
        var prefix = confirmed ? "Email.WorkExpRefConfirmed" : "Email.WorkExpRefDeclined";
        var title = EmailStrings.Get(culture, prefix + ".PushTitle");
        var body = EmailStrings.FormatRaw(culture, prefix + ".PushBody", request.ReferentName);
        const string link = "/candidate/paspoort?tab=references";
        await notifications.CreateAsync(
            new NotificationCreateRequest(
                user.Id,
                title,
                body,
                confirmed ? "WorkExpRefConfirmed" : "WorkExpRefDeclined",
                link,
                title,
                link,
                nameof(WorkExperienceReferenceRequest),
                request.Id),
            cancellationToken);
        await push.SendAsync(
            new PushMessage(user.Email, title, body, link, confirmed ? "WorkExpRefConfirmed" : "WorkExpRefDeclined"),
            cancellationToken);
    }

    private static CandidatePreferencesDto ParsePreferences(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new CandidatePreferencesDto([], null, null);
        }

        try
        {
            return JsonSerializer.Deserialize<CandidatePreferencesDto>(json, PreferenceJson)
                   ?? new CandidatePreferencesDto([], null, null);
        }
        catch (JsonException)
        {
            return new CandidatePreferencesDto([], null, null);
        }
    }

    private static readonly JsonSerializerOptions PreferenceJson = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private async Task<WorkExperienceReferenceToken?> FindTokenAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length != 64)
        {
            return null;
        }

        var hash = VerificationCodes.Hash(token);
        return await db.WorkExperienceReferenceTokens
            .Include(t => t.Request)
            .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
    }

    private static string TokenState(WorkExperienceReferenceToken row, DateTime now)
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
}

public sealed record ReferenceVerificationListRow(
    Guid Id,
    Guid WorkExperienceEntryId,
    string EmployerName,
    string RoleTitle,
    string DisplayStatus,
    string ReferentName,
    string Relation,
    DateTime? CompletedAtUtc,
    DateTime? SentAtUtc,
    DateTime? ExpiresAtUtc,
    int? DaysRemaining,
    bool Active,
    int EndorsedCount);

public sealed record ReferenceVerificationPublicView(
    string State,
    string CandidateName,
    string EmployerName,
    string RoleTitle,
    string? StartMonth,
    string? EndMonth,
    bool IsCurrent,
    string? DutiesText,
    IReadOnlyList<ReferenceVerificationCompetenceOffer> Competences,
    bool AlreadyConfirmed);

public sealed record ReferenceVerificationCompetenceOffer(string Code, string Label, string? Description);

public sealed record VerifiedCompetenceFact(string Code, string Label, string RecognitionLine, DateTime CompletedAtUtc);

public sealed record VerifiedWorkExperienceFact(Guid WorkExperienceEntryId, string EmployerName, DateTime? VerifiedAtUtc);
