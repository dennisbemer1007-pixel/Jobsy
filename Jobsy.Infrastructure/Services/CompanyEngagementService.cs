using Jobsy.Core.Email;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class CompanyEngagementService : ICompanyEngagementService
{
    private readonly JobsyDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IVacancyDiscoveryIndex? _discovery;
    private readonly IEmailService _email;
    private readonly ILogger<CompanyEngagementService> _logger;

    public CompanyEngagementService(
        JobsyDbContext db,
        IMemoryCache cache,
        IEmailService email,
        ILogger<CompanyEngagementService> logger,
        IVacancyDiscoveryIndex? discovery = null)
    {
        _db = db;
        _cache = cache;
        _email = email;
        _logger = logger;
        _discovery = discovery;
    }

    public async Task<CompanyEngagementDto?> GetAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return null;
        }

        var rootId = company.ParentCompanyId ?? company.Id;
        var claims = await _db.CompanyEngagementClaims.AsNoTracking()
            .Where(c => c.CompanyId == rootId
                        && c.Status != CompanyEngagementStatuses.Removed)
            .OrderBy(c => c.ItemId)
            .ToListAsync(cancellationToken);

        return new CompanyEngagementDto(
            companyId,
            rootId,
            claims.Select(ToDto).ToList());
    }

    public async Task<CompanyEngagementDto> SaveAsync(
        Guid companyId,
        CompanyEngagementUpdate update,
        CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new InvalidOperationException("Bedrijf niet gevonden.");
        var rootId = company.ParentCompanyId ?? company.Id;

        var inputs = NormalizeInputs(update.Claims);
        var existing = await _db.CompanyEngagementClaims
            .Where(c => c.CompanyId == rootId)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var claimedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var input in inputs)
        {
            claimedIds.Add(input.ItemId);
            var row = existing.FirstOrDefault(c =>
                string.Equals(c.ItemId, input.ItemId, StringComparison.OrdinalIgnoreCase));

            if (row is { Status: CompanyEngagementStatuses.Removed })
            {
                var cooldownEnds = (row.RemovedAtUtc ?? row.UpdatedAtUtc).AddDays(EngagementCatalog.RemovedCooldownDays);
                if (cooldownEnds > now)
                {
                    throw new InvalidOperationException(
                        $"Dit kenmerk is verwijderd en kan tot {cooldownEnds:yyyy-MM-dd} niet opnieuw worden toegevoegd.");
                }
            }

            var proofUrl = NormalizeProofUrl(input.ProofUrl);
            var proofText = NormalizeProofText(input.ProofText);

            if (row is null)
            {
                row = new CompanyEngagementClaim
                {
                    Id = Guid.NewGuid(),
                    CompanyId = rootId,
                    ItemId = input.ItemId,
                    CreatedAtUtc = now
                };
                _db.CompanyEngagementClaims.Add(row);
                existing.Add(row);
            }

            var proofChanged = !string.Equals(row.ProofUrl, proofUrl, StringComparison.Ordinal)
                               || !string.Equals(row.ProofText, proofText, StringComparison.Ordinal);
            row.ProofUrl = proofUrl;
            row.ProofText = proofText;
            row.UpdatedAtUtc = now;

            if (row.Status == CompanyEngagementStatuses.Removed
                || row.Status == CompanyEngagementStatuses.Checked && proofChanged)
            {
                row.Status = CompanyEngagementStatuses.SelfDeclared;
                row.CheckedSource = null;
                row.CheckedByUserId = null;
                row.CheckedAtUtc = null;
                row.RemovedReason = null;
                row.RemovedAtUtc = null;
            }
            else if (row.Status != CompanyEngagementStatuses.Checked)
            {
                row.Status = CompanyEngagementStatuses.SelfDeclared;
            }
        }

        foreach (var row in existing.Where(c =>
                     c.Status != CompanyEngagementStatuses.Removed
                     && !claimedIds.Contains(c.ItemId)))
        {
            // Employer un-claims: soft-remove without admin reason (no cooldown mail).
            row.Status = CompanyEngagementStatuses.Removed;
            row.RemovedReason = "Door werkgever verwijderd";
            row.RemovedAtUtc = now;
            row.UpdatedAtUtc = now;
            row.CheckedSource = null;
            row.CheckedByUserId = null;
            row.CheckedAtUtc = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await EvictAndInvalidateAsync(rootId, cancellationToken);

        return (await GetAsync(companyId, cancellationToken))!;
    }

    public async Task<IReadOnlyList<AdminEngagementQueueItem>> ListAdminQueueAsync(
        string? filter,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var normalized = (filter ?? "queue").Trim().ToLowerInvariant();
        var q = _db.CompanyEngagementClaims.AsNoTracking()
            .Include(c => c.Company)
            .AsQueryable();

        if (normalized == "queue")
        {
            q = q.Where(c =>
                c.Status == CompanyEngagementStatuses.SelfDeclared
                && (c.ProofUrl != null || c.ProofText != null));
        }
        else if (normalized == "noproof")
        {
            q = q.Where(c =>
                c.Status == CompanyEngagementStatuses.SelfDeclared
                && c.ProofUrl == null
                && c.ProofText == null);
        }
        else if (normalized == "checked")
        {
            q = q.Where(c => c.Status == CompanyEngagementStatuses.Checked);
        }
        else if (normalized == "removed")
        {
            q = q.Where(c => c.Status == CompanyEngagementStatuses.Removed);
        }
        else if (normalized == "reports")
        {
            var reportedIds = await _db.CompanyEngagementReports.AsNoTracking()
                .Where(r => r.ReviewedAtUtc == null)
                .Select(r => r.ClaimId)
                .Distinct()
                .ToListAsync(cancellationToken);
            q = q.Where(c => reportedIds.Contains(c.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c =>
                (c.Company != null && c.Company.Name.Contains(s))
                || (c.Company != null && c.Company.KvkNumber != null && c.Company.KvkNumber.Contains(s))
                || c.ItemId.Contains(s)
                || (c.ProofText != null && c.ProofText.Contains(s)));
        }

        var rows = await q
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var claimIds = rows.Select(r => r.Id).ToList();
        var reportCounts = await _db.CompanyEngagementReports.AsNoTracking()
            .Where(r => claimIds.Contains(r.ClaimId) && r.ReviewedAtUtc == null)
            .GroupBy(r => r.ClaimId)
            .Select(g => new { ClaimId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClaimId, x => x.Count, cancellationToken);

        return rows.Select(r => new AdminEngagementQueueItem(
            r.Id,
            r.CompanyId,
            r.Company?.Name ?? "",
            r.Company?.KvkNumber,
            r.ItemId,
            r.Status,
            r.ProofUrl,
            r.ProofText,
            r.CheckedSource,
            r.UpdatedAtUtc,
            InQueue: r.Status == CompanyEngagementStatuses.SelfDeclared
                     && (r.ProofUrl is not null || r.ProofText is not null),
            OpenReportCount: reportCounts.GetValueOrDefault(r.Id),
            r.RemovedReason)).ToList();
    }

    public async Task CheckAsync(Guid claimId, Guid adminUserId, CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyEngagementClaims
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.Id == claimId, cancellationToken)
            ?? throw new KeyNotFoundException("Claim niet gevonden.");

        row.Status = CompanyEngagementStatuses.Checked;
        row.CheckedSource = CompanyEngagementCheckedSources.Admin;
        row.CheckedByUserId = adminUserId;
        row.CheckedAtUtc = DateTime.UtcNow;
        row.RemovedReason = null;
        row.RemovedAtUtc = null;
        row.UpdatedAtUtc = DateTime.UtcNow;

        await MarkReportsReviewedAsync(claimId, cancellationToken);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "admin.engagement.check",
            Message = $"claim={claimId:N};company={row.CompanyId:N};item={row.ItemId};by={adminUserId:N}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        await EvictAndInvalidateAsync(row.CompanyId, cancellationToken);
    }

    public async Task RemoveAsync(
        Guid claimId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3)
        {
            throw new ArgumentException("Reden is verplicht (minimaal 3 tekens).");
        }

        var row = await _db.CompanyEngagementClaims
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.Id == claimId, cancellationToken)
            ?? throw new KeyNotFoundException("Claim niet gevonden.");

        var trimmed = reason.Trim();
        if (trimmed.Length > 500)
        {
            trimmed = trimmed[..500];
        }

        row.Status = CompanyEngagementStatuses.Removed;
        row.RemovedReason = trimmed;
        row.RemovedAtUtc = DateTime.UtcNow;
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.CheckedSource = null;
        row.CheckedByUserId = null;
        row.CheckedAtUtc = null;

        await MarkReportsReviewedAsync(claimId, cancellationToken);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "admin.engagement.remove",
            Message = $"claim={claimId:N};company={row.CompanyId:N};item={row.ItemId};by={adminUserId:N}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        await EvictAndInvalidateAsync(row.CompanyId, cancellationToken);
        await NotifyManagersRemovedAsync(row, trimmed, cancellationToken);
    }

    public async Task ResetToSelfDeclaredAsync(
        Guid claimId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyEngagementClaims
            .FirstOrDefaultAsync(c => c.Id == claimId, cancellationToken)
            ?? throw new KeyNotFoundException("Claim niet gevonden.");

        row.Status = CompanyEngagementStatuses.SelfDeclared;
        row.CheckedSource = null;
        row.CheckedByUserId = null;
        row.CheckedAtUtc = null;
        row.RemovedReason = null;
        row.RemovedAtUtc = null;
        row.UpdatedAtUtc = DateTime.UtcNow;

        await MarkReportsReviewedAsync(claimId, cancellationToken);
        _db.PlatformLogs.Add(new PlatformLog
        {
            Id = Guid.NewGuid(),
            Level = PlatformLogLevel.Info,
            Category = "admin.engagement.reset",
            Message = $"claim={claimId:N};company={row.CompanyId:N};item={row.ItemId};by={adminUserId:N}",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        await EvictAndInvalidateAsync(row.CompanyId, cancellationToken);
    }

    public async Task ReportAsync(
        Guid companyId,
        string itemId,
        string message,
        string? reporterEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Trim().Length < 5)
        {
            throw new ArgumentException("Bericht is verplicht (minimaal 5 tekens).");
        }

        var id = EngagementCatalog.NormalizeId(itemId)
                 ?? throw new ArgumentException("Onbekend kenmerk.");
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Bedrijf niet gevonden.");
        var rootId = company.ParentCompanyId ?? company.Id;

        var claim = await _db.CompanyEngagementClaims
            .FirstOrDefaultAsync(
                c => c.CompanyId == rootId
                     && c.ItemId == id
                     && c.Status != CompanyEngagementStatuses.Removed,
                cancellationToken)
            ?? throw new KeyNotFoundException("Kenmerk niet gevonden.");

        var text = message.Trim();
        if (text.Length > 500)
        {
            text = text[..500];
        }

        var email = string.IsNullOrWhiteSpace(reporterEmail)
            ? null
            : reporterEmail.Trim().ToLowerInvariant();
        if (email is { Length: > 200 })
        {
            email = email[..200];
        }

        _db.CompanyEngagementReports.Add(new CompanyEngagementReport
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            CompanyId = rootId,
            ItemId = id,
            ReporterEmail = email,
            Message = text,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkReportsReviewedAsync(Guid claimId, CancellationToken cancellationToken)
    {
        var open = await _db.CompanyEngagementReports
            .Where(r => r.ClaimId == claimId && r.ReviewedAtUtc == null)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var report in open)
        {
            report.ReviewedAtUtc = now;
        }
    }

    private async Task NotifyManagersRemovedAsync(
        CompanyEngagementClaim claim,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var managers = await (
                from uc in _db.UserCompanies.AsNoTracking()
                join u in _db.Users.AsNoTracking() on uc.UserId equals u.Id
                where uc.CompanyId == claim.CompanyId
                      && u.IsActive
                      && u.Email != null
                      && (u.Role == UserRole.EnterpriseManager
                          || u.Role == UserRole.BranchManager
                          || u.Role == UserRole.RegionalManager)
                select new { u.Email, u.FullName }
            ).Distinct().ToListAsync(cancellationToken);

            if (managers.Count == 0)
            {
                return;
            }

            var label = EngagementCatalog.DutchLabel(claim.ItemId);
            var companyName = claim.Company?.Name ?? "jullie bedrijf";
            foreach (var user in managers)
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    continue;
                }

                var mail = TransactionalEmails.EngagementClaimRemoved(
                    null,
                    companyName,
                    label,
                    reason);
                await _email.SendAsync(
                    new EmailMessage(user.Email, mail.Subject, mail.Html, mail.Category),
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to e-mail engagement removal for claim {ClaimId}", claim.Id);
        }
    }

    private async Task EvictAndInvalidateAsync(Guid rootCompanyId, CancellationToken cancellationToken)
    {
        _cache.Remove(CompanyCultureCacheKeys.ForCompany(rootCompanyId));
        var childIds = await _db.Companies.AsNoTracking()
            .Where(c => c.ParentCompanyId == rootCompanyId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        foreach (var childId in childIds)
        {
            _cache.Remove(CompanyCultureCacheKeys.ForCompany(childId));
        }

        if (_discovery is not null)
        {
            await _discovery.InvalidateCompanyAsync(rootCompanyId, cancellationToken);
            foreach (var childId in childIds)
            {
                await _discovery.InvalidateCompanyAsync(childId, cancellationToken);
            }
        }
    }

    private static IReadOnlyList<CompanyEngagementClaimInput> NormalizeInputs(
        IReadOnlyList<CompanyEngagementClaimInput>? claims)
    {
        if (claims is null || claims.Count == 0)
        {
            return [];
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<CompanyEngagementClaimInput>();
        foreach (var claim in claims)
        {
            var id = EngagementCatalog.NormalizeId(claim.ItemId);
            if (id is null || !seen.Add(id))
            {
                continue;
            }

            list.Add(new CompanyEngagementClaimInput(id, claim.ProofUrl, claim.ProofText));
        }

        return list;
    }

    private static string? NormalizeProofUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = raw.Trim();
        if (value.Length > EngagementCatalog.MaxProofUrlLength)
        {
            throw new InvalidOperationException(
                $"Bewijs-link mag maximaal {EngagementCatalog.MaxProofUrlLength} tekens zijn.");
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Bewijs-link moet een https-URL zijn.");
        }

        return uri.AbsoluteUri;
    }

    private static string? NormalizeProofText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = raw.Trim();
        if (value.Length > EngagementCatalog.MaxProofTextLength)
        {
            throw new InvalidOperationException(
                $"Toelichting mag maximaal {EngagementCatalog.MaxProofTextLength} tekens zijn.");
        }

        return value;
    }

    private static CompanyEngagementClaimDto ToDto(CompanyEngagementClaim row)
        => new(
            row.ItemId,
            row.Status,
            row.ProofUrl,
            row.ProofText,
            row.CheckedSource,
            row.CheckedAtUtc,
            row.RemovedAtUtc,
            row.RemovedReason);
}
