using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public sealed class TokenPurchaseInvoiceService : ITokenPurchaseInvoiceService
{
    private readonly JobsyDbContext _db;
    private readonly IPlatformCompanySettingsService _companySettings;

    static TokenPurchaseInvoiceService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public TokenPurchaseInvoiceService(JobsyDbContext db, IPlatformCompanySettingsService companySettings)
    {
        _db = db;
        _companySettings = companySettings;
    }

    public async Task<TokenPurchaseInvoice> CreateForCheckoutAsync(
        TokenPurchaseCheckout checkout,
        TokenTransaction purchaseTransaction,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.TokenPurchaseInvoices
            .FirstOrDefaultAsync(i => i.TokenPurchaseCheckoutId == checkout.Id, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == checkout.CompanyId, cancellationToken)
            ?? throw new InvalidOperationException("Company not found.");

        EnsureCheckoutMoney(checkout);

        var now = DateTime.UtcNow;
        var invoice = new TokenPurchaseInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = await NextInvoiceNumberAsync(cancellationToken),
            TokenPurchaseCheckoutId = checkout.Id,
            TokenTransactionId = purchaseTransaction.Id,
            CompanyId = checkout.CompanyId,
            MolliePaymentId = checkout.PaymentId,
            PackSize = checkout.PackSize,
            AmountExVatCents = checkout.AmountExVatCents,
            VatAmountCents = checkout.VatAmountCents,
            TotalAmountCents = checkout.TotalAmountCents,
            VatRate = TokenVatPricing.VatRate,
            CompanyName = company.Name,
            CompanyKvkNumber = string.IsNullOrWhiteSpace(company.KvkNumber) ? null : company.KvkNumber,
            CompanyAddress = string.IsNullOrWhiteSpace(company.Address) ? null : company.Address,
            IssuedAt = now,
            CreatedAt = now
        };

        _db.TokenPurchaseInvoices.Add(invoice);
        await _db.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public Task<TokenPurchaseInvoice?> GetAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        => _db.TokenPurchaseInvoices.AsNoTracking()
            .Include(i => i.Company)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

    public Task<TokenPurchaseInvoice?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
        => _db.TokenPurchaseInvoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);

    public async Task<byte[]> RenderPdfAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await GetAsync(invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Factuur niet gevonden.");

        var platform = await _companySettings.GetAsync(cancellationToken);
        var logo = _companySettings.GetBrandLogoPng();
        var watermark = _companySettings.GetBrandWatermarkPng();
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var platformAddress = platform.FormatAddressBlock();

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
                        col.Item().AlignRight().Text("FACTUUR").FontSize(8)
                            .FontColor(Colors.Grey.Medium);
                        col.Item().AlignRight().Text(invoice.InvoiceNumber).FontSize(12).SemiBold();
                    });
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Factuur aan").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().Text(invoice.CompanyName).SemiBold();
                            if (!string.IsNullOrWhiteSpace(invoice.CompanyAddress))
                            {
                                c.Item().Text(invoice.CompanyAddress);
                            }

                            if (!string.IsNullOrWhiteSpace(invoice.CompanyKvkNumber))
                            {
                                c.Item().Text($"KvK {invoice.CompanyKvkNumber}");
                            }
                        });
                        row.ConstantItem(180).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Datum: {invoice.IssuedAt.ToLocalTime().ToString("dd-MM-yyyy", culture)}");
                            c.Item().Text($"Mollie: {invoice.MolliePaymentId}").FontSize(8)
                                .FontColor(Colors.Grey.Darken1);
                        });
                    });

                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingBottom(4).Text("Omschrijving").SemiBold();
                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingBottom(4).AlignRight().Text("Aantal").SemiBold();
                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingBottom(4).AlignRight().Text("Bedrag").SemiBold();
                        });

                        table.Cell().PaddingVertical(6)
                            .Text($"Lobsy tokens — pakket {invoice.PackSize}");
                        table.Cell().PaddingVertical(6).AlignRight().Text(invoice.PackSize.ToString(culture));
                        table.Cell().PaddingVertical(6).AlignRight()
                            .Text($"€ {TokenVatPricing.FormatEuro(invoice.AmountExVatCents)}");
                    });

                    col.Item().PaddingTop(12).AlignRight().Width(220).Column(totals =>
                    {
                        totals.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Subtotaal excl. BTW");
                            r.ConstantItem(90).AlignRight()
                                .Text($"€ {TokenVatPricing.FormatEuro(invoice.AmountExVatCents)}");
                        });
                        totals.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"BTW ({invoice.VatRate:P0})");
                            r.ConstantItem(90).AlignRight()
                                .Text($"€ {TokenVatPricing.FormatEuro(invoice.VatAmountCents)}");
                        });
                        totals.Item().PaddingTop(4).BorderTop(1).BorderColor(Colors.Grey.Lighten1)
                            .PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text("Totaal incl. BTW").SemiBold();
                                r.ConstantItem(90).AlignRight()
                                    .Text($"€ {TokenVatPricing.FormatEuro(invoice.TotalAmountCents)}").SemiBold();
                            });
                    });

                    col.Item().PaddingTop(24).Text(
                            "Betaling ontvangen via Mollie. Deze factuur is automatisch gegenereerd.")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(platform.CompanyName).FontSize(8).SemiBold();
                        if (!string.IsNullOrWhiteSpace(platformAddress))
                        {
                            c.Item().Text(platformAddress).FontSize(8);
                        }

                        var meta = string.Join(" · ", new[]
                        {
                            string.IsNullOrWhiteSpace(platform.KvkNumber) ? null : $"KvK {platform.KvkNumber}",
                            string.IsNullOrWhiteSpace(platform.VatNumber) ? null : $"BTW {platform.VatNumber}"
                        }.Where(s => s is not null));
                        if (!string.IsNullOrWhiteSpace(meta))
                        {
                            c.Item().Text(meta).FontSize(8);
                        }
                    });
                    row.ConstantItem(120).AlignRight().AlignMiddle()
                        .Text(invoice.InvoiceNumber).FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    private async Task<string> NextInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"LOB-TK-{year}-";
        var last = await _db.TokenPurchaseInvoices
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

    private static void EnsureCheckoutMoney(TokenPurchaseCheckout checkout)
    {
        if (checkout.TotalAmountCents > 0)
        {
            return;
        }

        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(checkout.AmountEuro);
        checkout.AmountExVatCents = ex;
        checkout.VatAmountCents = vat;
        checkout.TotalAmountCents = total;
    }
}
