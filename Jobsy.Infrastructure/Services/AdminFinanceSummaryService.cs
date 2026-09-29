using Jobsy.Core.Admin;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class AdminFinanceSummaryService : IAdminFinanceSummaryService
{
    private readonly JobsyDbContext _db;

    public AdminFinanceSummaryService(JobsyDbContext db) => _db = db;

    public async Task<AdminFinanceSummary> GetAsync(
        string period,
        CancellationToken cancellationToken = default)
    {
        var metricsPeriod = MetricsPeriodParser.Parse(period);
        var (from, to) = MetricsPeriodParser.ResolveRange(metricsPeriod);
        var length = to - from;
        var prevTo = from;
        var prevFrom = from - length;

        var current = await SumPurchasesAsync(from, to, cancellationToken);
        var previous = await SumPurchasesAsync(prevFrom, prevTo, cancellationToken);

        var tokensSold = await _db.TokenPurchaseInvoices.AsNoTracking()
            .Where(i => i.IssuedAt >= from && i.IssuedAt <= to)
            .SumAsync(i => (int?)i.PackSize, cancellationToken) ?? 0;

        var openAtMollie = await _db.TokenPurchaseCheckouts.AsNoTracking()
            .Where(c => c.Status == TokenPurchaseCheckoutStatus.Pending)
            .SumAsync(c => (int?)c.TotalAmountCents, cancellationToken) ?? 0;

        var vatBufferPending = await _db.VatBufferTransfers.AsNoTracking()
            .Where(t => t.Status == VatBufferTransferStatus.Pending)
            .SumAsync(t => (int?)t.AmountCents, cancellationToken) ?? 0;

        var openPayouts = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.Status == SelfBillingInvoiceStatus.Issued)
            .Select(i => i.TotalInclVat)
            .ToListAsync(cancellationToken);

        var openPayoutsCents = (int)Math.Round(openPayouts.Sum() * 100m, MidpointRounding.AwayFromZero);

        return new AdminFinanceSummary(
            Period: metricsPeriod.ToString().ToLowerInvariant(),
            RevenueInclVatCents: current.Incl,
            RevenueExVatCents: current.Ex,
            PreviousRevenueInclVatCents: previous.Incl,
            TokensSold: tokensSold,
            OpenAtMollieCents: openAtMollie,
            VatBufferPendingCents: vatBufferPending,
            OpenPayoutsCents: openPayoutsCents,
            OpenPayoutsCount: openPayouts.Count);
    }

    private async Task<(int Incl, int Ex)> SumPurchasesAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var rows = await _db.TokenPurchaseInvoices.AsNoTracking()
            .Where(i => i.IssuedAt >= from && i.IssuedAt <= to)
            .Select(i => new { i.TotalAmountCents, i.AmountExVatCents })
            .ToListAsync(cancellationToken);
        return (rows.Sum(r => r.TotalAmountCents), rows.Sum(r => r.AmountExVatCents));
    }
}
