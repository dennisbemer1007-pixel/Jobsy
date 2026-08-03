using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class SelfBillingInvoiceService : ISelfBillingInvoiceService
{
    private readonly JobsyDbContext _db;
    private readonly ICommissionLedgerService _ledger;

    public SelfBillingInvoiceService(JobsyDbContext db, ICommissionLedgerService ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    public async Task<SelfBillingInvoice> CreateFromUninvoicedBalanceAsync(
        Guid salesManagerUserId,
        decimal? maxAmountExVat = null,
        CancellationToken cancellationToken = default)
    {
        var billing = await ResolveBillingIdentityAsync(salesManagerUserId, cancellationToken)
            ?? throw new InvalidOperationException("Profiel ontbreekt voor self-billing.");

        if (!billing.IsOnboardingComplete)
        {
            throw new InvalidOperationException("Onboarding moet compleet zijn vóór self-billing.");
        }

        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var entries = await _db.CommissionLedgerEntries
            .Where(e => e.SalesManagerUserId == salesManagerUserId
                        && e.SelfBillingInvoiceId == null
                        && e.AmountExVat > 0)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            throw new InvalidOperationException("Geen openstaand tegoed om te factureren.");
        }

        var available = decimal.Round(entries.Sum(e => e.AmountExVat), 2, MidpointRounding.AwayFromZero);
        decimal target;
        if (maxAmountExVat is null)
        {
            target = available;
        }
        else
        {
            target = decimal.Round(maxAmountExVat.Value, 2, MidpointRounding.AwayFromZero);
            if (target <= 0)
            {
                throw new InvalidOperationException("Kies een bedrag groter dan € 0,00.");
            }

            if (target > available)
            {
                throw new InvalidOperationException(
                    $"Bedrag mag niet hoger zijn dan je openstaande tegoed (€ {available:0.00} excl. BTW).");
            }
        }

        var selected = SelectEntries(entries, target);
        if (selected.Count == 0)
        {
            throw new InvalidOperationException("Geen openstaand tegoed om te factureren.");
        }

        // Split last partial entry before claiming so remainder stays uninvoiced.
        foreach (var pick in selected.Where(p => p.IsPartial).ToList())
        {
            SplitLedgerEntry(pick.Entry, pick.AmountExVat);
        }

        var entryIds = selected.Select(s => s.Entry.Id).ToList();
        var subtotal = decimal.Round(selected.Sum(s => s.AmountExVat), 2, MidpointRounding.AwayFromZero);
        var vat = SalesCommissionRules.VatOn(subtotal);
        var now = DateTime.UtcNow;
        var invoiceNumber = await NextInvoiceNumberAsync(cancellationToken);
        var invoiceId = Guid.NewGuid();

        var invoice = new SelfBillingInvoice
        {
            Id = invoiceId,
            SalesManagerUserId = salesManagerUserId,
            InvoiceNumber = invoiceNumber,
            SalesManagerCompanyName = billing.CompanyName,
            SalesManagerKvkNumber = billing.KvkNumber,
            SalesManagerVatNumber = billing.VatNumber,
            SalesManagerAddress = billing.FormattedAddress,
            SubtotalExVat = subtotal,
            VatAmount = vat,
            TotalInclVat = subtotal + vat,
            VatRate = SalesCommissionRules.VatRate,
            VatTreatment = SalesManagerVatTreatment.Standard21,
            Status = SelfBillingInvoiceStatus.Issued,
            CreatedAt = now,
            IssuedAt = now
        };

        foreach (var pick in selected)
        {
            invoice.Lines.Add(new SelfBillingInvoiceLine
            {
                Id = Guid.NewGuid(),
                Description = pick.Entry.Note ?? pick.Entry.Kind.ToString(),
                AmountExVat = pick.AmountExVat,
                SourceLedgerEntryId = pick.Entry.Id
            });
        }

        _db.SelfBillingInvoices.Add(invoice);
        await _db.SaveChangesAsync(cancellationToken);

        // Atomic claim: only attach rows that are still uninvoiced.
        int attached;
        try
        {
            attached = await _db.CommissionLedgerEntries
                .Where(e => entryIds.Contains(e.Id) && e.SelfBillingInvoiceId == null)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.SelfBillingInvoiceId, invoiceId),
                    cancellationToken);
        }
        catch (InvalidOperationException)
        {
            attached = 0;
            foreach (var entryId in entryIds)
            {
                var tracked = await _db.CommissionLedgerEntries
                    .FirstAsync(e => e.Id == entryId, cancellationToken);
                if (tracked.SelfBillingInvoiceId is null)
                {
                    tracked.SelfBillingInvoiceId = invoiceId;
                    attached++;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (attached != entryIds.Count)
        {
            if (tx is not null)
            {
                await tx.RollbackAsync(cancellationToken);
            }
            else
            {
                _db.SelfBillingInvoices.Remove(invoice);
                await _db.SaveChangesAsync(cancellationToken);
            }

            throw new InvalidOperationException(
                "Tegoed is ondertussen al gefactureerd. Vernieuw en probeer opnieuw.");
        }

        if (tx is not null)
        {
            await tx.CommitAsync(cancellationToken);
        }

        return await _db.SelfBillingInvoices
            .Include(i => i.Lines)
            .FirstAsync(i => i.Id == invoiceId, cancellationToken);
    }

    public async Task<SelfBillingInvoice> MarkPaidAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        // Atomic Issued → Paid claim to prevent double payout.
        int claimed;
        try
        {
            claimed = await _db.SelfBillingInvoices
                .Where(i => i.Id == invoiceId && i.Status == SelfBillingInvoiceStatus.Issued)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(i => i.Status, SelfBillingInvoiceStatus.Paid)
                        .SetProperty(i => i.PaidAt, DateTime.UtcNow),
                    cancellationToken);
        }
        catch (InvalidOperationException)
        {
            var invoice = await _db.SelfBillingInvoices
                .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
                ?? throw new KeyNotFoundException("Factuur niet gevonden.");

            if (invoice.Status == SelfBillingInvoiceStatus.Paid)
            {
                return invoice;
            }

            if (invoice.Status != SelfBillingInvoiceStatus.Issued)
            {
                throw new InvalidOperationException(
                    "Alleen uitgegeven facturen kunnen als betaald worden gemarkeerd.");
            }

            invoice.Status = SelfBillingInvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            claimed = 1;
        }

        if (claimed == 0)
        {
            var current = await _db.SelfBillingInvoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
                ?? throw new KeyNotFoundException("Factuur niet gevonden.");

            if (current.Status == SelfBillingInvoiceStatus.Paid)
            {
                return current;
            }

            throw new InvalidOperationException(
                "Alleen uitgegeven facturen kunnen als betaald worden gemarkeerd.");
        }

        var paid = await _db.SelfBillingInvoices
            .FirstAsync(i => i.Id == invoiceId, cancellationToken);

        // Idempotent payout: skip if a payout ledger row already exists for this invoice.
        var payoutExists = await _db.CommissionLedgerEntries.AnyAsync(
            e => e.SelfBillingInvoiceId == invoiceId && e.Kind == CommissionEntryKind.Payout,
            cancellationToken);
        if (!payoutExists)
        {
            await _ledger.RecordPayoutAsync(
                paid.SalesManagerUserId,
                paid.Id,
                paid.SubtotalExVat,
                paid.VatAmount,
                cancellationToken);
        }

        return paid;
    }

    public async Task<IReadOnlyList<SelfBillingInvoice>> ListForSalesManagerAsync(
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _db.SelfBillingInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.SalesManagerUserId == salesManagerUserId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SelfBillingInvoice?> GetAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        return await _db.SelfBillingInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);
    }

    private static List<SelectedLedgerAmount> SelectEntries(
        IReadOnlyList<CommissionLedgerEntry> entries,
        decimal targetExVat)
    {
        var remaining = targetExVat;
        var selected = new List<SelectedLedgerAmount>();
        foreach (var entry in entries)
        {
            if (remaining <= 0)
            {
                break;
            }

            var take = Math.Min(entry.AmountExVat, remaining);
            take = decimal.Round(take, 2, MidpointRounding.AwayFromZero);
            if (take <= 0)
            {
                continue;
            }

            selected.Add(new SelectedLedgerAmount(entry, take, take < entry.AmountExVat));
            remaining = decimal.Round(remaining - take, 2, MidpointRounding.AwayFromZero);
        }

        return selected;
    }

    private void SplitLedgerEntry(CommissionLedgerEntry entry, decimal invoiceAmountExVat)
    {
        var remainderExVat = decimal.Round(entry.AmountExVat - invoiceAmountExVat, 2, MidpointRounding.AwayFromZero);
        if (remainderExVat <= 0)
        {
            return;
        }

        // Keep unique indexes: don't copy SourcePaymentId / SourceTokenCheckoutId / founder CompanyId.
        var remainder = new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = entry.SalesManagerUserId,
            Kind = entry.Kind == CommissionEntryKind.FounderBonus
                ? CommissionEntryKind.Adjustment
                : entry.Kind,
            AmountExVat = remainderExVat,
            VatAmount = SalesCommissionRules.VatOn(remainderExVat),
            VatRate = entry.VatRate,
            Note = string.IsNullOrWhiteSpace(entry.Note)
                ? "Restant na gedeeltelijke uitbetaling"
                : $"{entry.Note} (restant)",
            CompanyId = entry.Kind == CommissionEntryKind.FounderBonus ? null : entry.CompanyId,
            SourcePaymentId = null,
            SourceTokenCheckoutId = null,
            SelfBillingInvoiceId = null,
            CreatedAt = DateTime.UtcNow
        };
        _db.CommissionLedgerEntries.Add(remainder);

        entry.AmountExVat = invoiceAmountExVat;
        entry.VatAmount = SalesCommissionRules.VatOn(invoiceAmountExVat);
    }

    private async Task<string> NextInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"SB-{year}-";
        var last = await _db.SelfBillingInvoices
            .AsNoTracking()
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var seq = 1;
        if (last is not null && last.Length > prefix.Length
            && int.TryParse(last[prefix.Length..], out var n))
        {
            seq = n + 1;
        }

        return $"{prefix}{seq:D4}";
    }

    private static string FormatAddress(string? address, string? postalCode, string? city, string? country) =>
        string.Join(", ", new[]
        {
            address,
            $"{postalCode} {city}".Trim(),
            country
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private async Task<BillingIdentity?> ResolveBillingIdentityAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var sm = await _db.SalesManagerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (sm is not null)
        {
            return new BillingIdentity(
                sm.CompanyName ?? "",
                sm.KvkNumber ?? "",
                sm.VatNumber ?? "",
                FormatAddress(sm.Address, sm.PostalCode, sm.City, sm.Country),
                sm.IsOnboardingComplete,
                sm.Iban);
        }

        var am = await _db.AmbassadeurProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (am is not null)
        {
            return new BillingIdentity(
                am.CompanyName ?? "",
                am.KvkNumber ?? "",
                am.VatNumber ?? "",
                FormatAddress(am.Address, am.PostalCode, am.City, am.Country),
                am.IsOnboardingComplete,
                am.Iban);
        }

        return null;
    }

    private sealed record BillingIdentity(
        string CompanyName,
        string KvkNumber,
        string VatNumber,
        string FormattedAddress,
        bool IsOnboardingComplete,
        string? Iban);

    private sealed record SelectedLedgerAmount(
        CommissionLedgerEntry Entry,
        decimal AmountExVat,
        bool IsPartial);
}
