using Jobsy.Core.Admin;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;
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

        var openCheckouts = await _db.TokenPurchaseCheckouts.AsNoTracking()
            .Where(c => c.Status == TokenPurchaseCheckoutStatus.Pending)
            .Select(c => new { c.TotalAmountCents, c.CreatedAt })
            .ToListAsync(cancellationToken);
        var openAtMollie = openCheckouts.Sum(c => c.TotalAmountCents);
        var openAtMollieCount = openCheckouts.Count;
        int? oldestDays = null;
        if (openCheckouts.Count > 0)
        {
            var oldest = openCheckouts.Min(c => c.CreatedAt);
            oldestDays = Math.Max(0, (int)Math.Floor((DateTime.UtcNow - oldest).TotalDays));
        }

        var vatBufferPending = await _db.VatBufferTransfers.AsNoTracking()
            .Where(t => t.Status == VatBufferTransferStatus.Pending)
            .SumAsync(t => (int?)t.AmountCents, cancellationToken) ?? 0;

        var openInvoices = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.Status == SelfBillingInvoiceStatus.Issued)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.TotalInclVat,
                i.SalesManagerUserId,
                FullName = i.SalesManagerUser.FullName,
                Role = i.SalesManagerUser.Role
            })
            .ToListAsync(cancellationToken);

        var openPayoutsCents = (int)Math.Round(
            openInvoices.Sum(i => i.TotalInclVat) * 100m,
            MidpointRounding.AwayFromZero);

        var userIds = openInvoices.Select(i => i.SalesManagerUserId).Distinct().ToList();
        var ibans = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.SalesManagerPayoutCheckouts.AsNoTracking()
                .Where(c => userIds.Contains(c.SalesManagerUserId) && c.MaskedIban != "")
                .GroupBy(c => c.SalesManagerUserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    MaskedIban = g.OrderByDescending(x => x.CreatedAt).Select(x => x.MaskedIban).FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.UserId, x => x.MaskedIban ?? "—", cancellationToken);

        var previews = openInvoices
            .OrderByDescending(i => i.TotalInclVat)
            .Select(i => new AdminFinanceOpenPayoutPreview(
                i.Id,
                i.InvoiceNumber,
                PersonalDataMasker.MaskName(i.FullName),
                i.Role == UserRole.Ambassadeur ? "Ambassadeur" : "Salesmanager",
                i.TotalInclVat,
                ibans.GetValueOrDefault(i.SalesManagerUserId, "—")))
            .ToList();

        return new AdminFinanceSummary(
            Period: metricsPeriod.ToString().ToLowerInvariant(),
            RevenueInclVatCents: current.Incl,
            RevenueExVatCents: current.Ex,
            PreviousRevenueInclVatCents: previous.Incl,
            TokensSold: tokensSold,
            OpenAtMollieCents: openAtMollie,
            OpenAtMollieCount: openAtMollieCount,
            OldestOpenMollieDays: oldestDays,
            VatBufferPendingCents: vatBufferPending,
            OpenPayoutsCents: openPayoutsCents,
            OpenPayoutsCount: openInvoices.Count,
            OpenPayoutPreviews: previews);
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
