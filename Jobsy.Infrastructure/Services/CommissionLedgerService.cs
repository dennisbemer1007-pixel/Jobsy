using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class CommissionLedgerService : ICommissionLedgerService
{
    private readonly JobsyDbContext _db;

    public CommissionLedgerService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetBalanceExVatAsync(
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _db.CommissionLedgerEntries
            .AsNoTracking()
            .Where(e => e.SalesManagerUserId == salesManagerUserId)
            .SumAsync(e => (decimal?)e.AmountExVat, cancellationToken) ?? 0m;
    }

    public async Task<decimal> GetUninvoicedBalanceExVatAsync(
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _db.CommissionLedgerEntries
            .AsNoTracking()
            .Where(e => e.SalesManagerUserId == salesManagerUserId
                        && e.SelfBillingInvoiceId == null
                        && e.AmountExVat > 0)
            .SumAsync(e => (decimal?)e.AmountExVat, cancellationToken) ?? 0m;
    }

    public async Task<IReadOnlyList<CommissionLedgerEntry>> ListEntriesAsync(
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _db.CommissionLedgerEntries
            .AsNoTracking()
            .Include(e => e.Company)
            .Where(e => e.SalesManagerUserId == salesManagerUserId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CommissionLedgerEntry?> TryCreditFounderBonusAsync(
        Guid salesManagerUserId,
        Guid companyId,
        string paymentId,
        int? firstYearSlot,
        CancellationToken cancellationToken = default)
    {
        if (!SalesCommissionRules.IsEligibleFounderSlot(firstYearSlot))
        {
            return null;
        }

        // Idempotent on paymentId AND per-company founder bonus (blocks multi-checkout abuse).
        var existing = await _db.CommissionLedgerEntries
            .FirstOrDefaultAsync(
                e => e.SourcePaymentId == paymentId
                     || (e.Kind == CommissionEntryKind.FounderBonus && e.CompanyId == companyId),
                cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var amountEx = SalesCommissionRules.FounderBonusExVat;
        var entry = new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = salesManagerUserId,
            Kind = CommissionEntryKind.FounderBonus,
            AmountExVat = amountEx,
            VatAmount = SalesCommissionRules.VatOn(amountEx),
            VatRate = SalesCommissionRules.VatRate,
            Note = $"Founder-bonus 20% first-year onboarding (slot {firstYearSlot})",
            CompanyId = companyId,
            SourcePaymentId = paymentId,
            CreatedAt = DateTime.UtcNow
        };
        _db.CommissionLedgerEntries.Add(entry);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return entry;
        }
        catch (DbUpdateException)
        {
            // Concurrent duplicate — return the winner.
            return await _db.CommissionLedgerEntries
                .FirstOrDefaultAsync(
                    e => e.SourcePaymentId == paymentId
                         || (e.Kind == CommissionEntryKind.FounderBonus && e.CompanyId == companyId),
                    cancellationToken);
        }
    }

    public async Task<CommissionLedgerEntry?> TryCreditTokenCommissionAsync(
        Guid salesManagerUserId,
        Guid companyId,
        Guid tokenCheckoutId,
        decimal purchaseAmountEuro,
        DateTime? firstYearStartedAt,
        CancellationToken cancellationToken = default)
    {
        var rate = SalesCommissionRules.TokenCommissionRate(firstYearStartedAt, DateTime.UtcNow);
        if (rate is null || purchaseAmountEuro <= 0)
        {
            return null;
        }

        if (await _db.CommissionLedgerEntries.AnyAsync(
                e => e.SourceTokenCheckoutId == tokenCheckoutId, cancellationToken))
        {
            return await _db.CommissionLedgerEntries
                .FirstAsync(e => e.SourceTokenCheckoutId == tokenCheckoutId, cancellationToken);
        }

        var amountEx = decimal.Round(purchaseAmountEuro * rate.Value, 2, MidpointRounding.AwayFromZero);
        if (amountEx <= 0)
        {
            return null;
        }

        var entry = new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = salesManagerUserId,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = amountEx,
            VatAmount = SalesCommissionRules.VatOn(amountEx),
            VatRate = SalesCommissionRules.VatRate,
            Note = $"Tokencommissie {(rate.Value * 100):0}% over €{purchaseAmountEuro:0.00}",
            CompanyId = companyId,
            SourceTokenCheckoutId = tokenCheckoutId,
            CreatedAt = DateTime.UtcNow
        };
        _db.CommissionLedgerEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task AttachEntriesToInvoiceAsync(
        Guid invoiceId,
        IReadOnlyList<Guid> entryIds,
        CancellationToken cancellationToken = default)
    {
        var entries = await _db.CommissionLedgerEntries
            .Where(e => entryIds.Contains(e.Id) && e.SelfBillingInvoiceId == null)
            .ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            entry.SelfBillingInvoiceId = invoiceId;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CommissionLedgerEntry> RecordPayoutAsync(
        Guid salesManagerUserId,
        Guid invoiceId,
        decimal amountExVat,
        decimal vatAmount,
        CancellationToken cancellationToken = default)
    {
        var entry = new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = salesManagerUserId,
            Kind = CommissionEntryKind.Payout,
            AmountExVat = -Math.Abs(amountExVat),
            VatAmount = -Math.Abs(vatAmount),
            VatRate = SalesCommissionRules.VatRate,
            Note = "Self-billing uitbetaling",
            SelfBillingInvoiceId = invoiceId,
            CreatedAt = DateTime.UtcNow
        };
        _db.CommissionLedgerEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }
}
