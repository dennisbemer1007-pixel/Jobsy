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

public sealed class ConsumerInvoiceService : IConsumerInvoiceService
{
    private readonly JobsyDbContext _db;
    private readonly IPlatformCompanySettingsService _companySettings;

    static ConsumerInvoiceService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ConsumerInvoiceService(JobsyDbContext db, IPlatformCompanySettingsService companySettings)
    {
        _db = db;
        _companySettings = companySettings;
    }

    public async Task<ConsumerPurchaseInvoice> CreateForDeepCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.ConsumerPurchaseInvoices
            .FirstOrDefaultAsync(i => i.DeepAnalysisCheckoutId == checkoutId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var checkout = await _db.DeepAnalysisCheckouts
            .FirstOrDefaultAsync(c => c.Id == checkoutId, cancellationToken)
            ?? throw new InvalidOperationException("Checkout niet gevonden.");

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == checkout.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Gebruiker niet gevonden.");

        EnsureCheckoutMoney(checkout);

        var now = DateTime.UtcNow;
        var invoice = new ConsumerPurchaseInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = await NextInvoiceNumberAsync(cancellationToken),
            DeepAnalysisCheckoutId = checkout.Id,
            UserId = checkout.UserId,
            CustomerName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName,
            CustomerEmail = user.Email,
            CustomerCountry = null,
            Description = DeepAnalysisPricing.DescriptionNl(checkout.Kind),
            Kind = checkout.Kind,
            AmountExVatCents = checkout.AmountExVatCents,
            VatAmountCents = checkout.VatAmountCents,
            TotalAmountCents = checkout.TotalAmountCents,
            VatRate = TokenVatPricing.VatRate,
            MolliePaymentId = checkout.PaymentId,
            PaymentMethod = checkout.PaymentMethod,
            IsStub = checkout.IsStub,
            IssuedAt = now,
            CreatedAt = now
        };

        _db.ConsumerPurchaseInvoices.Add(invoice);
        checkout.InvoiceId = invoice.Id;
        await _db.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public Task<ConsumerPurchaseInvoice?> GetAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        => _db.ConsumerPurchaseInvoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

    public async Task<IReadOnlyList<ConsumerPurchaseInvoice>> ListAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default)
    {
        var q = _db.ConsumerPurchaseInvoices.AsNoTracking().AsQueryable();
        if (year is int y)
        {
            var start = new DateTime(y, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddYears(1);
            if (quarter is int qq and >= 1 and <= 4)
            {
                start = new DateTime(y, (qq - 1) * 3 + 1, 1, 0, 0, 0, DateTimeKind.Utc);
                end = start.AddMonths(3);
            }

            q = q.Where(i => i.IssuedAt >= start && i.IssuedAt < end);
        }

        return await q.OrderByDescending(i => i.IssuedAt).ToListAsync(cancellationToken);
    }

    public async Task<byte[]> RenderPdfAsync(
        Guid invoiceId,
        string? culture = null,
        CancellationToken cancellationToken = default)
    {
        var invoice = await GetAsync(invoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Factuur niet gevonden.");

        var platform = await _companySettings.GetAsync(cancellationToken);
        var logo = _companySettings.GetBrandLogoPng();
        var watermark = _companySettings.GetBrandWatermarkPng();
        var cultureInfo = CultureInfo.GetCultureInfo(
            string.Equals(culture, "en", StringComparison.OrdinalIgnoreCase) ? "en-US" : "nl-NL");
        var amsterdam = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "W. Europe Standard Time" : "Europe/Amsterdam");
        var localIssued = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(invoice.IssuedAt, DateTimeKind.Utc), amsterdam);
        var platformAddress = platform.FormatAddressBlock();
        var testBanner = invoice.IsStub ? "TESTFACTUUR – geen betaling" : null;

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
                    if (testBanner is not null)
                    {
                        col.Item().Background(Colors.Orange.Lighten4).Padding(8)
                            .Text(testBanner).SemiBold().FontColor(Colors.Orange.Darken2);
                    }

                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Factuur aan").FontSize(8).FontColor(Colors.Grey.Medium);
                            c.Item().Text(invoice.CustomerName).SemiBold();
                            c.Item().Text(invoice.CustomerEmail);
                            if (!string.IsNullOrWhiteSpace(invoice.CustomerCountry))
                            {
                                c.Item().Text(invoice.CustomerCountry);
                            }
                        });
                        row.ConstantItem(180).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Datum: {localIssued.ToString("dd-MM-yyyy HH:mm", cultureInfo)}");
                            c.Item().Text(invoice.IsStub
                                    ? "Testbetaling"
                                    : $"Betaald via Mollie ({MolliePaymentMethods.DisplayName(invoice.PaymentMethod)})")
                                .FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"Mollie: {invoice.MolliePaymentId}").FontSize(8)
                                .FontColor(Colors.Grey.Darken1);
                        });
                    });

                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingBottom(4).Text("Omschrijving").SemiBold();
                            header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingBottom(4).AlignRight().Text("Bedrag").SemiBold();
                        });

                        table.Cell().PaddingVertical(6).Text(invoice.Description);
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
                                r.ConstantItem(90).AlignRight().Text($"€ {TokenVatPricing.FormatEuro(invoice.TotalAmountCents)}")
                                    .SemiBold();
                            });
                    });

                    col.Item().PaddingTop(24).Text(platformAddress).FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrWhiteSpace(platform.VatNumber))
                    {
                        col.Item().Text($"BTW {platform.VatNumber}").FontSize(8)
                            .FontColor(Colors.Grey.Darken1);
                    }
                });
            });
        }).GeneratePdf();
    }

    private static void EnsureCheckoutMoney(DeepAnalysisCheckout checkout)
    {
        if (checkout.TotalAmountCents > 0)
        {
            return;
        }

        var split = TokenVatPricing.SplitInclVatEuros(checkout.AmountEuro);
        checkout.AmountExVatCents = split.ExVatCents;
        checkout.VatAmountCents = split.VatCents;
        checkout.TotalAmountCents = split.TotalCents;
    }

    private async Task<string> NextInvoiceNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"LOB-KT-{year}-";
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var last = await _db.ConsumerPurchaseInvoices.AsNoTracking()
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var next = 1;
            if (last is not null
                && last.Length > prefix.Length
                && int.TryParse(last[prefix.Length..], out var parsed))
            {
                next = parsed + 1;
            }

            var candidate = $"{prefix}{next:0000}";
            var clash = await _db.ConsumerPurchaseInvoices.AsNoTracking()
                .AnyAsync(i => i.InvoiceNumber == candidate, cancellationToken);
            if (!clash)
            {
                return candidate;
            }
        }

        return $"{prefix}{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
    }
}
