using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SalesManagerPayoutService> _logger;

    static SalesManagerPayoutService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public SalesManagerPayoutService(
        JobsyDbContext db,
        ISelfBillingInvoiceService invoices,
        ICommissionLedgerService ledger,
        IPlatformCompanySettingsService companySettings,
        IHostEnvironment environment,
        IConfiguration configuration,
        ILogger<SalesManagerPayoutService> logger)
    {
        _db = db;
        _invoices = invoices;
        _ledger = ledger;
        _companySettings = companySettings;
        _environment = environment;
        _configuration = configuration;
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
                0, 0, 0, 0, null, masked,
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
                0, 0, 0, 0, null, masked,
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
                    available, 0, 0, 0, null, masked,
                    false, "Kies een bedrag groter dan € 0,00.");
            }

            if (amountExVat > available)
            {
                return new SalesManagerPayoutPreviewDto(
                    available, available, SalesCommissionRules.VatOn(available), SalesCommissionRules.InclVat(available),
                    null, masked,
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
            null,
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

        // Stub payouts only in Development / explicit demo auth (live Mollie not wired yet).
        if (!AllowStubPayouts())
        {
            throw new InvalidOperationException(
                "Live uitbetaling (Mollie) is nog niet geconfigureerd. Neem contact op met Lobsy.");
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

        // Development / demo stub: Pending → Paid (real Mollie webhook would set Paid).
        if (AllowStubPayouts()
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

        var platform = await _companySettings.GetAsync(cancellationToken);
        var logo = _companySettings.GetBrandLogoPng();
        var watermark = _companySettings.GetBrandWatermarkPng();
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var lines = invoice.Lines.OrderBy(l => l.Description).ToList();
        var platformAddress = platform.FormatAddressBlock();

        // Half A4 ≈ half page height on portrait A4 (~421pt). Keep logo square in the center.
        const float watermarkSize = 400f;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(36);
                page.MarginBottom(36);
                page.MarginHorizontal(42);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Background()
                    .AlignCenter()
                    .AlignMiddle()
                    .Width(watermarkSize)
                    .Height(watermarkSize)
                    .Image(watermark)
                    .FitArea();

                page.Header().Row(row =>
                {
                    row.ConstantItem(52).Height(52).Image(logo).FitArea();
                    row.RelativeItem().PaddingLeft(12).AlignMiddle().Column(col =>
                    {
                        col.Item().Text(platform.CompanyName).FontSize(18).SemiBold()
                            .FontColor(Color.FromHex("#0F766E"));
                        col.Item().PaddingTop(2).Text(platform.Slogan).FontSize(9)
                            .FontColor(Colors.Grey.Darken2).Italic();
                    });
                    row.ConstantItem(140).AlignRight().AlignMiddle().Column(col =>
                    {
                        col.Item().AlignRight().Text("SELF-BILLING").FontSize(8)
                            .FontColor(Colors.Grey.Medium);
                        col.Item().AlignRight().Text(invoice.InvoiceNumber).FontSize(12).SemiBold();
                    });
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Spacing(0);

                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("Leverancier").FontSize(8).FontColor(Colors.Grey.Medium);
                            left.Item().PaddingTop(4).Text(invoice.SalesManagerCompanyName).SemiBold().FontSize(11);
                            left.Item().PaddingTop(2).Text($"KvK {invoice.SalesManagerKvkNumber}");
                            left.Item().Text($"BTW {invoice.SalesManagerVatNumber}");
                            left.Item().PaddingTop(2).Text(invoice.SalesManagerAddress);
                        });

                        row.ConstantItem(18);
                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("Status").FontSize(8).FontColor(Colors.Grey.Medium);
                            right.Item().PaddingTop(4).Text(invoice.Status.ToString()).SemiBold();
                            if (invoice.IssuedAt is DateTime issued)
                            {
                                right.Item().PaddingTop(2)
                                    .Text($"Uitgegeven {issued.ToLocalTime().ToString("g", culture)}");
                            }

                            if (invoice.PaidAt is DateTime paid)
                            {
                                right.Item().Text($"Betaald {paid.ToLocalTime().ToString("g", culture)}");
                            }
                        });
                    });

                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(28);
                            columns.RelativeColumn(3.2f);
                            columns.RelativeColumn(1.2f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("#");
                            header.Cell().Element(HeaderCell).Text("Omschrijving");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Bedrag excl. BTW");
                        });

                        var i = 1;
                        foreach (var line in lines)
                        {
                            var zebra = i % 2 == 0;
                            table.Cell().Element(c => BodyCell(c, zebra)).Text(i.ToString(culture));
                            table.Cell().Element(c => BodyCell(c, zebra)).Text(line.Description);
                            table.Cell().Element(c => BodyCell(c, zebra)).AlignRight()
                                .Text($"€ {line.AmountExVat.ToString("0.00", culture)}");
                            i++;
                        }
                    });

                    col.Item().PaddingTop(14).AlignRight().Width(220).Column(totals =>
                    {
                        totals.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Subtotaal excl. BTW");
                            r.ConstantItem(90).AlignRight()
                                .Text($"€ {invoice.SubtotalExVat.ToString("0.00", culture)}");
                        });
                        totals.Item().PaddingTop(3).Row(r =>
                        {
                            r.RelativeItem().Text($"BTW ({(invoice.VatRate * 100).ToString("0", culture)}%)");
                            r.ConstantItem(90).AlignRight()
                                .Text($"€ {invoice.VatAmount.ToString("0.00", culture)}");
                        });
                        totals.Item().PaddingTop(6).BorderTop(1).BorderColor(Colors.Grey.Lighten1)
                            .PaddingTop(6).Row(r =>
                            {
                                r.RelativeItem().Text("Totaal incl. BTW").SemiBold().FontSize(11);
                                r.ConstantItem(90).AlignRight()
                                    .Text($"€ {invoice.TotalInclVat.ToString("0.00", culture)}")
                                    .SemiBold().FontSize(11);
                            });
                    });
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10).Column(plat =>
                    {
                        plat.Item().Text(platform.CompanyName).SemiBold().FontSize(9)
                            .FontColor(Color.FromHex("#0F766E"));
                        if (!string.IsNullOrWhiteSpace(platformAddress))
                        {
                            foreach (var line in platformAddress.Split('\n'))
                            {
                                plat.Item().Text(line).FontSize(8).FontColor(Colors.Grey.Darken2);
                            }
                        }

                        var meta = new List<string>();
                        if (!string.IsNullOrWhiteSpace(platform.KvkNumber))
                        {
                            meta.Add($"KvK {platform.KvkNumber}");
                        }

                        if (!string.IsNullOrWhiteSpace(platform.VatNumber))
                        {
                            meta.Add($"BTW {platform.VatNumber}");
                        }

                        if (!string.IsNullOrWhiteSpace(platform.Phone))
                        {
                            meta.Add(platform.Phone!);
                        }

                        if (!string.IsNullOrWhiteSpace(platform.Email))
                        {
                            meta.Add(platform.Email!);
                        }

                        if (meta.Count > 0)
                        {
                            plat.Item().PaddingTop(2).Text(string.Join("  ·  ", meta))
                                .FontSize(8).FontColor(Colors.Grey.Darken2);
                        }
                    });

                    footer.Item().PaddingTop(6).AlignCenter()
                        .Text("Self-billing factuur gegenereerd door Lobsy")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();

        static IContainer HeaderCell(IContainer container) =>
            container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Background(Color.FromHex("#F3FAF9"))
                .PaddingVertical(6)
                .PaddingHorizontal(4)
                .DefaultTextStyle(x => x.SemiBold().FontSize(9).FontColor(Colors.Grey.Darken3));

        static IContainer BodyCell(IContainer container, bool zebra) =>
            container
                .Background(zebra ? Color.FromHex("#FAFCFC") : Colors.White)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten3)
                .PaddingVertical(5)
                .PaddingHorizontal(4);
    }

    private bool AllowStubPayouts() =>
        _environment.IsDevelopment()
        || _configuration.GetValue("JobsyAuth:AllowDevelopmentAuth", false);
}
