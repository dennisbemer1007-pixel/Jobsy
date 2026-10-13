using Jobsy.Api.Models;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Admin;

/// <summary>
/// Admin organisations list: scalar projections + grouped counts (no Company.Location / N+1).
/// </summary>
public static class AdminCompaniesQuery
{
    public sealed record QueryArgs(
        string? Q = null,
        string? Type = null,
        string? Region = null,
        string? Status = null,
        int? Page = null,
        int? PageSize = null,
        int InactiveCompanyDays = 120);

    public sealed record Result(
        IReadOnlyList<AdminCompanyDetailDto> Items,
        int TotalCount,
        bool Paged,
        int Page,
        int PageSize,
        int CommandBudgetHint);

    public static async Task<Result> QueryAsync(
        JobsyDbContext db,
        QueryArgs args,
        CancellationToken cancellationToken = default)
    {
        var inactiveDays = Math.Clamp(args.InactiveCompanyDays, 30, 730);
        var cutoff = DateTime.UtcNow.AddDays(-inactiveDays);

        // 1) Company scalars only — never Location (PostGIS).
        var companies = await db.Companies.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CompanyRow(
                c.Id,
                c.Name,
                c.KvkNumber,
                c.Address,
                c.LogoUrl,
                c.Type.ToString(),
                c.ParentCompanyId,
                c.ReferredBySalesManagerUserId,
                c.ReferredBySalesManagerUser != null ? c.ReferredBySalesManagerUser.FullName : null,
                c.KvkVerificationStatus.ToString(),
                c.KvkVerificationAttempts,
                c.KvkLastVerificationAttemptAtUtc,
                c.KvkVerifiedAtUtc,
                c.FirstYearStartedAt,
                c.LastCsvImportAtUtc))
            .ToListAsync(cancellationToken);

        if (companies.Count == 0)
        {
            return new Result([], 0, false, 1, 0, 1);
        }

        var companyIds = companies.Select(c => c.Id).ToList();

        // 2) Grouped counts in parallel-friendly batches (fixed query count, no per-row round trips).
        var branchCounts = await db.Companies.AsNoTracking()
            .Where(c => c.ParentCompanyId != null && companyIds.Contains(c.ParentCompanyId.Value))
            .GroupBy(c => c.ParentCompanyId!.Value)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Count, cancellationToken);

        var activeUserIds = await db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        var activeUserSet = activeUserIds.ToHashSet();

        var membershipPairs = await db.UserCompanies.AsNoTracking()
            .Where(m => companyIds.Contains(m.CompanyId))
            .Select(m => new { m.CompanyId, m.UserId })
            .ToListAsync(cancellationToken);
        membershipPairs = membershipPairs.Where(m => activeUserSet.Contains(m.UserId)).ToList();

        var primaryPairs = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.CompanyId != null && companyIds.Contains(u.CompanyId.Value))
            .Select(u => new { CompanyId = u.CompanyId!.Value, UserId = u.Id })
            .ToListAsync(cancellationToken);

        var userCounts = primaryPairs.Concat(membershipPairs)
            .GroupBy(x => x.CompanyId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).Distinct().Count());

        var vacancyStats = await db.Vacancies.AsNoTracking()
            .Where(v => companyIds.Contains(v.CompanyId))
            .GroupBy(v => v.CompanyId)
            .Select(g => new
            {
                CompanyId = g.Key,
                Active = g.Count(v => v.Status == VacancyStatus.Active),
                Total = g.Count(),
                LastCreated = g.Max(v => (DateTime?)v.CreatedAtUtc)
            })
            .ToDictionaryAsync(x => x.CompanyId, cancellationToken);

        var companyByVacancy = await db.Vacancies.AsNoTracking()
            .Where(v => companyIds.Contains(v.CompanyId))
            .Select(v => new { v.Id, v.CompanyId })
            .ToListAsync(cancellationToken);
        var vacancyCompanyMap = companyByVacancy.ToDictionary(v => v.Id, v => v.CompanyId);

        var applicationRows = await db.Applications.AsNoTracking()
            .Select(a => a.VacancyId)
            .ToListAsync(cancellationToken);
        var applicationCounts = applicationRows
            .Where(vacancyCompanyMap.ContainsKey)
            .Select(vacancyId => vacancyCompanyMap[vacancyId])
            .GroupBy(companyId => companyId)
            .ToDictionary(g => g.Key, g => g.Count());

        var tokenBalances = await db.TokenTransactions.AsNoTracking()
            .Where(t => companyIds.Contains(t.CompanyId))
            .GroupBy(t => t.CompanyId)
            .Select(g => new { CompanyId = g.Key, Balance = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Balance, cancellationToken);

        var goodwillBalances = await db.TokenTransactions.AsNoTracking()
            .Where(t => companyIds.Contains(t.CompanyId) && t.Kind == TokenTransactionKind.Goodwill)
            .GroupBy(t => t.CompanyId)
            .Select(g => new { CompanyId = g.Key, Balance = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Balance, cancellationToken);

        var regionRows = await db.RegionCompanies.AsNoTracking()
            .Where(rc => companyIds.Contains(rc.CompanyId))
            .Select(rc => new { rc.CompanyId, RegionName = rc.Region.Name })
            .ToListAsync(cancellationToken);
        var regionByCompany = regionRows
            .GroupBy(r => r.CompanyId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RegionName).FirstOrDefault());

        var hosts = await db.RegionHosts.AsNoTracking()
            .Where(h => h.IsActive)
            .Select(h => new { h.Hostname, h.DisplayName })
            .ToListAsync(cancellationToken);

        var enterpriseManagers = await db.Users.AsNoTracking()
            .Where(u => u.IsActive
                        && u.Role == UserRole.EnterpriseManager
                        && u.CompanyId != null
                        && companyIds.Contains(u.CompanyId.Value))
            .Select(u => new { CompanyId = u.CompanyId!.Value, u.FullName })
            .ToListAsync(cancellationToken);
        var emByCompany = enterpriseManagers
            .GroupBy(e => e.CompanyId)
            .ToDictionary(g => g.Key, g => PersonalDataMasker.MaskName(g.First().FullName));

        var pendingTakeovers = await db.EstablishmentTakeoverRequests.AsNoTracking()
            .Where(t => t.Status == TakeoverRequestStatus.Pending)
            .Select(t => new
            {
                t.Id,
                t.TargetCompanyId,
                Applicant = t.Registration.EstablishmentName,
                t.CreatedAt
            })
            .ToListAsync(cancellationToken);
        var takeoverByTarget = pendingTakeovers
            .GroupBy(t => t.TargetCompanyId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CreatedAt).First());

        var lastLogins = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.CompanyId != null && companyIds.Contains(u.CompanyId.Value))
            .GroupBy(u => u.CompanyId!.Value)
            .Select(g => new { CompanyId = g.Key, Last = g.Max(u => u.LastLoginAtUtc) })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Last, cancellationToken);

        // Assemble rows
        var assembled = companies.Select(c =>
        {
            vacancyStats.TryGetValue(c.Id, out var vac);
            takeoverByTarget.TryGetValue(c.Id, out var directTakeover);
            // Also surface takeover on parent when any child is targeted
            var childTakeover = pendingTakeovers
                .Where(t => companies.Any(ch => ch.Id == t.TargetCompanyId && ch.ParentCompanyId == c.Id))
                .OrderBy(t => t.CreatedAt)
                .Select(t => new TakeoverHit(t.Id, t.TargetCompanyId, t.Applicant, t.CreatedAt))
                .FirstOrDefault();
            TakeoverHit? effectiveTakeover = directTakeover is null
                ? childTakeover
                : new TakeoverHit(directTakeover.Id, directTakeover.TargetCompanyId, directTakeover.Applicant, directTakeover.CreatedAt);

            var regionName = regionByCompany.GetValueOrDefault(c.Id)
                             ?? (c.ParentCompanyId is Guid pid
                                 ? regionByCompany.GetValueOrDefault(pid)
                                 : null);
            string? domain = null;
            if (!string.IsNullOrWhiteSpace(regionName))
            {
                domain = hosts.FirstOrDefault(h =>
                    string.Equals(h.DisplayName, regionName, StringComparison.OrdinalIgnoreCase))?.Hostname;
            }

            var orgId = c.ParentCompanyId ?? c.Id;
            emByCompany.TryGetValue(orgId, out var emName);

            lastLogins.TryGetValue(c.Id, out var lastLogin);
            var lastActivity = MaxDate(c.LastCsvImportAtUtc, vac?.LastCreated, lastLogin);
            var (status, inactiveDaysOut) = ResolveStatus(
                c.KvkVerificationStatus,
                effectiveTakeover is not null,
                lastActivity,
                cutoff);

            var customerSince = c.KvkVerifiedAtUtc ?? c.FirstYearStartedAt;

            return new AdminCompanyDetailDto(
                c.Id,
                c.Name,
                c.KvkNumber,
                c.Address,
                c.LogoUrl,
                c.Type,
                c.ParentCompanyId,
                userCounts.GetValueOrDefault(c.Id),
                vac?.Active ?? 0,
                vac?.Total ?? 0,
                applicationCounts.GetValueOrDefault(c.Id),
                tokenBalances.GetValueOrDefault(c.Id),
                c.SalesManagerUserId,
                c.SalesManagerName,
                BranchCount: branchCounts.GetValueOrDefault(c.Id),
                RegionName: regionName,
                DomainHostname: domain,
                EnterpriseManagerName: emName,
                KvkVerificationStatus: c.KvkVerificationStatus,
                Status: status,
                InactiveDays: inactiveDaysOut,
                CustomerSinceUtc: customerSince,
                GoodwillBalance: goodwillBalances.GetValueOrDefault(c.Id),
                PackageLabel: null,
                PendingTakeoverId: effectiveTakeover?.Id,
                PendingTakeoverApplicant: effectiveTakeover?.Applicant,
                PendingTakeoverAtUtc: effectiveTakeover?.CreatedAt,
                PendingTakeoverTargetCompanyId: effectiveTakeover?.TargetCompanyId,
                KvkVerificationAttempts: c.KvkVerificationAttempts,
                KvkLastVerificationAttemptAtUtc: c.KvkLastAttempt);
        }).ToList();

        // Filters
        IEnumerable<AdminCompanyDetailDto> filtered = assembled;
        if (!string.IsNullOrWhiteSpace(args.Type)
            && Enum.TryParse<CompanyType>(args.Type, ignoreCase: true, out _))
        {
            filtered = filtered.Where(c =>
                c.Type.Equals(args.Type, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(args.Region))
        {
            var regionTerm = args.Region.Trim();
            filtered = filtered.Where(c =>
                c.RegionName != null
                && c.RegionName.Contains(regionTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(args.Status))
        {
            var st = args.Status.Trim().ToLowerInvariant();
            filtered = st switch
            {
                "active" => filtered.Where(c => c.Status == "active"),
                "kvk-failed" => filtered.Where(c => c.Status == "kvk-failed"),
                "inactive" => filtered.Where(c => c.Status == "inactive"),
                "not-live" => filtered.Where(c => c.Status == "not-live"),
                "takeover" => filtered.Where(c => c.Status == "takeover"),
                "kvk-issue" => filtered.Where(c => c.Status is "kvk-failed" or "not-live"),
                _ => filtered
            };
        }

        if (!string.IsNullOrWhiteSpace(args.Q))
        {
            var terms = args.Q.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            filtered = filtered.Where(c => terms.All(t =>
                Contains(c.Name, t) || Contains(c.KvkNumber, t) || Contains(c.Address, t)));
        }

        var filteredList = filtered.OrderBy(c => c.Name).ToList();

        // When a root matches filters, always include its children (tree integrity).
        var matchedRootIds = filteredList
            .Where(c => c.ParentCompanyId is null)
            .Select(c => c.Id)
            .ToHashSet();
        foreach (var child in assembled.Where(c =>
                     c.ParentCompanyId is Guid pid
                     && matchedRootIds.Contains(pid)
                     && filteredList.All(f => f.Id != c.Id)))
        {
            filteredList.Add(child);
        }

        filteredList = filteredList.OrderBy(c => c.Name).ToList();

        // Paging applies to top-level organisations; include their children.
        var paged = args.Page is not null || args.PageSize is not null;
        if (!paged)
        {
            return new Result(filteredList, filteredList.Count, false, 1, filteredList.Count, 12);
        }

        var page = Math.Max(1, args.Page ?? 1);
        var pageSize = Math.Clamp(args.PageSize ?? 50, 1, 100);
        var roots = filteredList.Where(c => c.ParentCompanyId is null).ToList();
        // Include roots that have a matching child when only children matched filters
        var matchingChildParentIds = filteredList
            .Where(c => c.ParentCompanyId is Guid)
            .Select(c => c.ParentCompanyId!.Value)
            .ToHashSet();
        foreach (var parentId in matchingChildParentIds)
        {
            if (roots.All(r => r.Id != parentId))
            {
                var parent = assembled.FirstOrDefault(c => c.Id == parentId);
                if (parent is not null)
                {
                    roots.Add(parent);
                }
            }
        }

        roots = roots.OrderBy(c => c.Name).ToList();
        var totalRoots = roots.Count;
        var pageRoots = roots.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var pageRootIds = pageRoots.Select(r => r.Id).ToHashSet();
        var pageItems = filteredList
            .Where(c => pageRootIds.Contains(c.Id)
                        || (c.ParentCompanyId is Guid pid && pageRootIds.Contains(pid)))
            .OrderBy(c => c.ParentCompanyId.HasValue ? 1 : 0)
            .ThenBy(c => c.Name)
            .ToList();

        // Ensure selected roots are present even if filter excluded them somehow
        foreach (var root in pageRoots)
        {
            if (pageItems.All(i => i.Id != root.Id))
            {
                pageItems.Insert(0, root);
            }
        }

        return new Result(pageItems, totalRoots, true, page, pageSize, 12);
    }

    private static (string Status, int? InactiveDays) ResolveStatus(
        string kvkStatus,
        bool hasTakeover,
        DateTime? lastActivity,
        DateTime cutoff)
    {
        if (hasTakeover)
        {
            return ("takeover", null);
        }

        if (string.Equals(kvkStatus, nameof(KvkVerificationStatus.Failed), StringComparison.OrdinalIgnoreCase))
        {
            return ("kvk-failed", null);
        }

        if (string.Equals(kvkStatus, nameof(KvkVerificationStatus.Pending), StringComparison.OrdinalIgnoreCase))
        {
            return ("not-live", null);
        }

        if (lastActivity is not null && lastActivity < cutoff)
        {
            var days = (int)Math.Max(1, (DateTime.UtcNow - lastActivity.Value).TotalDays);
            return ("inactive", days);
        }

        return ("active", null);
    }

    private static DateTime? MaxDate(params DateTime?[] values)
    {
        DateTime? max = null;
        foreach (var v in values)
        {
            if (v is null) continue;
            if (max is null || v > max) max = v;
        }

        return max;
    }

    private static bool Contains(string? value, string term)
        => !string.IsNullOrEmpty(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private sealed record CompanyRow(
        Guid Id,
        string Name,
        string KvkNumber,
        string Address,
        string? LogoUrl,
        string Type,
        Guid? ParentCompanyId,
        Guid? SalesManagerUserId,
        string? SalesManagerName,
        string KvkVerificationStatus,
        int KvkVerificationAttempts,
        DateTime? KvkLastAttempt,
        DateTime? KvkVerifiedAtUtc,
        DateTime? FirstYearStartedAt,
        DateTime? LastCsvImportAtUtc);

    private sealed record TakeoverHit(Guid Id, Guid TargetCompanyId, string Applicant, DateTime CreatedAt);

    public static async Task<IReadOnlyList<AdminKvkIssueDto>> ListKvkIssuesAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken = default)
    {
        var companies = await db.Companies.AsNoTracking()
            .Where(c => c.KvkVerificationStatus == KvkVerificationStatus.Failed
                        || c.KvkVerificationStatus == KvkVerificationStatus.Pending)
            .OrderBy(c => c.Name)
            .Select(c => new AdminKvkIssueDto(
                c.Id,
                "company",
                c.Name,
                c.KvkNumber,
                c.KvkVerificationStatus.ToString(),
                c.KvkVerificationAttempts,
                c.KvkLastVerificationAttemptAtUtc))
            .ToListAsync(cancellationToken);

        var registrations = await db.CompanyRegistrations.AsNoTracking()
            .Where(r => r.KvkVerificationStatus == KvkVerificationStatus.Failed
                        || r.KvkVerificationStatus == KvkVerificationStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new AdminKvkIssueDto(
                r.Id,
                "registration",
                r.EstablishmentName,
                r.KvkNumber,
                r.KvkVerificationStatus.ToString(),
                0,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return companies.Concat(registrations).ToList();
    }
}
