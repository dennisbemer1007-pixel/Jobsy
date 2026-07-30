using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public sealed class VatDeclarationService : IVatDeclarationService
{
    private readonly JobsyDbContext _db;
    private readonly IPlatformCompanySettingsService _companySettings;

    static VatDeclarationService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public VatDeclarationService(JobsyDbContext db, IPlatformCompanySettingsService companySettings)
    {
        _db = db;
        _companySettings = companySettings;
    }

    public async Task<IReadOnlyList<VatOpenPeriodDto>> GetOpenPeriodsAsync(
        CancellationToken cancellationToken = default)
    {
        var tokenDates = await _db.TokenPurchaseInvoices.AsNoTracking()
            .Where(i => i.VatDeclarationId == null)
            .Select(i => i.IssuedAt)
            .ToListAsync(cancellationToken);

        var smDates = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.VatDeclarationId == null
                        && i.Status == SelfBillingInvoiceStatus.Paid
                        && i.PaidAt != null)
            .Select(i => i.PaidAt!.Value)
            .ToListAsync(cancellationToken);

        var periods = tokenDates.Concat(smDates)
            .Select(d => (d.Year, Quarter: ((d.Month - 1) / 3) + 1))
            .Distinct()
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Quarter)
            .ToList();

        // Always offer current + previous quarter even if empty (admin can still open wizard).
        var now = DateTime.UtcNow;
        var currentQ = ((now.Month - 1) / 3) + 1;
        EnsurePeriod(periods, now.Year, currentQ);
        var prev = currentQ == 1 ? (now.Year - 1, 4) : (now.Year, currentQ - 1);
        EnsurePeriod(periods, prev.Item1, prev.Item2);

        var result = new List<VatOpenPeriodDto>();
        foreach (var (year, quarter) in periods.OrderByDescending(p => p.Year).ThenByDescending(p => p.Quarter))
        {
            var preview = await PreviewAsync(year, quarter, cancellationToken);
            result.Add(new VatOpenPeriodDto(
                year,
                quarter,
                preview.PeriodLabel,
                preview.TokenInvoiceCount,
                preview.SalesManagerInvoiceCount,
                !preview.AlreadyDeclared
                    && (preview.TokenInvoiceCount > 0 || preview.SalesManagerInvoiceCount > 0 || preview.GoodwillCount > 0)));
        }

        return result;
    }

    public async Task<VatDeclarationPreviewDto> PreviewAsync(
        int year,
        int quarter,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, quarter);
        var label = PeriodLabel(year, quarter);
        var (start, end) = PeriodBounds(year, quarter);

        var already = await _db.VatDeclarations.AsNoTracking()
            .AnyAsync(d => d.Year == year && d.Quarter == quarter
                           && d.Status == VatDeclarationStatus.Confirmed, cancellationToken);

        var tokens = await _db.TokenPurchaseInvoices.AsNoTracking()
            .Where(i => i.VatDeclarationId == null
                        && i.IssuedAt >= start && i.IssuedAt < end)
            .ToListAsync(cancellationToken);

        var goodwillCount = await _db.TokenTransactions.AsNoTracking()
            .CountAsync(t => t.Kind == TokenTransactionKind.Goodwill
                             && t.CreatedAt >= start && t.CreatedAt < end, cancellationToken);

        var smInvoices = await _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.VatDeclarationId == null
                        && i.Status == SelfBillingInvoiceStatus.Paid
                        && i.PaidAt != null
                        && i.PaidAt >= start && i.PaidAt < end)
            .ToListAsync(cancellationToken);

        var r1Ex = tokens.Sum(t => t.AmountExVatCents);
        var r1Vat = tokens.Sum(t => t.VatAmountCents);

        var r5Ex = 0;
        var r5Vat = 0;
        foreach (var inv in smInvoices)
        {
            r5Ex += TokenVatPricing.ToCents(inv.SubtotalExVat);
            if (inv.VatTreatment == SalesManagerVatTreatment.Standard21)
            {
                r5Vat += TokenVatPricing.ToCents(inv.VatAmount);
            }
        }

        return new VatDeclarationPreviewDto(
            year,
            quarter,
            label,
            r1Ex,
            r1Vat,
            tokens.Count,
            goodwillCount,
            r5Vat,
            r5Ex,
            smInvoices.Count,
            r1Vat - r5Vat,
            already);
    }

    public async Task<VatDeclaration> GenerateAndConfirmAsync(
        int year,
        int quarter,
        Guid? actorUserId = null,
        string? actorName = null,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, quarter);
        var label = PeriodLabel(year, quarter);
        var (start, end) = PeriodBounds(year, quarter);

        var existing = await _db.VatDeclarations
            .AnyAsync(d => d.Year == year && d.Quarter == quarter
                           && d.Status == VatDeclarationStatus.Confirmed, cancellationToken);
        if (existing)
        {
            throw new InvalidOperationException(
                $"Er bestaat al een bevestigde BTW-aangifte voor {label}.");
        }

        var tokens = await _db.TokenPurchaseInvoices
            .Where(i => i.VatDeclarationId == null
                        && i.IssuedAt >= start && i.IssuedAt < end)
            .ToListAsync(cancellationToken);

        var smInvoices = await _db.SelfBillingInvoices
            .Where(i => i.VatDeclarationId == null
                        && i.Status == SelfBillingInvoiceStatus.Paid
                        && i.PaidAt != null
                        && i.PaidAt >= start && i.PaidAt < end)
            .ToListAsync(cancellationToken);

        var goodwillCount = await _db.TokenTransactions.AsNoTracking()
            .CountAsync(t => t.Kind == TokenTransactionKind.Goodwill
                             && t.CreatedAt >= start && t.CreatedAt < end, cancellationToken);

        if (tokens.Count == 0 && smInvoices.Count == 0)
        {
            throw new InvalidOperationException(
                $"Geen openstaande BTW-regels voor {label}.");
        }

        var platform = await _companySettings.GetAsync(cancellationToken);
        var r1Ex = tokens.Sum(t => t.AmountExVatCents);
        var r1Vat = tokens.Sum(t => t.VatAmountCents);
        var r5Ex = 0;
        var r5Vat = 0;
        foreach (var inv in smInvoices)
        {
            r5Ex += TokenVatPricing.ToCents(inv.SubtotalExVat);
            if (inv.VatTreatment == SalesManagerVatTreatment.Standard21)
            {
                r5Vat += TokenVatPricing.ToCents(inv.VatAmount);
            }
        }

        var declarationId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var statusLabel = $"Verwerkt in aangifte {label}";
        var declaration = new VatDeclaration
        {
            Id = declarationId,
            Year = year,
            Quarter = quarter,
            PeriodLabel = label,
            Status = VatDeclarationStatus.Confirmed,
            Rubriek1OmzetExVatCents = r1Ex,
            Rubriek1VatCents = r1Vat,
            TokenInvoiceCount = tokens.Count,
            GoodwillCount = goodwillCount,
            Rubriek5VoorbelastingCents = r5Vat,
            Rubriek5CostExVatCents = r5Ex,
            SalesManagerInvoiceCount = smInvoices.Count,
            AmountDueCents = r1Vat - r5Vat,
            GeneratedAt = now,
            GeneratedByUserId = actorUserId,
            GeneratedByName = actorName,
            PdfFileName = $"BTW-aangifte-{label}.pdf",
            PlatformCompanyName = platform.CompanyName,
            PlatformKvkNumber = platform.KvkNumber,
            PlatformVatNumber = platform.VatNumber,
            PlatformAddress = platform.FormatAddressBlock()
        };

        foreach (var inv in tokens)
        {
            inv.VatDeclarationId = declarationId;
            inv.VatDeclarationStatusLabel = statusLabel;
        }

        foreach (var inv in smInvoices)
        {
            inv.VatDeclarationId = declarationId;
            inv.VatDeclarationStatusLabel = statusLabel;
        }

        declaration.PdfBytes = RenderPdf(declaration, platform);
        _db.VatDeclarations.Add(declaration);
        await _db.SaveChangesAsync(cancellationToken);
        return declaration;
    }

    public async Task<IReadOnlyList<VatDeclarationListItemDto>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.VatDeclarations.AsNoTracking()
            .OrderByDescending(d => d.Year)
            .ThenByDescending(d => d.Quarter)
            .Select(d => new VatDeclarationListItemDto(
                d.Id,
                d.Year,
                d.Quarter,
                d.PeriodLabel,
                d.Status.ToString(),
                d.Rubriek1OmzetExVatCents,
                d.Rubriek1VatCents,
                d.Rubriek5VoorbelastingCents,
                d.AmountDueCents,
                d.TokenInvoiceCount,
                d.GoodwillCount,
                d.SalesManagerInvoiceCount,
                d.GeneratedAt,
                d.GeneratedByName,
                d.PlatformCompanyName,
                d.PdfBytes != null && d.PdfBytes.Length > 0))
            .ToListAsync(cancellationToken);
    }

    public Task<VatDeclaration?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.VatDeclarations.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<byte[]> GetPdfAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await _db.VatDeclarations.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new { d.PdfBytes, d.PdfFileName })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("BTW-aangifte niet gevonden.");

        if (row.PdfBytes is null || row.PdfBytes.Length == 0)
        {
            throw new InvalidOperationException("PDF ontbreekt voor deze aangifte.");
        }

        return row.PdfBytes;
    }

    public async Task<IReadOnlyList<SalesManagerCostFinanceRow>> GetSalesManagerCostsAsync(
        int? year = null,
        int? quarter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.SelfBillingInvoices.AsNoTracking()
            .Where(i => i.Status == SelfBillingInvoiceStatus.Paid || i.Status == SelfBillingInvoiceStatus.Issued);

        if (year is int y)
        {
            query = query.Where(i => (i.PaidAt ?? i.IssuedAt ?? i.CreatedAt).Year == y);
            if (quarter is int q && q is >= 1 and <= 4)
            {
                var startMonth = (q - 1) * 3 + 1;
                var endMonth = startMonth + 2;
                query = query.Where(i =>
                    (i.PaidAt ?? i.IssuedAt ?? i.CreatedAt).Month >= startMonth
                    && (i.PaidAt ?? i.IssuedAt ?? i.CreatedAt).Month <= endMonth);
            }
        }

        return await query
            .OrderByDescending(i => i.PaidAt ?? i.IssuedAt ?? i.CreatedAt)
            .Select(i => new SalesManagerCostFinanceRow(
                i.Id,
                i.InvoiceNumber,
                i.SalesManagerUserId,
                i.SalesManagerCompanyName,
                i.SubtotalExVat,
                i.VatAmount,
                i.TotalInclVat,
                i.VatTreatment.ToString(),
                i.Status.ToString(),
                i.PaidAt,
                i.VatDeclarationStatusLabel))
            .Take(2000)
            .ToListAsync(cancellationToken);
    }

    private byte[] RenderPdf(VatDeclaration d, PlatformCompanySnapshot platform)
    {
        var logo = _companySettings.GetBrandLogoPng();
        var watermark = _companySettings.GetBrandWatermarkPng();
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var dueLabel = d.AmountDueCents >= 0 ? "Te betalen aan Belastingdienst" : "Terug te ontvangen van Belastingdienst";
        var dueAbs = Math.Abs(d.AmountDueCents);

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
                    .Width(400)
                    .Height(400)
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
                    row.ConstantItem(160).AlignRight().AlignMiddle().Column(col =>
                    {
                        col.Item().AlignRight().Text("BTW-AANGIFTE").FontSize(8)
                            .FontColor(Colors.Grey.Medium);
                        col.Item().AlignRight().Text(d.PeriodLabel).FontSize(14).SemiBold();
                    });
                });

                page.Content().PaddingTop(18).Column(col =>
                {
                    col.Item().Text("Overzicht voor Belastingdienst Nederland").FontSize(12).SemiBold();
                    col.Item().PaddingTop(4).Text(
                        $"Periode: {d.PeriodLabel} · Gegenereerd: {d.GeneratedAt.ToLocalTime().ToString("dd-MM-yyyy HH:mm", culture)}"
                        + (string.IsNullOrWhiteSpace(d.GeneratedByName) ? "" : $" · Door: {d.GeneratedByName}"));

                    col.Item().PaddingTop(14).Text("Rubriek 1 — Prestaties binnenland (tokenverkopen)").SemiBold()
                        .FontColor(Color.FromHex("#0F766E"));
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });
                        AddRow(table, "Omzet excl. BTW", Euro(d.Rubriek1OmzetExVatCents, culture));
                        AddRow(table, "Verschuldigde BTW (21%)", Euro(d.Rubriek1VatCents, culture));
                        AddRow(table, "Aantal verkoopfacturen", d.TokenInvoiceCount.ToString(culture));
                        AddRow(table, "Goodwill-/compensatietokens (geen omzet)", d.GoodwillCount.ToString(culture));
                    });

                    col.Item().PaddingTop(14).Text("Rubriek 5 — Voorbelasting / inkoop (o.a. salesmanagers)").SemiBold()
                        .FontColor(Color.FromHex("#0F766E"));
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });
                        AddRow(table, "Kosten excl. BTW", Euro(d.Rubriek5CostExVatCents, culture));
                        AddRow(table, "Aftrekbare voorbelasting", Euro(d.Rubriek5VoorbelastingCents, culture));
                        AddRow(table, "Aantal salesmanager-uitbetalingen", d.SalesManagerInvoiceCount.ToString(culture));
                    });

                    col.Item().PaddingTop(16).BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(10)
                        .Row(r =>
                        {
                            r.RelativeItem().Text(dueLabel).FontSize(12).SemiBold();
                            r.ConstantItem(120).AlignRight()
                                .Text(Euro(dueAbs, culture)).FontSize(14).SemiBold()
                                .FontColor(Color.FromHex("#0F766E"));
                        });

                    col.Item().PaddingTop(18).Text(
                            "Dit overzicht is automatisch gegenereerd vanuit Lobsy-administratie. "
                            + "Controleer de bedragen vóór indiening bij de Belastingdienst.")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(8).Column(c =>
                {
                    c.Item().Text(d.PlatformCompanyName).FontSize(8).SemiBold();
                    if (!string.IsNullOrWhiteSpace(d.PlatformAddress))
                    {
                        c.Item().Text(d.PlatformAddress).FontSize(8);
                    }

                    var meta = string.Join(" · ", new[]
                    {
                        string.IsNullOrWhiteSpace(d.PlatformKvkNumber) ? null : $"KvK {d.PlatformKvkNumber}",
                        string.IsNullOrWhiteSpace(d.PlatformVatNumber) ? null : $"BTW {d.PlatformVatNumber}"
                    }.Where(s => s is not null));
                    if (!string.IsNullOrWhiteSpace(meta))
                    {
                        c.Item().Text(meta).FontSize(8);
                    }
                });
            });
        }).GeneratePdf();
    }

    private static void AddRow(TableDescriptor table, string label, string value)
    {
        table.Cell().PaddingVertical(3).Text(label);
        table.Cell().PaddingVertical(3).AlignRight().Text(value);
    }

    private static string Euro(int cents, CultureInfo culture) =>
        $"€ {TokenVatPricing.FromCents(cents).ToString("0.00", culture)}";

    private static string PeriodLabel(int year, int quarter) => $"{year}-Q{quarter}";

    private static (DateTime Start, DateTime End) PeriodBounds(int year, int quarter)
    {
        var startMonth = (quarter - 1) * 3 + 1;
        var start = new DateTime(year, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(3);
        return (start, end);
    }

    private static void ValidatePeriod(int year, int quarter)
    {
        if (year < 2020 || year > 2100 || quarter is < 1 or > 4)
        {
            throw new ArgumentException("Ongeldige aangifteperiode.");
        }
    }

    private static void EnsurePeriod(List<(int Year, int Quarter)> periods, int year, int quarter)
    {
        if (!periods.Any(p => p.Year == year && p.Quarter == quarter))
        {
            periods.Add((year, quarter));
        }
    }
}
