using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserRole = Jobsy.Core.Enums.UserRole;

namespace Jobsy.Infrastructure.Services;

public sealed class SupportAccessService : ISupportAccessService
{
    public const int MinReasonLength = 15;
    public const int DefaultDurationMinutes = 15;
    public const int MaxDurationMinutes = 240;
    private static readonly int[] AllowedDurations = [15, 60, 240];

    private readonly JobsyDbContext _db;
    private readonly TimeProvider _clock;
    private readonly ILogger<SupportAccessService> _logger;
    private readonly IEmailService? _email;
    private readonly IPlatformFeatureService _features;

    public SupportAccessService(
        JobsyDbContext db,
        TimeProvider clock,
        ILogger<SupportAccessService> logger,
        IPlatformFeatureService features,
        IEmailService? email = null)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
        _features = features;
        _email = email;
    }

    public async Task<SupportAccessGrantDto> RequestAsync(
        Guid adminUserId,
        SupportAccessRequest request,
        bool mfaVerifiedInSession,
        string? authMethod = null,
        CancellationToken cancellationToken = default)
    {
        if (request.SubjectUserId is null && request.SubjectCompanyId is null)
        {
            throw new InvalidOperationException("Kies een subject (gebruiker of bedrijf).");
        }

        if (request.Scope == SupportAccessScope.None)
        {
            throw new InvalidOperationException("Kies minstens één scope.");
        }

        var reason = (request.Reason ?? "").Trim();
        if (reason.Length < MinReasonLength)
        {
            throw new InvalidOperationException($"Reden moet minstens {MinReasonLength} tekens zijn.");
        }

        var duration = AllowedDurations.Contains(request.DurationMinutes)
            ? request.DurationMinutes
            : DefaultDurationMinutes;
        duration = Math.Clamp(duration, 15, MaxDurationMinutes);

        var admin = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == adminUserId, cancellationToken)
            ?? throw new InvalidOperationException("Admin niet gevonden.");

        // Require MFA step-up when the admin has TOTP enrolled, unless this session came from an IdP.
        if (admin.AuthenticatorEnabled && !mfaVerifiedInSession && !IsExternalAuthMethod(authMethod))
        {
            throw new InvalidOperationException(
                "Bevestig eerst MFA (stap-up) voordat je tijdelijke toegang aanvraagt.");
        }

        if (request.SubjectUserId is Guid sid
            && !await _db.Users.AnyAsync(u => u.Id == sid, cancellationToken))
        {
            throw new InvalidOperationException("Subject-gebruiker niet gevonden.");
        }

        if (request.SubjectCompanyId is Guid cid
            && !await _db.Companies.AnyAsync(c => c.Id == cid, cancellationToken))
        {
            throw new InvalidOperationException("Subject-bedrijf niet gevonden.");
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var grant = new SupportAccessGrant
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            SubjectUserId = request.SubjectUserId,
            SubjectCompanyId = request.SubjectCompanyId,
            Scope = request.Scope,
            Reason = reason,
            TicketReference = string.IsNullOrWhiteSpace(request.TicketReference)
                ? null
                : request.TicketReference.Trim(),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(duration)
        };

        _db.SupportAccessGrants.Add(grant);
        await _db.SaveChangesAsync(cancellationToken);

        await MaybeNotifyAdminsAsync(grant, admin.Email, cancellationToken);

        return ToDto(grant, now);
    }

    public async Task<bool> HasActiveGrantAsync(
        Guid adminUserId,
        Guid? subjectUserId,
        Guid? subjectCompanyId,
        SupportAccessScope requiredScope,
        CancellationToken cancellationToken = default)
        => await FindActiveGrantIdAsync(
               adminUserId, subjectUserId, subjectCompanyId, requiredScope, cancellationToken)
           is not null;

    public async Task<Guid?> FindActiveGrantIdAsync(
        Guid adminUserId,
        Guid? subjectUserId,
        Guid? subjectCompanyId,
        SupportAccessScope requiredScope,
        CancellationToken cancellationToken = default)
    {
        if (requiredScope == SupportAccessScope.None)
        {
            return null;
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var query = _db.SupportAccessGrants.AsNoTracking()
            .Where(g => g.AdminUserId == adminUserId
                        && g.RevokedAt == null
                        && g.ExpiresAt > now
                        && (g.Scope & requiredScope) == requiredScope);

        if (subjectUserId is Guid sid)
        {
            query = query.Where(g => g.SubjectUserId == sid);
        }
        else if (subjectCompanyId is Guid cid)
        {
            query = query.Where(g => g.SubjectCompanyId == cid);
        }
        else
        {
            return null;
        }

        var id = await query
            .OrderByDescending(g => g.ExpiresAt)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return id;
    }

    public async Task RevokeAsync(
        Guid grantId,
        Guid revokedByUserId,
        CancellationToken cancellationToken = default)
    {
        var grant = await _db.SupportAccessGrants
            .FirstOrDefaultAsync(g => g.Id == grantId, cancellationToken)
            ?? throw new KeyNotFoundException("Toegang niet gevonden.");

        if (grant.RevokedAt is not null)
        {
            return;
        }

        grant.RevokedAt = _clock.GetUtcNow().UtcDateTime;
        grant.RevokedByUserId = revokedByUserId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SupportAccessGrantDto>> ListRecentAsync(
        int take = 50,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        var now = _clock.GetUtcNow().UtcDateTime;
        var query = _db.SupportAccessGrants.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            query = query.Where(g => g.RevokedAt == null && g.ExpiresAt > now);
        }

        var rows = await query
            .OrderByDescending(g => g.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
        return rows.Select(g => ToDto(g, now)).ToList();
    }

    public async Task<IReadOnlyList<DateTime>> ListAccessDatesForSubjectAsync(
        Guid subjectUserId,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 50);
        return await _db.SupportAccessGrants.AsNoTracking()
            .Where(g => g.SubjectUserId == subjectUserId)
            .OrderByDescending(g => g.CreatedAt)
            .Take(take)
            .Select(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private async Task MaybeNotifyAdminsAsync(
        SupportAccessGrant grant,
        string adminEmail,
        CancellationToken cancellationToken)
    {
        try
        {
            var features = await _features.GetAsync(cancellationToken);
            if (!features.SupportAccessNotifyAdmins || _email is null)
            {
                return;
            }

            var otherAdmins = await _db.Users.AsNoTracking()
                .Where(u => u.Role == UserRole.Admin && u.IsActive && u.Id != grant.AdminUserId)
                .Select(u => u.Email)
                .Take(20)
                .ToListAsync(cancellationToken);

            foreach (var to in otherAdmins)
            {
                var mail = TransactionalEmails.SupportAccessRequested(
                    features.PublicWebBaseUrl,
                    EmailServiceStub.RedactEmail(adminEmail),
                    grant.Reason,
                    grant.ExpiresAt,
                    grant.Scope.ToString());
                await _email.SendAsync(
                    new EmailMessage(to, mail.Subject, mail.Html, mail.Category),
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify admins about support access grant {GrantId}", grant.Id);
        }
    }

    private static SupportAccessGrantDto ToDto(SupportAccessGrant g, DateTime nowUtc)
        => new(
            g.Id,
            g.AdminUserId,
            g.SubjectUserId,
            g.SubjectCompanyId,
            g.Scope,
            g.Reason,
            g.TicketReference,
            g.CreatedAt,
            g.ExpiresAt,
            g.RevokedAt,
            g.RevokedByUserId,
            IsActive: g.RevokedAt is null && g.ExpiresAt > nowUtc);

    internal static bool IsExternalAuthMethod(string? authMethod)
        => !string.IsNullOrWhiteSpace(authMethod)
           && authMethod.StartsWith("external", StringComparison.OrdinalIgnoreCase);
}
