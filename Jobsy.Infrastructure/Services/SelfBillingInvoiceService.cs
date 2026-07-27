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
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.SalesManagerProfiles
            .FirstOrDefaultAsync(p => p.UserId == salesManagerUserId, cancellationToken)
            ?? throw new InvalidOperationException("Salesmanager-profiel ontbreekt.");

        if (!profile.IsOnboardingComplete)
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

        var entryIds = entries.Select(e => e.Id).ToList();
        var subtotal = entries.Sum(e => e.AmountExVat);
        var vat = SalesCommissionRules.VatOn(subtotal);
        var now = DateTime.UtcNow;
        var invoiceNumber = await NextInvoiceNumberAsync(cancellationToken);
        var invoiceId = Guid.NewGuid();

        var invoice = new SelfBillingInvoice
        {
            Id = invoiceId,
            SalesManagerUserId = salesManagerUserId,
            InvoiceNumber = invoiceNumber,
            SalesManagerCompanyName = profile.CompanyName ?? "",
            SalesManagerKvkNumber = profile.KvkNumber ?? "",
            SalesManagerVatNumber = profile.VatNumber ?? "",
            SalesManagerAddress = FormatAddress(profile),
            SubtotalExVat = subtotal,
            VatAmount = vat,
            TotalInclVat = subtotal + vat,
            VatRate = SalesCommissionRules.VatRate,
            Status = SelfBillingInvoiceStatus.Issued,
            CreatedAt = now,
            IssuedAt = now
        };

        foreach (var entry in entries)
        {
            invoice.Lines.Add(new SelfBillingInvoiceLine
            {
                Id = Guid.NewGuid(),
                Description = entry.Note ?? entry.Kind.ToString(),
                AmountExVat = entry.AmountExVat,
                SourceLedgerEntryId = entry.Id
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
            foreach (var entry in entries)
            {
                var tracked = await _db.CommissionLedgerEntries
                    .FirstAsync(e => e.Id == entry.Id, cancellationToken);
                if (tracked.SelfBillingInvoiceId is null)
                {
                    tracked.SelfBillingInvoiceId = invoiceId;
                    attached++;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        if (attached != entries.Count)
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

    private static string FormatAddress(SalesManagerProfile profile) =>
        string.Join(", ", new[]
        {
            profile.Address,
            $"{profile.PostalCode} {profile.City}".Trim(),
            profile.Country
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
