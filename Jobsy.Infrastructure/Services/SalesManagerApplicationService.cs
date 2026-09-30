using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Core.Security;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Jobsy.Core;

namespace Jobsy.Infrastructure.Services;

public sealed class SalesManagerApplicationService : ISalesManagerApplicationService
{
    private const int MinMotivationLength = 10;
    private const int MaxMotivationLength = 500;
    public const string ObjectionReason = "Bezwaar";

    private readonly JobsyDbContext _db;
    private readonly ISalesManagerInviteService _invite;
    private readonly IEmailService _email;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<SalesManagerApplicationService> _logger;

    public SalesManagerApplicationService(
        JobsyDbContext db,
        ISalesManagerInviteService invite,
        IEmailService email,
        IPlatformFeatureService features,
        ILogger<SalesManagerApplicationService> logger)
    {
        _db = db;
        _invite = invite;
        _email = email;
        _features = features;
        _logger = logger;
    }

    public async Task<SalesManagerApplicationDto> SubmitAsync(
        Guid referrerSalesManagerUserId,
        string candidateEmail,
        string candidateFullName,
        string motivation,
        bool referrerConfirmedPermission,
        CancellationToken cancellationToken = default)
    {
        if (!referrerConfirmedPermission)
        {
            throw new ArgumentException(
                "Bevestig dat deze persoon weet dat je hem of haar aanmeldt.");
        }

        if (string.IsNullOrWhiteSpace(candidateEmail)
            || string.IsNullOrWhiteSpace(candidateFullName)
            || string.IsNullOrWhiteSpace(motivation))
        {
            throw new ArgumentException("Naam, e-mail en motivatie zijn verplicht.");
        }

        var email = candidateEmail.Trim().ToLowerInvariant();
        var name = candidateFullName.Trim();
        var motive = motivation.Trim();
        if (motive.Length < MinMotivationLength)
        {
            throw new ArgumentException($"Motivatie moet minimaal {MinMotivationLength} tekens zijn.");
        }

        if (motive.Length > MaxMotivationLength)
        {
            throw new ArgumentException($"Motivatie mag maximaal {MaxMotivationLength} tekens zijn.");
        }

        var referrerUser = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Id == referrerSalesManagerUserId && u.Role == UserRole.SalesManager && u.IsActive,
                cancellationToken)
            ?? throw new InvalidOperationException("Salesmanager niet gevonden.");

        var referrerProfile = await _db.SalesManagerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == referrerSalesManagerUserId, cancellationToken)
            ?? throw new InvalidOperationException("Salesmanager-profiel niet gevonden.");

        if (!referrerProfile.IsOnboardingComplete || string.IsNullOrWhiteSpace(referrerProfile.TrackingCode))
        {
            throw new InvalidOperationException(
                "Rond eerst je onboarding af (trackingcode) voordat je een salesmanager kunt aanbevelen.");
        }

        if (!referrerProfile.CanRecruitSalesManagers || referrerProfile.ReferredBySalesManagerUserId is not null)
        {
            throw new InvalidOperationException(
                "Je kunt geen nieuwe salesmanagers aanbevelen. Alleen door Admin aangemaakte salesmanagers mogen werven (één laag).");
        }

        if (string.Equals(email, referrerUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Je kunt jezelf niet aanbevelen.");
        }

        var existingUser = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
        if (existingUser is not null && existingUser.Role == UserRole.SalesManager)
        {
            throw new InvalidOperationException("Deze persoon is al salesmanager.");
        }

        var emailSha = HashEmail(email);
        var duplicateCutoff = DateTime.UtcNow.AddDays(-PrivacyConstants.SalesManagerApplicationPendingRetentionDays);
        var duplicateExists = await _db.SalesManagerApplications.AnyAsync(
            a => a.CreatedAtUtc >= duplicateCutoff
                 && (a.CandidateEmail == email
                     || a.CandidateEmailSha256 == emailSha),
            cancellationToken);
        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "Er staat al een aanbeveling voor dit e-mailadres binnen 60 dagen.");
        }

        var plaintextToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var now = DateTime.UtcNow;
        var entity = new SalesManagerApplication
        {
            Id = Guid.NewGuid(),
            ReferrerSalesManagerUserId = referrerSalesManagerUserId,
            ReferrerTrackingCode = referrerProfile.TrackingCode!,
            CandidateEmail = email,
            CandidateFullName = name,
            Motivation = motive,
            Status = SalesManagerApplicationStatus.Pending,
            CreatedAtUtc = now,
            ReferrerConfirmedPermission = true,
            CandidateEmailSha256 = emailSha,
            ObjectionTokenHash = VerificationCodes.Hash(plaintextToken)
        };
        _db.SalesManagerApplications.Add(entity);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "SalesManagerApplication",
            Message =
                $"Pending SM recommendation via {referrerProfile.TrackingCode} for {EmailServiceStub.RedactEmail(email)}",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        await SendRecommendedNoticeAsync(
            entity,
            referrerUser.FullName,
            plaintextToken,
            cancellationToken);

        entity.SubjectNotifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Salesmanager application {ApplicationId} submitted by {ReferrerId}",
            entity.Id,
            referrerSalesManagerUserId);

        return await MapAsync(entity, includeCandidateEmail: false, cancellationToken);
    }

    public async Task<SalesRecommendOverviewDto> GetRecommendOverviewAsync(
        Guid referrerSalesManagerUserId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.SalesManagerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == referrerSalesManagerUserId, cancellationToken);
        var settings = await _db.SalesCommercialSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        var indirectRate = settings?.IndirectCommissionRate ?? SalesCommissionRules.DefaultIndirectCommissionRate;
        var referredYear1 = settings?.ReferredYear1DirectCommissionRate
            ?? SalesCommissionRules.DefaultReferredYear1DirectCommissionRate;

        var earned = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => e.SalesManagerUserId == referrerSalesManagerUserId
                        && e.Kind == CommissionEntryKind.IndirectTokenCommission)
            .SumAsync(e => (decimal?)e.AmountExVat, cancellationToken) ?? 0m;

        var rows = await _db.SalesManagerApplications.AsNoTracking()
            .Where(a => a.ReferrerSalesManagerUserId == referrerSalesManagerUserId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var items = rows.Select(a =>
        {
            var cleared = a.PersonalDataClearedAtUtc is not null
                          || string.IsNullOrWhiteSpace(a.CandidateFullName);
            return new SalesRecommendListItemDto(
                a.Id,
                a.CreatedAtUtc,
                cleared ? "" : a.CandidateFullName,
                a.Status.ToString(),
                SalesLabels.ApplicationStatusKey(a.Status, a.SubjectObjectedAtUtc),
                a.RejectionReason,
                cleared);
        }).ToList();

        return new SalesRecommendOverviewDto(
            Math.Round(indirectRate * 100m, 0, MidpointRounding.AwayFromZero),
            Math.Round(referredYear1 * 100m, 0, MidpointRounding.AwayFromZero),
            earned,
            profile?.CanRecruitSalesManagers == true && profile.ReferredBySalesManagerUserId is null,
            profile?.IsOnboardingComplete == true,
            profile?.TrackingCode,
            items);
    }

    public async Task<IReadOnlyList<SalesManagerApplicationDto>> ListMineAsync(
        Guid referrerSalesManagerUserId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.SalesManagerApplications.AsNoTracking()
            .Where(a => a.ReferrerSalesManagerUserId == referrerSalesManagerUserId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var result = new List<SalesManagerApplicationDto>(rows.Count);
        foreach (var row in rows)
        {
            result.Add(await MapAsync(row, includeCandidateEmail: false, cancellationToken));
        }

        return result;
    }

    public async Task<IReadOnlyList<SalesManagerApplicationDto>> ListPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.SalesManagerApplications.AsNoTracking()
            .Where(a => a.Status == SalesManagerApplicationStatus.Pending)
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var result = new List<SalesManagerApplicationDto>(rows.Count);
        foreach (var row in rows)
        {
            result.Add(await MapAsync(row, includeCandidateEmail: true, cancellationToken));
        }

        return result;
    }

    public async Task<IReadOnlyList<SalesManagerApplicationDto>> ListAllAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.SalesManagerApplications.AsNoTracking()
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var result = new List<SalesManagerApplicationDto>(rows.Count);
        foreach (var row in rows)
        {
            result.Add(await MapAsync(row, includeCandidateEmail: true, cancellationToken));
        }

        return result;
    }

    public async Task<SalesManagerApplicationDto> ApproveAsync(
        Guid applicationId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var application = await _db.SalesManagerApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            ?? throw new KeyNotFoundException("Aanbeveling niet gevonden.");

        if (application.Status != SalesManagerApplicationStatus.Pending)
        {
            throw new InvalidOperationException("Deze aanbeveling is al beoordeeld.");
        }

        if (application.PersonalDataClearedAtUtc is not null
            || string.IsNullOrWhiteSpace(application.CandidateEmail))
        {
            throw new InvalidOperationException("Persoonsgegevens zijn al verwijderd; goedkeuren is niet meer mogelijk.");
        }

        var invite = await _invite.InviteAsync(
            application.CandidateEmail,
            application.CandidateFullName,
            referredBySalesManagerUserId: application.ReferrerSalesManagerUserId,
            cancellationToken);

        application.Status = SalesManagerApplicationStatus.Approved;
        application.ReviewedAtUtc = DateTime.UtcNow;
        application.ReviewedByAdminUserId = adminUserId;
        application.ProvisionedUserId = invite.UserId;
        application.RejectionReason = null;
        application.ObjectionTokenHash = null;

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "SalesManagerApplication",
            Message =
                $"Approved SM recommendation {applicationId:N} → user {invite.UserId:N} ({EmailServiceStub.RedactEmail(application.CandidateEmail)})",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(application, includeCandidateEmail: true, cancellationToken);
    }

    public async Task<SalesManagerApplicationDto> RejectAsync(
        Guid applicationId,
        Guid adminUserId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var application = await _db.SalesManagerApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken)
            ?? throw new KeyNotFoundException("Aanbeveling niet gevonden.");

        if (application.Status != SalesManagerApplicationStatus.Pending)
        {
            throw new InvalidOperationException("Deze aanbeveling is al beoordeeld.");
        }

        application.Status = SalesManagerApplicationStatus.Rejected;
        application.ReviewedAtUtc = DateTime.UtcNow;
        application.ReviewedByAdminUserId = adminUserId;
        application.RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        application.ObjectionTokenHash = null;

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "SalesManagerApplication",
            Message =
                $"Rejected SM recommendation {applicationId:N} ({EmailServiceStub.RedactEmail(application.CandidateEmail)})",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(application, includeCandidateEmail: true, cancellationToken);
    }

    public async Task<bool> ObjectByTokenAsync(string plaintextToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(plaintextToken))
        {
            return false;
        }

        var hash = VerificationCodes.Hash(plaintextToken.Trim());
        var application = await _db.SalesManagerApplications
            .FirstOrDefaultAsync(a => a.ObjectionTokenHash == hash, cancellationToken);
        if (application is null)
        {
            return false;
        }

        if (application.SubjectObjectedAtUtc is not null
            || application.PersonalDataClearedAtUtc is not null
            || application.Status != SalesManagerApplicationStatus.Pending)
        {
            // Single-use: already handled.
            return false;
        }

        var now = DateTime.UtcNow;
        application.SubjectObjectedAtUtc = now;
        application.Status = SalesManagerApplicationStatus.Rejected;
        application.ReviewedAtUtc = now;
        application.RejectionReason = ObjectionReason;
        application.ObjectionTokenHash = null;
        ClearPersonalData(application, now);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "SalesManagerApplication",
            Message = $"Subject objected to SM recommendation {application.Id:N}; PII cleared",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>AVG retention (D13): expire pending / clear PII on rejected &amp; approved. Testable with fixed clock.</summary>
    public static async Task<int> ApplyRetentionAsync(
        JobsyDbContext db,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var changed = 0;

        var pendingCutoff = utcNow.AddDays(-PrivacyConstants.SalesManagerApplicationPendingRetentionDays);
        var pending = await db.SalesManagerApplications
            .Where(a => a.Status == SalesManagerApplicationStatus.Pending
                        && a.CreatedAtUtc < pendingCutoff
                        && a.PersonalDataClearedAtUtc == null)
            .Take(200)
            .ToListAsync(cancellationToken);
        foreach (var row in pending)
        {
            row.Status = SalesManagerApplicationStatus.Expired;
            row.ReviewedAtUtc ??= utcNow;
            row.RejectionReason ??= "Verlopen";
            row.ObjectionTokenHash = null;
            ClearPersonalData(row, utcNow);
            changed++;
        }

        var rejectedCutoff = utcNow.AddDays(-PrivacyConstants.SalesManagerApplicationRejectedRetentionDays);
        var rejected = await db.SalesManagerApplications
            .Where(a => a.Status == SalesManagerApplicationStatus.Rejected
                        && a.ReviewedAtUtc != null
                        && a.ReviewedAtUtc < rejectedCutoff
                        && a.PersonalDataClearedAtUtc == null)
            .Take(200)
            .ToListAsync(cancellationToken);
        foreach (var row in rejected)
        {
            ClearPersonalData(row, utcNow);
            changed++;
        }

        var approvedCutoff = utcNow.AddDays(-PrivacyConstants.SalesManagerApplicationApprovedRetentionDays);
        var approved = await db.SalesManagerApplications
            .Where(a => a.Status == SalesManagerApplicationStatus.Approved
                        && a.ProvisionedUserId != null
                        && a.ReviewedAtUtc != null
                        && a.ReviewedAtUtc < approvedCutoff
                        && a.PersonalDataClearedAtUtc == null)
            .Take(200)
            .ToListAsync(cancellationToken);
        foreach (var row in approved)
        {
            ClearPersonalData(row, utcNow);
            changed++;
        }

        if (changed > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }

    public static void ClearPersonalData(SalesManagerApplication application, DateTime utcNow)
    {
        application.CandidateFullName = "";
        application.CandidateEmail = "";
        application.Motivation = "";
        application.ObjectionTokenHash = null;
        application.PersonalDataClearedAtUtc = utcNow;
        // CandidateEmailSha256 kept for the 60-day duplicate rule.
    }

    public static string HashEmail(string normalizedEmail)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private async Task SendRecommendedNoticeAsync(
        SalesManagerApplication application,
        string referrerDisplayName,
        string plaintextToken,
        CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        var baseUrl = JobsyPublicUrl.NormalizeOrigin(features.PublicWebBaseUrl).TrimEnd('/');
        var link = $"{baseUrl}/sales/aanbevelen/bezwaar?token={Uri.EscapeDataString(plaintextToken)}";
        var subject = "Iemand heeft je aanbevolen bij Lobsy";
        var html =
            $"<p>Hallo {WebUtility.HtmlEncode(application.CandidateFullName)},</p>"
            + $"<p><strong>{WebUtility.HtmlEncode(referrerDisplayName)}</strong> heeft je aanbevolen als salesmanager bij Lobsy.</p>"
            + "<p>Lobsy bewaart je naam, e-mailadres en de motivatie van de aanbeveler. "
            + "Openstaande aanbevelingen wissen we na 60 dagen; bij afwijzing of goedkeuring na 30 dagen.</p>"
            + "<p>Wil je dit niet? Verwijder je gegevens via deze link (60 dagen geldig, eenmalig):</p>"
            + $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Ik wil dit niet – verwijder mijn gegevens</a></p>"
            + "<p>Heb je vragen? Mail privacy@lobsy.nl.</p>";

        await _email.SendAsync(
            new EmailMessage(application.CandidateEmail, subject, html, "SalesMail.RecommendedNotice"),
            cancellationToken);
    }

    private async Task<SalesManagerApplicationDto> MapAsync(
        SalesManagerApplication application,
        bool includeCandidateEmail,
        CancellationToken cancellationToken)
    {
        var referrer = await _db.Users.AsNoTracking()
            .Where(u => u.Id == application.ReferrerSalesManagerUserId)
            .Select(u => new { u.FullName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        var cleared = application.PersonalDataClearedAtUtc is not null
                      || string.IsNullOrWhiteSpace(application.CandidateFullName);
        var displayName = cleared ? "" : application.CandidateFullName;
        var email = includeCandidateEmail && !cleared ? application.CandidateEmail : "";
        var motivation = cleared ? "" : application.Motivation;

        return new SalesManagerApplicationDto(
            application.Id,
            application.ReferrerSalesManagerUserId,
            referrer?.FullName ?? "—",
            referrer?.Email ?? "—",
            application.ReferrerTrackingCode,
            email,
            displayName,
            motivation,
            application.Status.ToString(),
            application.CreatedAtUtc,
            application.ReviewedAtUtc,
            application.ProvisionedUserId,
            application.RejectionReason,
            application.SubjectNotifiedAtUtc,
            application.SubjectObjectedAtUtc,
            application.PersonalDataClearedAtUtc,
            application.ReferrerConfirmedPermission,
            SalesLabels.ApplicationStatusKey(application.Status, application.SubjectObjectedAtUtc));
    }
}
