using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class VatBufferTransferService : IVatBufferTransferService
{
    private readonly JobsyDbContext _db;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly ILogger<VatBufferTransferService> _logger;

    public VatBufferTransferService(
        JobsyDbContext db,
        IPlatformCompanySettingsService companySettings,
        ILogger<VatBufferTransferService> logger)
    {
        _db = db;
        _companySettings = companySettings;
        _logger = logger;
    }

    public async Task<VatBufferTransfer> QueueForInvoiceAsync(
        TokenPurchaseInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.VatBufferTransfers
            .FirstOrDefaultAsync(t => t.TokenPurchaseInvoiceId == invoice.Id, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        if (invoice.VatAmountCents <= 0)
        {
            var skippedZero = new VatBufferTransfer
            {
                Id = Guid.NewGuid(),
                TokenPurchaseInvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                DestinationIban = "",
                AmountCents = 0,
                Status = VatBufferTransferStatus.SkippedNoIban,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                Note = "Geen BTW-bedrag om over te boeken."
            };
            _db.VatBufferTransfers.Add(skippedZero);
            await _db.SaveChangesAsync(cancellationToken);
            return skippedZero;
        }

        var platform = await _companySettings.GetAsync(cancellationToken);
        var iban = platform.VatBufferIban;
        var now = DateTime.UtcNow;

        var transfer = new VatBufferTransfer
        {
            Id = Guid.NewGuid(),
            TokenPurchaseInvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            DestinationIban = iban ?? "",
            AmountCents = invoice.VatAmountCents,
            Status = string.IsNullOrWhiteSpace(iban)
                ? VatBufferTransferStatus.SkippedNoIban
                : VatBufferTransferStatus.Pending,
            CreatedAt = now,
            ProcessedAt = string.IsNullOrWhiteSpace(iban) ? now : null,
            Note = string.IsNullOrWhiteSpace(iban)
                ? "Geen Knab BTW-IBAN geconfigureerd in Admin → Bedrijfsgegevens."
                : $"Omschrijving/kenmerk: {invoice.InvoiceNumber}"
        };

        _db.VatBufferTransfers.Add(transfer);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "BTW-buffer queued for invoice {InvoiceNumber}: {Cents} cents → {Iban} ({Status})",
            invoice.InvoiceNumber, transfer.AmountCents, MaskIban(transfer.DestinationIban), transfer.Status);

        return transfer;
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _db.VatBufferTransfers
            .Where(t => t.Status == VatBufferTransferStatus.Pending)
            .OrderBy(t => t.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return 0;
        }

        var processed = 0;
        foreach (var transfer in pending)
        {
            if (string.IsNullOrWhiteSpace(transfer.DestinationIban))
            {
                transfer.Status = VatBufferTransferStatus.SkippedNoIban;
                transfer.ProcessedAt = DateTime.UtcNow;
                transfer.Note = "Geen Knab BTW-IBAN geconfigureerd.";
                processed++;
                continue;
            }

            // Logboek-trigger: record the bank transfer order with invoice ID as omschrijving.
            // Actual Knab API payout can be wired later; the audit trail is complete.
            transfer.Status = VatBufferTransferStatus.Logged;
            transfer.ProcessedAt = DateTime.UtcNow;
            transfer.Note =
                $"Overboeking-opdracht gelogd. Bedrag € {transfer.AmountCents / 100m:0.00} naar {MaskIban(transfer.DestinationIban)}. Kenmerk: {transfer.InvoiceNumber}";

            _logger.LogInformation(
                "BTW-buffer transfer logged: {InvoiceNumber} €{Amount} → {Iban}",
                transfer.InvoiceNumber,
                transfer.AmountCents / 100m,
                MaskIban(transfer.DestinationIban));

            processed++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return processed;
    }

    public async Task<IReadOnlyList<VatBufferTransfer>> ListAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.VatBufferTransfers.AsNoTracking().AsQueryable();
        if (year is int y)
        {
            query = query.Where(t => t.CreatedAt.Year == y);
            if (quarter is int q && q is >= 1 and <= 4)
            {
                var startMonth = (q - 1) * 3 + 1;
                var endMonth = startMonth + 2;
                query = query.Where(t => t.CreatedAt.Month >= startMonth && t.CreatedAt.Month <= endMonth);
            }
        }

        return await query.OrderByDescending(t => t.CreatedAt).Take(2000).ToListAsync(cancellationToken);
    }

    private static string MaskIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban) || iban.Length < 8)
        {
            return "—";
        }

        return $"{iban[..4]}••••{iban[^4..]}";
    }
}
