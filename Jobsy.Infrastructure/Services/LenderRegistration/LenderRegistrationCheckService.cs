using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LenderRegistrationEntity = Jobsy.Core.Entities.LenderRegistration;

namespace Jobsy.Infrastructure.Services.LenderRegistration;

public sealed class LenderRegistrationCheckService : ILenderRegistrationCheck
{
    private readonly JobsyDbContext _db;
    private readonly IEnumerable<ILenderRegistrationProvider> _providers;
    private readonly IUserNotificationService _notifications;
    private readonly ILogger<LenderRegistrationCheckService> _logger;

    public LenderRegistrationCheckService(
        JobsyDbContext db,
        IEnumerable<ILenderRegistrationProvider> providers,
        IUserNotificationService notifications,
        ILogger<LenderRegistrationCheckService> logger)
    {
        _db = db;
        _providers = providers;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<LenderRegistrationState> GetStateAsync(
        Guid bureauOrgId,
        CancellationToken cancellationToken = default)
    {
        await EnsureBackfillAsync(cancellationToken);

        var latest = await LatestAsync(bureauOrgId, cancellationToken);
        if (latest is null)
        {
            return EmptyState(bureauOrgId, LenderRegistrationStatuses.NotChecked);
        }

        return ToState(latest);
    }

    public async Task<LenderRegistrationState> StartForNewBureauAsync(
        Guid bureauOrgId,
        string kvkNumber,
        CancellationToken cancellationToken = default)
    {
        if (bureauOrgId == Guid.Empty)
        {
            throw new ArgumentException("Bureau company id is required.", nameof(bureauOrgId));
        }

        var digits = new string((kvkNumber ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            throw new ArgumentException("KvK number is required.", nameof(kvkNumber));
        }

        // Idempotent: if a Pending/Verified/Rejected row already exists, don't duplicate Start.
        var existing = await LatestAsync(bureauOrgId, cancellationToken);
        if (existing is not null
            && existing.Status is LenderRegistrationStatuses.Pending
                or LenderRegistrationStatuses.Verified
                or LenderRegistrationStatuses.Rejected)
        {
            return ToState(existing);
        }

        string? deepLink = null;
        string? note = null;
        foreach (var provider in _providers.Where(p => p.IsEnabled))
        {
            var result = await provider.CheckAsync(digits, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.DeepLink))
            {
                deepLink = result.DeepLink;
            }

            if (!string.IsNullOrWhiteSpace(result.Note))
            {
                note = result.Note;
            }
        }

        deepLink ??= WaadiKvkProvider.BuildDeepLink(digits);

        var row = new LenderRegistrationEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = bureauOrgId,
            Status = LenderRegistrationStatuses.Pending,
            Source = LenderRegistrationSources.WaadiKvk,
            Reference = null,
            CheckedAtUtc = null,
            Note = note,
            CreatedAtUtc = DateTime.UtcNow
        };
        // Store deep link in Note prefix when no admin note yet — also exposed via GetState WaadiCheckUrl.
        row.Note = string.IsNullOrWhiteSpace(note)
            ? $"waadi_url:{deepLink}"
            : $"{note} | waadi_url:{deepLink}";

        _db.LenderRegistrations.Add(row);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "lender_registration.start",
            Message = $"bureau={bureauOrgId};kvk=set;status=Pending",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Started lender registration check for bureau {BureauId}", bureauOrgId);
        return ToState(row, deepLink);
    }

    public async Task RecordDecisionAsync(
        Guid bureauOrgId,
        LenderRegistrationDecision decision,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (!decision.Approve && string.IsNullOrWhiteSpace(decision.Note))
        {
            throw new ArgumentException("Afwijzen vereist een notitie.");
        }

        var source = string.IsNullOrWhiteSpace(decision.Source)
            ? LenderRegistrationSources.AdminManual
            : decision.Source.Trim();
        if (source is not (LenderRegistrationSources.WaadiKvk
            or LenderRegistrationSources.WttaNau
            or LenderRegistrationSources.AdminManual))
        {
            throw new ArgumentException("Ongeldige bron.");
        }

        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == bureauOrgId, cancellationToken)
            ?? throw new KeyNotFoundException("Bureau niet gevonden.");

        var previous = await LatestAsync(bureauOrgId, cancellationToken);
        var deepLink = ExtractWaadiUrl(previous?.Note)
            ?? WaadiKvkProvider.BuildDeepLink(company.KvkNumber);

        var status = decision.Approve
            ? LenderRegistrationStatuses.Verified
            : LenderRegistrationStatuses.Rejected;

        var row = new LenderRegistrationEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = bureauOrgId,
            Status = status,
            Source = source,
            Reference = string.IsNullOrWhiteSpace(decision.Reference) ? null : decision.Reference.Trim(),
            CheckedAtUtc = DateTime.UtcNow,
            DecidedByUserId = adminUserId,
            Note = BuildNote(decision.Note, deepLink),
            ValidUntil = decision.Approve ? decision.ValidUntil : null,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.LenderRegistrations.Add(row);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = decision.Approve
                ? "admin.lender_registration.approve"
                : "admin.lender_registration.reject",
            Message = $"bureau={bureauOrgId};admin={adminUserId};source={source};status={status}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        await NotifyBureauUsersAsync(bureauOrgId, decision.Approve, decision.Note, cancellationToken);
        _logger.LogInformation(
            "Lender registration {Status} for bureau {BureauId} by admin {Admin}",
            status,
            bureauOrgId,
            adminUserId);
    }

    public bool CanPublish(LenderRegistrationState state)
    {
        if (!string.Equals(state.Status, LenderRegistrationStatuses.Verified, StringComparison.Ordinal))
        {
            return false;
        }

        if (state.ValidUntil is DateTime until && until.ToUniversalTime() < DateTime.UtcNow)
        {
            return false;
        }

        return true;
    }

    private async Task NotifyBureauUsersAsync(
        Guid bureauOrgId,
        bool approved,
        string? note,
        CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive
                        && u.CompanyId == bureauOrgId
                        && u.Role == UserRole.Intermediary)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var title = approved
            ? "Uitleenregistratie bevestigd"
            : "Uitleenregistratie afgewezen";
        var body = approved
            ? "Lobsy heeft je uitleenregistratie (Waadi) bevestigd. Je kunt nu vacatures publiceren."
            : $"Lobsy heeft je uitleenregistratie afgewezen. {(string.IsNullOrWhiteSpace(note) ? "" : note)}".Trim();

        foreach (var userId in users)
        {
            await _notifications.CreateAsync(
                new NotificationCreateRequest(
                    userId,
                    title,
                    body,
                    "lender_registration",
                    DeepLink: "/home",
                    RelatedEntityType: "Company",
                    RelatedEntityId: bureauOrgId),
                cancellationToken);
        }
    }

    private async Task EnsureBackfillAsync(CancellationToken cancellationToken)
    {
        // One-shot: existing intermediary companies without any row get NotChecked
        // (does not block already-live vacancies; gate applies to new publications).
        var missingIds = await _db.Companies
            .AsNoTracking()
            .Where(c => c.Type == CompanyType.Intermediary
                        && !_db.LenderRegistrations.Any(r => r.CompanyId == c.Id))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (missingIds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var id in missingIds)
        {
            _db.LenderRegistrations.Add(new LenderRegistrationEntity
            {
                Id = Guid.NewGuid(),
                CompanyId = id,
                Status = LenderRegistrationStatuses.NotChecked,
                Source = null,
                CreatedAtUtc = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Backfilled NotChecked lender registration for {Count} bureaus", missingIds.Count);
    }

    private Task<LenderRegistrationEntity?> LatestAsync(Guid bureauOrgId, CancellationToken cancellationToken)
        => _db.LenderRegistrations
            .AsNoTracking()
            .Where(r => r.CompanyId == bureauOrgId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private static LenderRegistrationState EmptyState(Guid bureauOrgId, string status)
        => new(bureauOrgId, status, null, null, null, null, null, null);

    private static LenderRegistrationState ToState(LenderRegistrationEntity row, string? deepLinkOverride = null)
    {
        var url = deepLinkOverride ?? ExtractWaadiUrl(row.Note);
        return new LenderRegistrationState(
            row.CompanyId,
            row.Status,
            row.Source,
            row.Reference,
            row.CheckedAtUtc,
            row.ValidUntil,
            StripWaadiUrl(row.Note),
            url);
    }

    private static string? ExtractWaadiUrl(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        const string marker = "waadi_url:";
        var idx = note.LastIndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
        {
            return null;
        }

        return note[(idx + marker.Length)..].Trim();
    }

    private static string? StripWaadiUrl(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        const string marker = " | waadi_url:";
        var idx = note.LastIndexOf(marker, StringComparison.Ordinal);
        if (idx >= 0)
        {
            return note[..idx].Trim();
        }

        if (note.StartsWith("waadi_url:", StringComparison.Ordinal))
        {
            return null;
        }

        return note;
    }

    private static string BuildNote(string? note, string deepLink)
    {
        var clean = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        return clean is null ? $"waadi_url:{deepLink}" : $"{clean} | waadi_url:{deepLink}";
    }
}
