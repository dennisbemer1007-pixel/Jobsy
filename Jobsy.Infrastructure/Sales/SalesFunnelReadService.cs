using Jobsy.Core.Entities;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesFunnelReadService : ISalesFunnelReadService
{
    private readonly JobsyDbContext _db;

    public SalesFunnelReadService(JobsyDbContext db) => _db = db;

    public async Task<SalesFunnelSnapshot> GetAsync(
        Guid beneficiaryUserId,
        DateOnly fromInclusive,
        DateOnly toInclusive,
        CancellationToken cancellationToken = default)
    {
        if (toInclusive < fromInclusive)
        {
            (fromInclusive, toInclusive) = (toInclusive, fromInclusive);
        }

        var fromUtcBound = SalesClock.EndOfLocalDayUtc(fromInclusive.AddDays(-1)).AddSeconds(1);
        var toUtcBound = SalesClock.EndOfLocalDayUtc(toInclusive);

        var visits = await _db.SalesLinkClickDailies.AsNoTracking()
            .Where(c => c.BeneficiaryUserId == beneficiaryUserId
                        && c.Date >= fromInclusive
                        && c.Date <= toInclusive)
            .SumAsync(c => (int?)c.Count, cancellationToken) ?? 0;

        var registered = await _db.Companies.AsNoTracking()
            .CountAsync(
                c => c.ParentCompanyId == null
                     && c.ReferredBySalesManagerUserId == beneficiaryUserId
                     && c.SalesAttributedAtUtc != null
                     && c.SalesAttributedAtUtc >= fromUtcBound
                     && c.SalesAttributedAtUtc <= toUtcBound,
                cancellationToken);

        var firstPurchase = await _db.Companies.AsNoTracking()
            .CountAsync(
                c => c.ParentCompanyId == null
                     && c.ReferredBySalesManagerUserId == beneficiaryUserId
                     && c.CommissionStartsAtUtc != null
                     && c.CommissionStartsAtUtc >= fromUtcBound
                     && c.CommissionStartsAtUtc <= toUtcBound,
                cancellationToken);

        var activeSince = DateTime.UtcNow.AddDays(-90);
        var activeCompanyIds = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => e.SalesManagerUserId == beneficiaryUserId
                        && e.Kind == CommissionEntryKind.TokenCommission
                        && e.CreatedAt >= activeSince
                        && e.CompanyId != null)
            .Select(e => e.CompanyId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var activeNow = 0;
        if (activeCompanyIds.Count > 0)
        {
            activeNow = await _db.Companies.AsNoTracking()
                .Where(c => activeCompanyIds.Contains(c.Id))
                .Select(c => c.ParentCompanyId ?? c.Id)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        return new SalesFunnelSnapshot(visits, registered, firstPurchase, activeNow);
    }
}
