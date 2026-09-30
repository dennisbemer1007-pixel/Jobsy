using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Single place that verifies a company and runs the post-verify pipeline (D4 / 03.5).
/// </summary>
public sealed class CompanyVerificationService : ICompanyVerificationService
{
    private readonly JobsyDbContext _db;
    private readonly CompanyRegistrationService _registration;
    private readonly IVacancyProductService _products;
    private readonly IVacancyDiscoveryIndex? _discovery;
    private readonly ITransactionalMailer _mailer;
    private readonly IUserNotificationService _notifications;
    private readonly IPlatformFeatureService _features;
    private readonly ILogger<CompanyVerificationService> _logger;

    public CompanyVerificationService(
        JobsyDbContext db,
        CompanyRegistrationService registration,
        IVacancyProductService products,
        IVacancyDiscoveryIndex? discovery,
        ITransactionalMailer mailer,
        IUserNotificationService notifications,
        IPlatformFeatureService features,
        ILogger<CompanyVerificationService> logger)
    {
        _db = db;
        _registration = registration;
        _products = products;
        _discovery = discovery;
        _mailer = mailer;
        _notifications = notifications;
        _features = features;
        _logger = logger;
    }

    public async Task MarkVerifiedAsync(
        Guid rootCompanyId,
        CompanyVerificationMethod method,
        Guid? actorUserId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var root = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == rootCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company {rootCompanyId} not found.");

        // Always operate on the organisation root.
        if (root.ParentCompanyId is Guid parentId)
        {
            await MarkVerifiedAsync(parentId, method, actorUserId, note, cancellationToken);
            return;
        }

        if (root.VerificationStatus == CompanyVerificationStatus.Verified)
        {
            // Idempotent: still try welcome + klaar publish in case a previous run stopped early.
            await GrantWelcomeForTreeAsync(root, actorUserId, cancellationToken);
            await PublishReadyVacanciesAsync(root.Id, cancellationToken);
            await (_discovery?.InvalidateCompanyAsync(root.Id, cancellationToken) ?? Task.CompletedTask);
            return;
        }

        var now = DateTime.UtcNow;
        var tree = await LoadTreeAsync(root.Id, cancellationToken);
        foreach (var company in tree)
        {
            if (company.VerificationStatus is CompanyVerificationStatus.Unverified
                or CompanyVerificationStatus.Pending
                or CompanyVerificationStatus.Rejected)
            {
                company.VerificationStatus = CompanyVerificationStatus.Verified;
                company.VerificationMethod = company.Id == root.Id
                    ? method
                    : CompanyVerificationMethod.InheritedFromOrganization;
                company.VerifiedAtUtc ??= now;
                company.VerificationUpdatedAtUtc = now;
                if (company.ManualVerificationOpenedAtUtc is not null
                    && company.ManualVerificationClosedAtUtc is null)
                {
                    company.ManualVerificationClosedAtUtc = now;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var welcomeGranted = await GrantWelcomeForTreeAsync(root, actorUserId, cancellationToken);
        var publishedTitles = await PublishReadyVacanciesAsync(root.Id, cancellationToken);
        root.LastAutoPublishedVacancyCount = publishedTitles.Count;
        await (_discovery?.InvalidateCompanyAsync(root.Id, cancellationToken) ?? Task.CompletedTask);

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "company.verified",
            Message =
                $"company={root.Id};method={method};actor={actorUserId};note={(note is null ? "-" : "set")};published={publishedTitles.Count};welcome={welcomeGranted}",
            CreatedAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);

        await NotifyVerifiedAsync(root, welcomeGranted, publishedTitles, cancellationToken);

        _logger.LogInformation(
            "Company {CompanyId} verified via {Method} (published {Count})",
            root.Id,
            method,
            publishedTitles.Count);
    }

    public async Task MarkPendingAsync(
        Guid rootCompanyId,
        CompanyVerificationMethod method,
        Guid? actorUserId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var root = await ResolveRootAsync(rootCompanyId, cancellationToken);
        if (root.VerificationStatus == CompanyVerificationStatus.Verified)
        {
            return;
        }

        var now = DateTime.UtcNow;
        root.VerificationStatus = CompanyVerificationStatus.Pending;
        root.VerificationMethod = method;
        root.VerificationUpdatedAtUtc = now;
        root.ManualVerificationOpenedAtUtc ??= now;
        root.ManualVerificationClosedAtUtc = null;

        foreach (var child in await LoadChildrenAsync(root.Id, cancellationToken))
        {
            if (child.VerificationStatus == CompanyVerificationStatus.Verified)
            {
                continue;
            }

            child.VerificationStatus = CompanyVerificationStatus.Pending;
            child.VerificationMethod = CompanyVerificationMethod.InheritedFromOrganization;
            child.VerificationUpdatedAtUtc = now;
        }

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "company.verification-pending",
            Message = $"company={root.Id};method={method};actor={actorUserId};note={(note is null ? "-" : "set")}",
            CreatedAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkRejectedAsync(
        Guid rootCompanyId,
        CompanyVerificationMethod method,
        Guid? actorUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var root = await ResolveRootAsync(rootCompanyId, cancellationToken);
        if (root.VerificationStatus == CompanyVerificationStatus.Verified)
        {
            return;
        }

        var now = DateTime.UtcNow;
        root.VerificationStatus = CompanyVerificationStatus.Rejected;
        root.VerificationMethod = method;
        root.VerificationUpdatedAtUtc = now;
        if (root.ManualVerificationOpenedAtUtc is not null)
        {
            root.ManualVerificationClosedAtUtc = now;
        }

        foreach (var child in await LoadChildrenAsync(root.Id, cancellationToken))
        {
            if (child.VerificationStatus == CompanyVerificationStatus.Verified)
            {
                continue;
            }

            child.VerificationStatus = CompanyVerificationStatus.Rejected;
            child.VerificationMethod = CompanyVerificationMethod.InheritedFromOrganization;
            child.VerificationUpdatedAtUtc = now;
        }

        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "company.verification-rejected",
            Message =
                $"company={root.Id};method={method};actor={actorUserId};reasonLen={reason?.Length ?? 0}",
            CreatedAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> GrantWelcomeForTreeAsync(
        Company root,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        // Prefer the primary branch from the registration that created this org; else the root itself.
        var grantTargetId = await _db.CompanyRegistrations.AsNoTracking()
            .Where(r => r.CreatedOrganizationCompanyId == root.Id || r.CreatedBranchCompanyId == root.Id)
            .OrderByDescending(r => r.ActivatedAt)
            .Select(r => r.CreatedBranchCompanyId ?? r.CreatedOrganizationCompanyId)
            .FirstOrDefaultAsync(cancellationToken) ?? root.Id;

        // If any company in the tree already received the welcome, treat as done (idempotent).
        var already = await _db.Companies.AsNoTracking()
            .AnyAsync(
                c => (c.Id == root.Id || c.ParentCompanyId == root.Id) && c.HasReceivedWelcomeToken,
                cancellationToken);
        if (already)
        {
            return false;
        }

        // Ensure root is Verified in memory for the CanUseWelcomeToken check inside GrantWelcomeTokenAsync.
        var target = await _db.Companies.FirstAsync(c => c.Id == grantTargetId, cancellationToken);
        if (target.VerificationStatus != CompanyVerificationStatus.Verified)
        {
            // Parent may be the verified root while branch inherits — reload after MarkVerified save.
            await _db.Entry(target).ReloadAsync(cancellationToken);
        }

        return await _registration.GrantWelcomeTokenAsync(
            grantTargetId,
            actorUserId ?? Guid.Empty,
            cancellationToken);
    }

    private async Task<IReadOnlyList<string>> PublishReadyVacanciesAsync(
        Guid rootCompanyId,
        CancellationToken cancellationToken)
    {
        var companyIds = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == rootCompanyId || c.ParentCompanyId == rootCompanyId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var ready = await _db.Vacancies
            .Include(v => v.Company)
            .Include(v => v.Category)
            .Where(v => companyIds.Contains(v.CompanyId)
                        && v.PublishOnVerification
                        && (v.Status == VacancyStatus.Draft || v.Status == VacancyStatus.PendingApproval))
            .OrderBy(v => v.ReadyMarkedAtUtc ?? v.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (ready.Count == 0)
        {
            return [];
        }

        var published = new List<string>();
        var remainingReady = 0;

        foreach (var vacancy in ready)
        {
            // PendingApproval: BM already approved klaar → treat as Draft for publish.
            if (vacancy.Status == VacancyStatus.PendingApproval)
            {
                vacancy.Status = VacancyStatus.Draft;
                await _db.SaveChangesAsync(cancellationToken);
            }

            var actor = vacancy.ReadyMarkedByUserId;
            if (actor is null)
            {
                actor = await FindBedrijfsmanagerUserIdAsync(vacancy.CompanyId, rootCompanyId, cancellationToken);
            }

            var result = await _products.PublishAsync(
                vacancy,
                new VacancyPublishOptions(
                    vacancy.RequestedHighlight,
                    vacancy.RequestedPushBom,
                    vacancy.RequestedExtend),
                actor,
                cancellationToken,
                allowPendingApproval: false);

            if (result.Succeeded)
            {
                vacancy.PublishOnVerification = false;
                vacancy.ReadyMarkedAtUtc = null;
                vacancy.ReadyMarkedByUserId = null;
                await _db.SaveChangesAsync(cancellationToken);
                published.Add(vacancy.Title);
            }
            else if (result.InsufficientTokens)
            {
                remainingReady++;
            }
            else
            {
                _logger.LogWarning(
                    "Auto-publish of ready vacancy {VacancyId} failed: {Error}",
                    vacancy.Id,
                    result.ErrorMessage);
                remainingReady++;
            }
        }

        if (remainingReady > 0)
        {
            await NotifyBuyTokensForReadyAsync(rootCompanyId, remainingReady, cancellationToken);
        }

        return published;
    }

    private async Task NotifyBuyTokensForReadyAsync(
        Guid rootCompanyId,
        int remainingReady,
        CancellationToken cancellationToken)
    {
        var managers = await LoadManagerContactsAsync(rootCompanyId, cancellationToken);
        var title = "Koop tokens om klaargezette vacatures te publiceren";
        var body =
            $"Koop tokens om {remainingReady} klaargezette vacature(s) te publiceren.";
        foreach (var manager in managers)
        {
            await _notifications.CreateAsync(
                new NotificationCreateRequest(
                    manager.Id,
                    title,
                    body,
                    "CompanyVerification",
                    "/employer/tokens",
                    "Tokens kopen",
                    "/employer/tokens",
                    "Company",
                    rootCompanyId),
                cancellationToken);
        }
    }

    private async Task NotifyVerifiedAsync(
        Company root,
        bool welcomeGranted,
        IReadOnlyList<string> publishedTitles,
        CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        var managers = await LoadManagerContactsAsync(root.Id, cancellationToken);
        foreach (var manager in managers)
        {
            var mail = TransactionalEmails.CompanyVerified(
                features.PublicWebBaseUrl,
                manager.FullName,
                root.Name,
                welcomeGranted,
                publishedTitles);
            await _mailer.SendAsync(mail, manager.Email, cancellationToken: cancellationToken);
            await _notifications.CreateAsync(
                new NotificationCreateRequest(
                    manager.Id,
                    mail.Subject,
                    publishedTitles.Count > 0
                        ? $"{publishedTitles.Count} vacature(s) gepubliceerd."
                        : "Je bedrijf is nu zichtbaar voor kandidaten.",
                    "CompanyVerification",
                    "/employer/vacancies",
                    "Naar vacatures",
                    "/employer/vacancies",
                    "Company",
                    root.Id),
                cancellationToken);
        }
    }

    private async Task<List<(Guid Id, string Email, string FullName)>> LoadManagerContactsAsync(
        Guid rootCompanyId,
        CancellationToken cancellationToken)
    {
        var companyIds = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == rootCompanyId || c.ParentCompanyId == rootCompanyId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var rows = await _db.Users.AsNoTracking()
            .Include(u => u.CompanyMemberships)
            .Where(u => u.IsActive
                        && u.Role == UserRole.EnterpriseManager
                        && (u.CompanyId != null && companyIds.Contains(u.CompanyId.Value)
                            || u.CompanyMemberships.Any(m => companyIds.Contains(m.CompanyId))))
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToListAsync(cancellationToken);

        return rows
            .DistinctBy(u => u.Id)
            .Select(u => (u.Id, u.Email, u.FullName))
            .ToList();
    }

    private async Task<Guid?> FindBedrijfsmanagerUserIdAsync(
        Guid companyId,
        Guid rootCompanyId,
        CancellationToken cancellationToken)
    {
        var ids = new[] { companyId, rootCompanyId }.Distinct().ToList();
        return await _db.Users.AsNoTracking()
            .Include(u => u.CompanyMemberships)
            .Where(u => u.IsActive
                        && u.Role == UserRole.EnterpriseManager
                        && (u.CompanyId != null && ids.Contains(u.CompanyId.Value)
                            || u.CompanyMemberships.Any(m => ids.Contains(m.CompanyId))))
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Company> ResolveRootAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company {companyId} not found.");
        if (company.ParentCompanyId is Guid parentId)
        {
            return await _db.Companies.FirstAsync(c => c.Id == parentId, cancellationToken);
        }

        return company;
    }

    private Task<List<Company>> LoadTreeAsync(Guid rootId, CancellationToken cancellationToken)
        => _db.Companies
            .Where(c => c.Id == rootId || c.ParentCompanyId == rootId)
            .ToListAsync(cancellationToken);

    private Task<List<Company>> LoadChildrenAsync(Guid rootId, CancellationToken cancellationToken)
        => _db.Companies
            .Where(c => c.ParentCompanyId == rootId)
            .ToListAsync(cancellationToken);
}
