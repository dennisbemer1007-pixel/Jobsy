using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class SalesManagerPayoutService : ISalesManagerPayoutService
{
    private readonly JobsyDbContext _db;
    private readonly ISelfBillingInvoiceService _invoices;
    private readonly ICommissionLedgerService _ledger;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<SalesManagerPayoutService> _logger;

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
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.SalesManagerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == salesManagerUserId, cancellationToken);

        if (profile is null)
        {
            return new SalesManagerPayoutPreviewDto(
                0, 0, 0, null, "—", false, "Salesmanager-profiel ontbreekt.");
        }

        if (!profile.IsOnboardingComplete)
        {
            return new SalesManagerPayoutPreviewDto(
                0, 0, 0, profile.Iban, ISalesManagerPayoutService.MaskIban(profile.Iban),
                false, "Onboarding moet compleet zijn vóór uitbetaling.");
        }

        if (string.IsNullOrWhiteSpace(profile.Iban))
        {
            return new SalesManagerPayoutPreviewDto(
                0, 0, 0, null, "—", false,
                "Vul eerst je IBAN in bij onboarding om uit te laten betalen.");
        }

        var uninvoiced = await _ledger.GetUninvoicedBalanceExVatAsync(salesManagerUserId, cancellationToken);
        if (uninvoiced <= 0)
        {
            return new SalesManagerPayoutPreviewDto(
                0, 0, 0, profile.Iban, ISalesManagerPayoutService.MaskIban(profile.Iban),
                false, "Geen openstaand tegoed om uit te betalen.");
        }

        var vat = SalesCommissionRules.VatOn(uninvoiced);
        return new SalesManagerPayoutPreviewDto(
            uninvoiced,
            vat,
            uninvoiced + vat,
            profile.Iban,
            ISalesManagerPayoutService.MaskIban(profile.Iban),
            true,
            null);
    }

    public async Task<SalesManagerPayoutCheckoutResult> CreateCheckoutAsync(
        Guid salesManagerUserId,
        CancellationToken cancellationToken = default)
    {
        var preview = await GetPreviewAsync(salesManagerUserId, cancellationToken);
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
            var invoice = await _invoices.CreateFromUninvoicedBalanceAsync(
                salesManagerUserId, cancellationToken);
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

    public async Task<string> RenderInvoiceHtmlAsync(
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
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\"/>");
        sb.Append("<title>").Append(WebUtility.HtmlEncode(invoice.InvoiceNumber)).Append("</title>");
        sb.Append("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:2rem;color:#111}");
        sb.Append("h1{font-size:1.4rem}table{width:100%;border-collapse:collapse;margin-top:1.25rem}");
        sb.Append("th,td{border-bottom:1px solid #ddd;padding:.45rem .2rem;text-align:left}");
        sb.Append("td.num,th.num{text-align:right}.muted{color:#666}.totals{margin-top:1rem}</style></head><body>");
        sb.Append("<p class=\"muted\">Self-billing factuur</p>");
        sb.Append("<h1>").Append(WebUtility.HtmlEncode(invoice.InvoiceNumber)).Append("</h1>");
        sb.Append("<p><strong>").Append(WebUtility.HtmlEncode(invoice.SalesManagerCompanyName)).Append("</strong><br/>");
        sb.Append("KvK ").Append(WebUtility.HtmlEncode(invoice.SalesManagerKvkNumber)).Append("<br/>");
        sb.Append("BTW ").Append(WebUtility.HtmlEncode(invoice.SalesManagerVatNumber)).Append("<br/>");
        sb.Append(WebUtility.HtmlEncode(invoice.SalesManagerAddress)).Append("</p>");
        sb.Append("<p class=\"muted\">Status: ").Append(WebUtility.HtmlEncode(invoice.Status.ToString()));
        if (invoice.IssuedAt is DateTime issued)
        {
            sb.Append(" · Uitgegeven ").Append(issued.ToLocalTime().ToString("g", culture));
        }

        if (invoice.PaidAt is DateTime paid)
        {
            sb.Append(" · Betaald ").Append(paid.ToLocalTime().ToString("g", culture));
        }

        sb.Append("</p><table><thead><tr><th>Omschrijving</th><th class=\"num\">Bedrag excl. BTW</th></tr></thead><tbody>");
        foreach (var line in invoice.Lines.OrderBy(l => l.Description))
        {
            sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(line.Description)).Append("</td>");
            sb.Append("<td class=\"num\">€ ").Append(line.AmountExVat.ToString("0.00", culture)).Append("</td></tr>");
        }

        sb.Append("</tbody></table><div class=\"totals\">");
        sb.Append("<div>Subtotaal excl. BTW: € ").Append(invoice.SubtotalExVat.ToString("0.00", culture)).Append("</div>");
        sb.Append("<div>BTW (").Append((invoice.VatRate * 100).ToString("0", culture)).Append("%): € ")
            .Append(invoice.VatAmount.ToString("0.00", culture)).Append("</div>");
        sb.Append("<div><strong>Totaal incl. BTW: € ")
            .Append(invoice.TotalInclVat.ToString("0.00", culture)).Append("</strong></div></div>");
        sb.Append("<p class=\"muted\" style=\"margin-top:2rem\">Gegenereerd door Lobsy self-billing.</p></body></html>");
        return sb.ToString();
    }
}
