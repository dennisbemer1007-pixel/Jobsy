using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class SalesManagerDashboardService : ISalesManagerDashboardService
{
    private readonly JobsyDbContext _db;
    private readonly ICommissionLedgerService _ledger;

    public SalesManagerDashboardService(JobsyDbContext db, ICommissionLedgerService ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    public async Task<SalesManagerDashboardDto?> GetDashboardAsync(
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == salesManagerUserId && u.Role == UserRole.SalesManager, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var profile = await _db.SalesManagerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == salesManagerUserId, cancellationToken);

        var balance = await _ledger.GetBalanceExVatAsync(salesManagerUserId, cancellationToken);
        var uninvoiced = await _ledger.GetUninvoicedBalanceExVatAsync(salesManagerUserId, cancellationToken);

        var outstandingIssued = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.SalesManagerUserId == salesManagerUserId
                        && i.Status == SelfBillingInvoiceStatus.Issued)
            .SumAsync(i => (decimal?)i.SubtotalExVat, cancellationToken) ?? 0m;

        var suppliers = await _db.Companies.AsNoTracking()
            .Where(c => c.ReferredBySalesManagerUserId == salesManagerUserId)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.KvkNumber,
                c.FirstYearSupplierSlot,
                c.FirstYearStartedAt,
                HasPaid = _db.SupplierOnboardingCheckouts.Any(o =>
                    o.CompanyId == c.Id && o.Status == SupplierOnboardingCheckoutStatus.Credited)
            })
            .ToListAsync(cancellationToken);

        var ledger = await _ledger.ListEntriesAsync(salesManagerUserId, cancellationToken);
        var invoices = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.SalesManagerUserId == salesManagerUserId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        return new SalesManagerDashboardDto(
            user.Id,
            user.Email,
            user.FullName,
            profile?.TrackingCode,
            profile?.IsOnboardingComplete ?? false,
            balance,
            SalesCommissionRules.InclVat(balance),
            uninvoiced,
            outstandingIssued,
            suppliers.Select(s => new ReferredSupplierDto(
                s.Id, s.Name, s.KvkNumber, s.FirstYearSupplierSlot, s.FirstYearStartedAt, s.HasPaid)).ToList(),
            ledger.Take(50).Select(e => new CommissionEntryDto(
                e.Id,
                e.Kind.ToString(),
                e.AmountExVat,
                e.VatAmount,
                e.Note,
                e.CompanyId,
                e.Company?.Name,
                e.CreatedAt,
                e.SelfBillingInvoiceId)).ToList(),
            invoices.Select(i => new SelfBillingInvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.SubtotalExVat,
                i.VatAmount,
                i.TotalInclVat,
                i.Status.ToString(),
                i.CreatedAt,
                i.IssuedAt,
                i.PaidAt)).ToList());
    }

    public async Task<IReadOnlyList<SalesManagerListItemDto>> ListSalesManagersAsync(
        CancellationToken cancellationToken = default)
    {
        var managers = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.SalesManager)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

        var profiles = await _db.SalesManagerProfiles.AsNoTracking()
            .Where(p => managers.Select(m => m.Id).Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId, cancellationToken);

        var result = new List<SalesManagerListItemDto>();
        foreach (var user in managers)
        {
            profiles.TryGetValue(user.Id, out var profile);
            var balance = await _ledger.GetBalanceExVatAsync(user.Id, cancellationToken);
            var count = await _db.Companies.CountAsync(
                c => c.ReferredBySalesManagerUserId == user.Id, cancellationToken);
            result.Add(new SalesManagerListItemDto(
                user.Id,
                user.Email,
                user.FullName,
                profile?.TrackingCode,
                profile?.IsOnboardingComplete ?? false,
                balance,
                count));
        }

        return result;
    }
}
