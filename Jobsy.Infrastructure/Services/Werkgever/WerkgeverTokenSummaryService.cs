using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.Werkgever;

public sealed class WerkgeverTokenSummaryService : IWerkgeverTokenSummaryService
{
    private readonly JobsyDbContext _db;
    private readonly ITokenLedgerService _ledger;

    public WerkgeverTokenSummaryService(JobsyDbContext db, ITokenLedgerService ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    public async Task<WerkgeverTokenSummaryDto> GetSummaryAsync(
        IReadOnlyList<Guid> companyIds,
        int periodDays = 30,
        CancellationToken cancellationToken = default)
    {
        var ids = companyIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new WerkgeverTokenSummaryDto(0, 0, 0, 0, 0, 0, 0, 0, 0, []);
        }

        var companies = await _db.Companies.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.ParentCompanyId, c.TokensManagedByEnterprise })
            .ToListAsync(cancellationToken);

        var parents = companies.Where(c => c.ParentCompanyId is null).ToList();
        var branches = companies.Where(c => c.ParentCompanyId is not null).ToList();
        if (parents.Count == 0 && branches.Count > 0)
        {
            // Scope is vestigingen only — still resolve parent pots for central figures.
            var parentIds = branches.Select(b => b.ParentCompanyId!.Value).Distinct().ToList();
            parents = await _db.Companies.AsNoTracking()
                .Where(c => parentIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name, c.ParentCompanyId, c.TokensManagedByEnterprise })
                .ToListAsync(cancellationToken);
        }

        decimal central = 0;
        foreach (var pot in parents)
        {
            central += await _ledger.GetBalanceAsync(pot.Id, cancellationToken);
        }

        decimal allocated = 0;
        var branchUsage = new List<WerkgeverTokenBranchUsageDto>();
        var regionByCompany = await _db.RegionCompanies.AsNoTracking()
            .Where(rc => ids.Contains(rc.CompanyId) || branches.Select(b => b.Id).Contains(rc.CompanyId))
            .Select(rc => new { rc.CompanyId, rc.Region.Name })
            .ToListAsync(cancellationToken);
        var regionMap = regionByCompany
            .GroupBy(x => x.CompanyId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).FirstOrDefault());

        var now = DateTime.UtcNow;
        var periodStart = now.AddDays(-Math.Clamp(periodDays, 1, 365));
        var prevStart = periodStart.AddDays(-Math.Clamp(periodDays, 1, 365));

        decimal usedPeriod = 0;
        decimal usedPrev = 0;

        foreach (var branch in branches.OrderBy(b => b.Name))
        {
            var bal = await _ledger.GetBalanceAsync(branch.Id, cancellationToken);
            allocated += bal;

            var spend = await _db.TokenTransactions.AsNoTracking()
                .Where(t => t.CompanyId == branch.Id
                            && t.Kind == TokenTransactionKind.Spend
                            && t.CreatedAt >= prevStart)
                .Select(t => new { t.Amount, t.CreatedAt })
                .ToListAsync(cancellationToken);

            var used = spend.Where(t => t.CreatedAt >= periodStart).Sum(t => Math.Abs(t.Amount));
            var usedPrevBranch = spend.Where(t => t.CreatedAt < periodStart).Sum(t => Math.Abs(t.Amount));
            usedPeriod += used;
            usedPrev += usedPrevBranch;

            // Allocated ≈ current balance + spent in period (approximation for the table).
            var assigned = bal + used;
            regionMap.TryGetValue(branch.Id, out var regionName);
            branchUsage.Add(new WerkgeverTokenBranchUsageDto(
                branch.Id,
                branch.Name,
                regionName,
                assigned,
                used,
                bal));
        }

        // Also count spend on pot itself (direct org spends).
        foreach (var pot in parents)
        {
            var potSpend = await _db.TokenTransactions.AsNoTracking()
                .Where(t => t.CompanyId == pot.Id
                            && t.Kind == TokenTransactionKind.Spend
                            && t.CreatedAt >= prevStart)
                .Select(t => new { t.Amount, t.CreatedAt })
                .ToListAsync(cancellationToken);
            usedPeriod += potSpend.Where(t => t.CreatedAt >= periodStart).Sum(t => Math.Abs(t.Amount));
            usedPrev += potSpend.Where(t => t.CreatedAt < periodStart).Sum(t => Math.Abs(t.Amount));
        }

        var branchIds = branches.Select(b => b.Id).ToHashSet();
        var scopeIds = ids.Concat(parents.Select(p => p.Id)).Concat(branchIds).Distinct().ToHashSet();

        var openPublish = await _db.Vacancies.AsNoTracking()
            .CountAsync(
                v => scopeIds.Contains(v.CompanyId) && v.Status == VacancyStatus.PendingApproval,
                cancellationToken);

        var openTokenRequests = await _db.TokenRequests.AsNoTracking()
            .Where(r => r.Status == TokenRequestStatus.Open
                        && (scopeIds.Contains(r.BranchCompanyId) || scopeIds.Contains(r.OrganisationCompanyId)))
            .ToListAsync(cancellationToken);

        var reservedRequests = openTokenRequests.Sum(r => (decimal)r.Amount);

        // Pending publish token cost estimate from active spend costs.
        var publishCost = await _ledger.GetCostAsync(TokenSpendReason.Publish, cancellationToken) ?? 1m;
        var reservedPublish = openPublish * publishCost;
        var reserved = reservedRequests + reservedPublish;

        var withAllocation = branchUsage.Count(b => b.Allocated > 0 || b.Remaining > 0);

        return new WerkgeverTokenSummaryDto(
            CentralBalance: central + allocated,
            Unallocated: central,
            AllocatedToBranches: allocated,
            BranchCountWithAllocation: withAllocation,
            UsedLast30Days: usedPeriod,
            UsedPrevious30Days: usedPrev,
            ReservedForRequests: reserved,
            OpenPublishRequestCount: openPublish,
            OpenTokenRequestCount: openTokenRequests.Count,
            BranchUsage: branchUsage.OrderByDescending(b => b.Used).ThenBy(b => b.Name).ToList());
    }
}
