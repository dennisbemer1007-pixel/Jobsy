using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesEmployerReadService : ISalesEmployerReadService
{
    private readonly JobsyDbContext _db;

    public SalesEmployerReadService(JobsyDbContext db) => _db = db;

    public async Task<IReadOnlyList<SalesEmployerUnitDto>> ListUnitsAsync(
        Guid beneficiaryUserId,
        CancellationToken cancellationToken = default)
    {
        var attributed = await _db.Companies.AsNoTracking()
            .Where(c => c.ReferredBySalesManagerUserId == beneficiaryUserId)
            .Select(c => new
            {
                c.Id,
                c.ParentCompanyId,
                c.Name,
                c.Address,
                c.LegalForm,
                c.SalesAttributedAtUtc,
                c.FirstYearStartedAt,
                c.CommissionStartsAtUtc
            })
            .ToListAsync(cancellationToken);

        if (attributed.Count == 0)
        {
            return [];
        }

        var byRoot = attributed
            .GroupBy(c => SalesCommercialUnit.RootIdOf(c.Id, c.ParentCompanyId))
            .ToList();

        var rootIds = byRoot.Select(g => g.Key).ToList();
        var roots = await _db.Companies.AsNoTracking()
            .Where(c => rootIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Address,
                c.LegalForm,
                c.SalesAttributedAtUtc,
                c.CommissionStartsAtUtc
            })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var commissions = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => e.SalesManagerUserId == beneficiaryUserId
                        && e.CompanyId != null
                        && e.Kind != CommissionEntryKind.Payout)
            .GroupBy(e => e.CompanyId!.Value)
            .Select(g => new { CompanyId = g.Key, Total = g.Sum(x => x.AmountExVat) })
            .ToListAsync(cancellationToken);
        var commissionByCompany = commissions.ToDictionary(x => x.CompanyId, x => x.Total);

        var result = new List<SalesEmployerUnitDto>();
        foreach (var group in byRoot)
        {
            roots.TryGetValue(group.Key, out var root);
            var sample = group.First();
            var displayName = root?.Name ?? sample.Name;
            var legalForm = root?.LegalForm ?? sample.LegalForm;
            var address = root?.Address ?? sample.Address;
            string? place = null;
            if (legalForm is not null and not CompanyLegalForm.Eenmanszaak)
            {
                place = ExtractPlace(address);
            }

            var branchIds = group.Select(g => g.Id).ToHashSet();
            branchIds.Add(group.Key);
            var ownCommission = commissionByCompany
                .Where(kv => branchIds.Contains(kv.Key))
                .Sum(kv => kv.Value);

            result.Add(new SalesEmployerUnitDto(
                group.Key,
                displayName,
                place,
                root?.SalesAttributedAtUtc
                    ?? sample.SalesAttributedAtUtc
                    ?? sample.FirstYearStartedAt,
                root?.CommissionStartsAtUtc
                    ?? group.Select(g => g.CommissionStartsAtUtc).FirstOrDefault(d => d is not null),
                BranchCount: group.Count(),
                OwnCommissionExVat: ownCommission));
        }

        return result
            .OrderByDescending(u => u.AttributedAtUtc)
            .ThenBy(u => u.DisplayName)
            .ToList();
    }

    private static string? ExtractPlace(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        // Address is free text; take the last comma-separated segment as place when present.
        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? null : parts[^1];
    }
}
