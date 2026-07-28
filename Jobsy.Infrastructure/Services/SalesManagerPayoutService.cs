using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public sealed class SalesManagerPayoutService : ISalesManagerPayoutService
{
    private readonly JobsyDbContext _db;
    private readonly ISelfBillingInvoiceService _invoices;
    private readonly ICommissionLedgerService _ledger;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SalesManagerPayoutService> _logger;

    static SalesManagerPayoutService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public SalesManagerPayoutService(
        JobsyDbContext db,
        ISelfBillingInvoiceService invoices,
        ICommissionLedgerService ledger,
        IHostEnvironment environment,
        ILogger<SalesManagerPayoutService> logger)
    {
        _db = db;
        _invoices = invoices;
        _ledger = ledger;
        _environment = environment;
        _logger = logger;
    }

    public async Task<SalesManagerPayoutPreviewDto> GetPreviewAsync(
        Guid salesManagerUserId,
        decimal? requestedAmountExVat = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.SalesManagerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == salesManagerUserId, cancellationToken);

        if (profile is null)
        {
            return new SalesManagerPayoutPreviewDto(
                0, 0, 0, 0, null, "—", false, "Salesmanager-profiel ontbreekt.");
        }

        var masked = ISalesManagerPayoutService.MaskIban(profile.Iban);

        if (!profile.IsOnboardingComplete)
        {
            return new SalesManagerPayoutPreviewDto(
                0, 0, 0, 0, profile.Iban, masked,
                false, "Onboarding moet compleet zijn vóór uitbetaling.");
        }

        if (string.IsNullOrWhiteSpace(profile.Iban))
        {
            return new SalesManagerPayoutPreviewDto(
                0, 0, 0, 0, null, "—", false,
                "Vul eerst je IBAN in bij onboarding om uit te laten betalen.");
        }

        var available = await _ledger.GetUninvoicedBalanceExVatAsync(salesManagerUserId, cancellationToken);
        available = decimal.Round(available, 2, MidpointRounding.AwayFromZero);
        if (available <= 0)
        {
            return new SalesManagerPayoutPreviewDto(
                0, 0, 0, 0, profile.Iban, masked,
                false, "Geen openstaand tegoed om uit te betalen.");
        }

        decimal amountExVat;
        if (requestedAmountExVat is null)
        {
            amountExVat = available;
        }
        else
        {
            amountExVat = decimal.Round(requestedAmountExVat.Value, 2, MidpointRounding.AwayFromZero);
            if (amountExVat <= 0)
            {
                return new SalesManagerPayoutPreviewDto(
                    available, 0, 0, 0, profile.Iban, masked,
                    false, "Kies een bedrag groter dan € 0,00.");
            }

            if (amountExVat > available)
            {
                return new SalesManagerPayoutPreviewDto(
                    available, available, SalesCommissionRules.VatOn(available), SalesCommissionRules.InclVat(available),
                    profile.Iban, masked,
                    false,
                    $"Bedrag mag niet hoger zijn dan je openstaande tegoed (€ {available:0.00} excl. BTW).");
            }
        }

        var vat = SalesCommissionRules.VatOn(amountExVat);
        return new SalesManagerPayoutPreviewDto(
            available,
            amountExVat,
            vat,
            amountExVat + vat,
            profile.Iban,
            masked,
            true,
            null);
    }

    public async Task<SalesManagerPayoutCheckoutResult> CreateCheckoutAsync(
        Guid salesManagerUserId,
        decimal requestedAmountExVat,
        CancellationToken cancellationToken = default)
    {
        var preview = await GetPreviewAsync(salesManagerUserId, requestedAmountExVat, cancellationToken);
        if (!preview.CanPayout)
        {
            throw new InvalidOperationException(preview.BlockReason ?? "Uitbetaling niet mogelijk.");
        }

        // Cancel stale open sessions for this salesmanager.
        var open = await _db.SalesManagerPayoutCheckouts
            .Where(c => c.SalesManagerUserId == salesManagerUserId
                        && (c.Status == SalesManagerPayoutCheckoutStatus.Pending
                            || c.Status == SalesManagerPayoutCheckoutStatus.Paid))
            .ToListAsync(cancellationToken);
        foreach (var prior in open)
        {
            prior.Status = SalesManagerPayoutCheckoutStatus.Cancelled;
        }

        // TODO(real-Mollie): create a Mollie payout/transfer to the SM bank account and use the returned id/url.
        var paymentId = $"stub_payout_{Guid.NewGuid():N}";
        _db.SalesManagerPayoutCheckouts.Add(new SalesManagerPayoutCheckout
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            SalesManagerUserId = salesManagerUserId,
            AmountEuro = preview.AmountInclVat,
            AmountExVat = preview.AmountExVat,
            VatAmount = preview.VatAmount,
            MaskedIban = preview.MaskedIban,
            Status = SalesManagerPayoutCheckoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "SalesManager payout stub checkout {PaymentId}: €{Amount} → {MaskedIban}",
            paymentId, preview.AmountInclVat, preview.MaskedIban);

        return new SalesManagerPayoutCheckoutResult(
            paymentId,
            $"https://localhost:5201/salesmanager/payout-checkout?paymentId={Uri.EscapeDataString(paymentId)}",
            preview.AmountInclVat,
            preview.MaskedIban,
            IsStub: true);
    }

    public async Task<SalesManagerPayoutCompleteResult> CompleteCheckoutAsync(
        string paymentId,
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            throw new ArgumentException("Ongeldige checkout.");
        }

        var session = await _db.SalesManagerPayoutCheckouts
            .FirstOrDefaultAsync(c => c.PaymentId == paymentId, cancellationToken)
            ?? throw new KeyNotFoundException("Uitbetalings-checkout niet gevonden.");

        if (session.SalesManagerUserId != salesManagerUserId)
        {
            throw new UnauthorizedAccessException("Checkout hoort niet bij deze salesmanager.");
        }

        if (session.Status == SalesManagerPayoutCheckoutStatus.Completed
            && session.SelfBillingInvoiceId is Guid existingId)
        {
            var existing = await _invoices.GetAsync(existingId, cancellationToken)
                ?? throw new KeyNotFoundException("Factuur niet gevonden.");
            return new SalesManagerPayoutCompleteResult(
                existing.Id,
                existing.InvoiceNumber,
                existing.TotalInclVat,
                session.MaskedIban,
                nameof(SalesManagerPayoutCheckoutStatus.Completed));
        }

        if (session.Status == SalesManagerPayoutCheckoutStatus.Cancelled)
        {
            throw new InvalidOperationException("Checkout is geannuleerd.");
        }

        // Development stub: Pending → Paid (real Mollie webhook would set Paid).
        if (_environment.IsDevelopment()
            && session.PaymentId.StartsWith("stub_payout_", StringComparison.Ordinal)
            && session.Status == SalesManagerPayoutCheckoutStatus.Pending)
        {
            session.Status = SalesManagerPayoutCheckoutStatus.Paid;
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (session.Status is not (SalesManagerPayoutCheckoutStatus.Paid
            or SalesManagerPayoutCheckoutStatus.Completed))
        {
            throw new InvalidOperationException(
                "Uitbetaling is nog niet bevestigd door Mollie. (In Development: gebruik de stub-pagina.)");
        }

        // Atomic claim Pending/Paid → Completed before creating invoice.
        int claimed;
        try
        {
            claimed = await _db.SalesManagerPayoutCheckouts
                .Where(c => c.Id == session.Id
                            && (c.Status == SalesManagerPayoutCheckoutStatus.Pending
                                || c.Status == SalesManagerPayoutCheckoutStatus.Paid))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(c => c.Status, SalesManagerPayoutCheckoutStatus.Completed)
                        .SetProperty(c => c.CompletedAt, DateTime.UtcNow),
                    cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // InMemory provider: fall back to tracked update.
            session = await _db.SalesManagerPayoutCheckouts.FirstAsync(c => c.Id == session.Id, cancellationToken);
            if (session.Status == SalesManagerPayoutCheckoutStatus.Completed
                && session.SelfBillingInvoiceId is Guid alreadyId)
            {
                var already = await _invoices.GetAsync(alreadyId, cancellationToken)
                    ?? throw new KeyNotFoundException("Factuur niet gevonden.");
                return new SalesManagerPayoutCompleteResult(
                    already.Id, already.InvoiceNumber, already.TotalInclVat,
                    session.MaskedIban, nameof(SalesManagerPayoutCheckoutStatus.Completed));
            }

            if (session.Status is not (SalesManagerPayoutCheckoutStatus.Pending
                or SalesManagerPayoutCheckoutStatus.Paid))
            {
                throw new InvalidOperationException("Checkout kan niet worden afgerond.");
            }

            session.Status = SalesManagerPayoutCheckoutStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            claimed = 1;
        }

        if (claimed == 0)
        {
            var refreshed = await _db.SalesManagerPayoutCheckouts
                .AsNoTracking()
                .FirstAsync(c => c.Id == session.Id, cancellationToken);
            if (refreshed.SelfBillingInvoiceId is Guid id)
            {
                var inv = await _invoices.GetAsync(id, cancellationToken)
                    ?? throw new KeyNotFoundException("Factuur niet gevonden.");
                return new SalesManagerPayoutCompleteResult(
                    inv.Id, inv.InvoiceNumber, inv.TotalInclVat,
                    refreshed.MaskedIban, nameof(SalesManagerPayoutCheckoutStatus.Completed));
            }

            throw new InvalidOperationException("Checkout kon niet worden geclaimd.");
        }

        try
        {
            // Use the amount locked on the checkout session (not the live full balance).
            var invoice = await _invoices.CreateFromUninvoicedBalanceAsync(
                salesManagerUserId, session.AmountExVat, cancellationToken);
            invoice = await _invoices.MarkPaidAsync(invoice.Id, cancellationToken);

            session = await _db.SalesManagerPayoutCheckouts.FirstAsync(c => c.Id == session.Id, cancellationToken);
            session.SelfBillingInvoiceId = invoice.Id;
            session.Status = SalesManagerPayoutCheckoutStatus.Completed;
            session.CompletedAt ??= DateTime.UtcNow;

            var amountText = invoice.TotalInclVat.ToString("0.00", CultureInfo.GetCultureInfo("nl-NL"));
            _db.PlatformLogs.Add(new PlatformLog
            {
                Id = Guid.NewGuid(),
                Level = PlatformLogLevel.Info,
                Category = "SalesManagerPayout",
                Message =
                    $"Uitbetaling naar rekening {session.MaskedIban} bedrag € {amountText}",
                DetailsJson =
                    $"{{\"paymentId\":\"{session.PaymentId}\",\"invoiceId\":\"{invoice.Id}\",\"invoiceNumber\":\"{invoice.InvoiceNumber}\"}}",
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "SalesManager payout completed {PaymentId}: invoice {InvoiceNumber} → {MaskedIban}",
                session.PaymentId, invoice.InvoiceNumber, session.MaskedIban);

            return new SalesManagerPayoutCompleteResult(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.TotalInclVat,
                session.MaskedIban,
                nameof(SalesManagerPayoutCheckoutStatus.Completed));
        }
        catch
        {
            // Revert claim so the SM can retry.
            try
            {
                await _db.SalesManagerPayoutCheckouts
                    .Where(c => c.Id == session.Id && c.Status == SalesManagerPayoutCheckoutStatus.Completed
                                && c.SelfBillingInvoiceId == null)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(c => c.Status, SalesManagerPayoutCheckoutStatus.Paid)
                            .SetProperty(c => c.CompletedAt, (DateTime?)null),
                        cancellationToken);
            }
            catch (InvalidOperationException)
            {
                var tracked = await _db.SalesManagerPayoutCheckouts.FirstAsync(c => c.Id == session.Id, cancellationToken);
                if (tracked.SelfBillingInvoiceId is null)
                {
                    tracked.Status = SalesManagerPayoutCheckoutStatus.Paid;
                    tracked.CompletedAt = null;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }

            throw;
        }
    }

    public async Task<byte[]> RenderInvoicePdfAsync(
        Guid invoiceId,
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _invoices.GetAsync(invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Factuur niet gevonden.");

        if (invoice.SalesManagerUserId != salesManagerUserId)
        {
            throw new UnauthorizedAccessException("Factuur hoort niet bij deze salesmanager.");
        }

        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var lines = invoice.Lines.OrderBy(l => l.Description).ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Column(col =>
                {
                    col.Item().Text("Self-billing factuur").FontSize(11).FontColor(Colors.Grey.Darken2);
                    col.Item().Text(invoice.InvoiceNumber).FontSize(20).SemiBold();
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text(invoice.SalesManagerCompanyName).SemiBold().FontSize(12);
                    col.Item().Text($"KvK {invoice.SalesManagerKvkNumber}");
                    col.Item().Text($"BTW {invoice.SalesManagerVatNumber}");
                    col.Item().Text(invoice.SalesManagerAddress);

                    var statusLine = $"Status: {invoice.Status}";
                    if (invoice.IssuedAt is DateTime issued)
                    {
                        statusLine += $" · Uitgegeven {issued.ToLocalTime().ToString("g", culture)}";
                    }

                    if (invoice.PaidAt is DateTime paid)
                    {
                        statusLine += $" · Betaald {paid.ToLocalTime().ToString("g", culture)}";
                    }

                    col.Item().PaddingTop(8).Text(statusLine).FontColor(Colors.Grey.Darken2);

                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingBottom(4).Text("Omschrijving").SemiBold();
                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingBottom(4).AlignRight().Text("Bedrag excl. BTW").SemiBold();
                        });

                        foreach (var line in lines)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .PaddingVertical(4).Text(line.Description);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .PaddingVertical(4).AlignRight()
                                .Text($"€ {line.AmountExVat.ToString("0.00", culture)}");
                        }
                    });

                    col.Item().AlignRight().PaddingTop(12).Column(totals =>
                    {
                        totals.Item().Text($"Subtotaal excl. BTW: € {invoice.SubtotalExVat.ToString("0.00", culture)}");
                        totals.Item().Text(
                            $"BTW ({(invoice.VatRate * 100).ToString("0", culture)}%): € {invoice.VatAmount.ToString("0.00", culture)}");
                        totals.Item().PaddingTop(4)
                            .Text($"Totaal incl. BTW: € {invoice.TotalInclVat.ToString("0.00", culture)}")
                            .SemiBold().FontSize(12);
                    });
                });

                page.Footer().AlignCenter()
                    .Text("Gegenereerd door Lobsy self-billing.")
                    .FontSize(9).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();
    }
}
