using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Sales;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class SelfBillingInvoiceService : ISelfBillingInvoiceService
{
    private static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");

    private readonly JobsyDbContext _db;
    private readonly ICommissionLedgerService _ledger;

    public SelfBillingInvoiceService(JobsyDbContext db, ICommissionLedgerService ledger)
    {
        _db = db;
        _ledger = ledger;
    }

    public async Task<SelfBillingInvoice> IssueForRequestAsync(
        SalesPayoutRequest request,
        SalesSelfBillingConsent consent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(consent);

        if (request.SelfBillingInvoiceId is Guid existingId)
        {
            return await GetAsync(existingId, cancellationToken)
                   ?? throw new KeyNotFoundException("Factuur niet gevonden.");
        }

        var billing = await ResolveBillingIdentityAsync(request.BeneficiaryUserId, cancellationToken)
            ?? throw new InvalidOperationException("Profiel ontbreekt voor self-billing.");

        var lines = await _db.CommissionLedgerEntries
            .Where(e => e.SalesPayoutRequestId == request.Id && e.SelfBillingInvoiceId == null)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Geen gekoppelde commissieregels voor deze aanvraag.");
        }

        var companyIds = lines.Where(l => l.CompanyId is not null)
            .Select(l => l.CompanyId!.Value).Distinct().ToList();
        var companyNames = companyIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Companies.AsNoTracking()
                .Where(c => companyIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var isKor = request.VatTreatment == SalesManagerVatTreatment.SmallBusinessScheme;
        var subtotal = decimal.Round(lines.Sum(l => l.AmountExVat), 2, MidpointRounding.AwayFromZero);
        var vat = isKor ? 0m : SalesCommissionRules.VatOn(subtotal);
        var now = DateTime.UtcNow;
        var invoiceId = Guid.NewGuid();

        SelfBillingInvoice? invoice = null;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var invoiceNumber = await NextInvoiceNumberAsync(cancellationToken);
            invoice = new SelfBillingInvoice
            {
                Id = invoiceId,
                SalesManagerUserId = request.BeneficiaryUserId,
                InvoiceNumber = invoiceNumber,
                SalesManagerCompanyName = billing.CompanyName,
                SalesManagerKvkNumber = billing.KvkNumber,
                SalesManagerVatNumber = billing.VatNumber ?? "",
                SalesManagerAddress = billing.FormattedAddress,
                SubtotalExVat = subtotal,
                VatAmount = vat,
                TotalInclVat = subtotal + vat,
                VatRate = isKor ? 0m : SalesCommissionRules.VatRate,
                VatTreatment = request.VatTreatment,
                Status = SelfBillingInvoiceStatus.Issued,
                CreatedAt = now,
                IssuedAt = now,
                SelfBillingConsentId = consent.Id,
                SalesPayoutRequestId = request.Id
            };

            foreach (var line in lines)
            {
                var employer = line.CompanyId is Guid cid && companyNames.TryGetValue(cid, out var n)
                    ? n
                    : "";
                var month = SalesClock.ToLocal(line.CreatedAt).ToString("MMMM yyyy", Nl);
                var desc = string.IsNullOrWhiteSpace(employer)
                    ? $"Commissie · {month}"
                    : $"Commissie · {employer} · {month}";
                if (line.AmountExVat < 0)
                {
                    desc = string.IsNullOrWhiteSpace(line.Reason)
                        ? $"Correctie · {employer}".Trim(' ', '·')
                        : $"Correctie · {employer} · {line.Reason}".Replace(" ·  · ", " · ");
                }

                invoice.Lines.Add(new SelfBillingInvoiceLine
                {
                    Id = Guid.NewGuid(),
                    Description = desc,
                    AmountExVat = line.AmountExVat,
                    SourceLedgerEntryId = line.Id
                });
            }

            _db.SelfBillingInvoices.Add(invoice);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateException) when (attempt < 4)
            {
                _db.SelfBillingInvoices.Remove(invoice);
                invoice = null;
                invoiceId = Guid.NewGuid();
            }
        }

        if (invoice is null)
        {
            throw new InvalidOperationException("Kon geen uniek factuurnummer toekennen.");
        }

        var entryIds = lines.Select(l => l.Id).ToList();
        try
        {
            await _db.CommissionLedgerEntries
                .Where(e => entryIds.Contains(e.Id) && e.SelfBillingInvoiceId == null)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.SelfBillingInvoiceId, invoice.Id),
                    cancellationToken);
        }
        catch (InvalidOperationException)
        {
            foreach (var line in lines)
            {
                var tracked = await _db.CommissionLedgerEntries
                    .FirstAsync(e => e.Id == line.Id, cancellationToken);
                tracked.SelfBillingInvoiceId = invoice.Id;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        var trackedRequest = await _db.SalesPayoutRequests
            .FirstAsync(r => r.Id == request.Id, cancellationToken);
        trackedRequest.SelfBillingInvoiceId = invoice.Id;
        await _db.SaveChangesAsync(cancellationToken);

        return await _db.SelfBillingInvoices
            .Include(i => i.Lines)
            .FirstAsync(i => i.Id == invoice.Id, cancellationToken);
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

        var positiveUninvoiced = decimal.Round(entries.Sum(e => e.AmountExVat), 2, MidpointRounding.AwayFromZero);
        var ledgerBalance = decimal.Round(
            await _ledger.GetBalanceExVatAsync(salesManagerUserId, cancellationToken),
            2,
            MidpointRounding.AwayFromZero);
        var available = Math.Max(0m, Math.Min(positiveUninvoiced, ledgerBalance));
        if (available <= 0)
        {
            throw new InvalidOperationException("Geen openstaand tegoed om te factureren.");
        }

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
        var treatment = billing.VatTreatment;
        var isKor = treatment == SalesManagerVatTreatment.SmallBusinessScheme;
        var vat = isKor ? 0m : SalesCommissionRules.VatOn(subtotal);
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
            VatRate = isKor ? 0m : SalesCommissionRules.VatRate,
            VatTreatment = treatment,
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

        // Close matching payout request from every mark-paid path (admin page, redesign 06.4, run bulk).
        await CloseLinkedPayoutRequestAsync(paid, cancellationToken);

        return paid;
    }

    private async Task CloseLinkedPayoutRequestAsync(
        SelfBillingInvoice paid,
        CancellationToken cancellationToken)
    {
        SalesPayoutRequest? request = null;
        if (paid.SalesPayoutRequestId is Guid rid)
        {
            request = await _db.SalesPayoutRequests
                .FirstOrDefaultAsync(r => r.Id == rid, cancellationToken);
        }

        request ??= await _db.SalesPayoutRequests
            .FirstOrDefaultAsync(r => r.SelfBillingInvoiceId == paid.Id, cancellationToken);

        if (request is null || request.Status == SalesPayoutRequestStatus.Paid)
        {
            return;
        }

        request.Status = SalesPayoutRequestStatus.Paid;
        request.PaidAtUtc ??= paid.PaidAt ?? DateTime.UtcNow;
        request.SelfBillingInvoiceId ??= paid.Id;
        await _db.SaveChangesAsync(cancellationToken);

        if (request.SalesPayoutRunId is not Guid runId)
        {
            return;
        }

        var run = await _db.SalesPayoutRuns
            .Include(r => r.Requests)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (run is null)
        {
            return;
        }

        var anyOpen = run.Requests.Any(r =>
            r.Status is SalesPayoutRequestStatus.Approved or SalesPayoutRequestStatus.InRun);
        if (!anyOpen && run.Requests.Any(r => r.Status == SalesPayoutRequestStatus.Paid))
        {
            run.Status = SalesPayoutRunStatus.Closed;
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (run.Status is SalesPayoutRunStatus.Approved or SalesPayoutRunStatus.Exported)
        {
            run.Status = SalesPayoutRunStatus.Paid;
            await _db.SaveChangesAsync(cancellationToken);
        }
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
                sm.Iban,
                sm.VatTreatment);
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
                am.Iban,
                am.VatTreatment);
        }

        var partner = await _db.PartnerAffiliateProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .ThenInclude(u => u.Company)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (partner is not null)
        {
            var company = partner.User.Company;
            return new BillingIdentity(
                FirstNonEmpty(partner.CompanyName, company?.Name) ?? "",
                FirstNonEmpty(partner.KvkNumber, company?.KvkNumber) ?? "",
                partner.VatNumber ?? "",
                FirstNonEmpty(
                    FormatAddress(partner.Address, partner.PostalCode, partner.City, partner.Country),
                    company?.Address) ?? "",
                partner.IsOnboardingComplete,
                partner.Iban);
        }

        return null;
    }

    private sealed record BillingIdentity(
        string CompanyName,
        string KvkNumber,
        string VatNumber,
        string FormattedAddress,
        bool IsOnboardingComplete,
        string? Iban,
        SalesManagerVatTreatment VatTreatment = SalesManagerVatTreatment.Standard21);

    private sealed record SelectedLedgerAmount(
        CommissionLedgerEntry Entry,
        decimal AmountExVat,
        bool IsPartial);

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
