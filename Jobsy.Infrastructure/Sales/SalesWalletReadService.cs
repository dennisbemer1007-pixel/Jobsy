using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesWalletReadService : ISalesWalletReadService
{
    private static readonly CommissionEntryKind[] EarningKinds =
    [
        CommissionEntryKind.TokenCommission,
        CommissionEntryKind.IndirectTokenCommission,
        CommissionEntryKind.FounderBonus,
        CommissionEntryKind.RefundCorrection,
        CommissionEntryKind.ChargebackCorrection,
        CommissionEntryKind.Adjustment
    ];

    private readonly JobsyDbContext _db;

    public SalesWalletReadService(JobsyDbContext db) => _db = db;

    public async Task<decimal> GetAvailableAsync(
        Guid beneficiaryUserId,
        CancellationToken cancellationToken = default)
    {
        var balances = await GetBalancesAsync(beneficiaryUserId, null, cancellationToken);
        return balances.Available;
    }

    public async Task<SalesWalletBalances> GetBalancesAsync(
        Guid beneficiaryUserId,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var entries = await _db.CommissionLedgerEntries.AsNoTracking()
            .Where(e => e.SalesManagerUserId == beneficiaryUserId)
            .Select(e => new
            {
                e.Id,
                e.Kind,
                e.AmountExVat,
                e.AvailableFromUtc,
                e.SalesPayoutRequestId,
                e.SelfBillingInvoiceId,
                e.CorrectsEntryId,
                e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return new SalesWalletBalances(0m, 0m, 0m, 0m, 0m);
        }

        var invoiceIds = entries
            .Where(e => e.SelfBillingInvoiceId is not null)
            .Select(e => e.SelfBillingInvoiceId!.Value)
            .Distinct()
            .ToList();
        var requestIds = entries
            .Where(e => e.SalesPayoutRequestId is not null)
            .Select(e => e.SalesPayoutRequestId!.Value)
            .Distinct()
            .ToList();

        var invoiceStatuses = invoiceIds.Count == 0
            ? new Dictionary<Guid, SelfBillingInvoiceStatus>()
            : await _db.SelfBillingInvoices.AsNoTracking()
                .Where(i => invoiceIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.Status, cancellationToken);

        var requestStatuses = requestIds.Count == 0
            ? new Dictionary<Guid, SalesPayoutRequestStatus>()
            : await _db.SalesPayoutRequests.AsNoTracking()
                .Where(r => requestIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Status, cancellationToken);

        var localYear = SalesClock.Today(now).Year;
        decimal available = 0m, pending = 0m, requested = 0m, paidThisYear = 0m, earnedThisYear = 0m;

        foreach (var e in entries)
        {
            SelfBillingInvoiceStatus? inv = e.SelfBillingInvoiceId is Guid iid
                && invoiceStatuses.TryGetValue(iid, out var is_)
                ? is_
                : null;
            SalesPayoutRequestStatus? req = e.SalesPayoutRequestId is Guid rid
                && requestStatuses.TryGetValue(rid, out var rs)
                ? rs
                : null;

            var stub = new CommissionLedgerEntry
            {
                Kind = e.Kind,
                AmountExVat = e.AmountExVat,
                AvailableFromUtc = e.AvailableFromUtc,
                SalesPayoutRequestId = e.SalesPayoutRequestId,
                SelfBillingInvoiceId = e.SelfBillingInvoiceId,
                CorrectsEntryId = e.CorrectsEntryId
            };
            var state = DeriveState(stub, now, inv, req);

            switch (state)
            {
                case CommissionEntryState.Available:
                    available += e.AmountExVat;
                    break;
                case CommissionEntryState.Pending:
                    pending += e.AmountExVat;
                    break;
                case CommissionEntryState.Requested:
                    requested += e.AmountExVat;
                    break;
                case CommissionEntryState.Paid:
                    if (SalesClock.Today(e.CreatedAt).Year == localYear)
                    {
                        paidThisYear += Math.Abs(e.AmountExVat);
                    }

                    break;
            }

            if (EarningKinds.Contains(e.Kind)
                && e.Kind is not CommissionEntryKind.Payout
                && SalesClock.Today(e.CreatedAt).Year == localYear
                && e.AmountExVat > 0)
            {
                earnedThisYear += e.AmountExVat;
            }
        }

        return new SalesWalletBalances(available, pending, requested, paidThisYear, earnedThisYear);
    }

    public CommissionEntryState DeriveState(
        CommissionLedgerEntry entry,
        DateTime utcNow,
        SelfBillingInvoiceStatus? invoiceStatus,
        SalesPayoutRequestStatus? requestStatus)
    {
        if (entry.Kind == CommissionEntryKind.Payout
            || invoiceStatus == SelfBillingInvoiceStatus.Paid
            || requestStatus == SalesPayoutRequestStatus.Paid)
        {
            return CommissionEntryState.Paid;
        }

        if (requestStatus is SalesPayoutRequestStatus.Requested
            or SalesPayoutRequestStatus.InRun
            or SalesPayoutRequestStatus.Approved
            || entry.SalesPayoutRequestId is not null)
        {
            return CommissionEntryState.Requested;
        }

        if (entry.Kind is CommissionEntryKind.RefundCorrection
                or CommissionEntryKind.ChargebackCorrection
                or CommissionEntryKind.Adjustment
            && entry.CorrectsEntryId is not null
            && entry.SelfBillingInvoiceId is not null)
        {
            return CommissionEntryState.Settled;
        }

        if (entry.AvailableFromUtc > utcNow
            && entry.Kind is CommissionEntryKind.TokenCommission
                or CommissionEntryKind.IndirectTokenCommission
                or CommissionEntryKind.FounderBonus)
        {
            return CommissionEntryState.Pending;
        }

        // Corrections / adjustments with AvailableFromUtc = now land in Available (can be negative).
        if (entry.SelfBillingInvoiceId is null && entry.SalesPayoutRequestId is null)
        {
            return CommissionEntryState.Available;
        }

        return CommissionEntryState.Requested;
    }
}
