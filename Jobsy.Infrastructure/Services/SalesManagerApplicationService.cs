using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class SalesManagerApplicationService : ISalesManagerApplicationService
{
    private const int MinMotivationLength = 10;
    private const int MaxMotivationLength = 1000;

    private readonly JobsyDbContext _db;
    private readonly ISalesManagerInviteService _invite;
    private readonly ILogger<SalesManagerApplicationService> _logger;

    public SalesManagerApplicationService(
        JobsyDbContext db,
        ISalesManagerInviteService invite,
        ILogger<SalesManagerApplicationService> logger)
    {
        _db = db;
        _invite = invite;
        _logger = logger;
    }

    public async Task<SalesManagerApplicationDto> SubmitAsync(
        Guid referrerSalesManagerUserId,
        string candidateEmail,
        string candidateFullName,
        string motivation,
        CancellationToken cancellationToken = default)
    {
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

        var pendingExists = await _db.SalesManagerApplications.AnyAsync(
            a => a.CandidateEmail == email && a.Status == SalesManagerApplicationStatus.Pending,
            cancellationToken);
        if (pendingExists)
        {
            throw new InvalidOperationException("Er staat al een openstaande aanbeveling voor dit e-mailadres.");
        }

        var entity = new SalesManagerApplication
        {
            Id = Guid.NewGuid(),
            ReferrerSalesManagerUserId = referrerSalesManagerUserId,
            ReferrerTrackingCode = referrerProfile.TrackingCode!,
            CandidateEmail = email,
            CandidateFullName = name,
            Motivation = motive,
            Status = SalesManagerApplicationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.SalesManagerApplications.Add(entity);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "SalesManagerApplication",
            Message =
                $"Pending SM recommendation via {referrerProfile.TrackingCode} for {EmailServiceStub.RedactEmail(email)}",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Salesmanager application {ApplicationId} submitted by {ReferrerId}",
            entity.Id,
            referrerSalesManagerUserId);

        return await MapAsync(entity, temporaryPassword: null, cancellationToken);
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
            result.Add(await MapAsync(row, null, cancellationToken));
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
            result.Add(await MapAsync(row, null, cancellationToken));
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
            result.Add(await MapAsync(row, null, cancellationToken));
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
        return await MapAsync(application, invite.TemporaryPassword, cancellationToken);
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
        return await MapAsync(application, null, cancellationToken);
    }

    private async Task<SalesManagerApplicationDto> MapAsync(
        SalesManagerApplication application,
        string? temporaryPassword,
        CancellationToken cancellationToken)
    {
        var referrer = await _db.Users.AsNoTracking()
            .Where(u => u.Id == application.ReferrerSalesManagerUserId)
            .Select(u => new { u.FullName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        return new SalesManagerApplicationDto(
            application.Id,
            application.ReferrerSalesManagerUserId,
            referrer?.FullName ?? "—",
            referrer?.Email ?? "—",
            application.ReferrerTrackingCode,
            application.CandidateEmail,
            application.CandidateFullName,
            application.Motivation,
            application.Status.ToString(),
            application.CreatedAtUtc,
            application.ReviewedAtUtc,
            application.ProvisionedUserId,
            application.RejectionReason,
            temporaryPassword);
    }
}
