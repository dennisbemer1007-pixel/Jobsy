using System.Globalization;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public sealed class PartnerFlyerPdfService : IPartnerFlyerPdfService
{
    private readonly ISalesCommercialService _sales;
    private readonly IPlatformCompanySettingsService _companySettings;

    private static readonly Color BrandNavy = Color.FromHex("#0f2d5c");
    private static readonly Color BrandDeep = Color.FromHex("#0a2044");
    private static readonly Color SoftBlue = Color.FromHex("#e8eef7");

    static PartnerFlyerPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public PartnerFlyerPdfService(
        ISalesCommercialService sales,
        IPlatformCompanySettingsService companySettings)
    {
        _sales = sales;
        _companySettings = companySettings;
    }

    public async Task<byte[]> RenderAsync(string? trackingCode, CancellationToken cancellationToken = default)
    {
        var catalog = await _sales.GetPublicCatalogAsync(cancellationToken);
        var platform = await _companySettings.GetAsync(cancellationToken);
        var logo = _companySettings.GetBrandLogoPng();
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var code = NormalizeTrackingCode(trackingCode);
        var brand = string.IsNullOrWhiteSpace(platform.CompanyName) ? "Lobsy" : platform.CompanyName.Trim();
        var registerHint = code is null
            ? "Registreer via lobsy.nl/register"
            : $"Registreer via lobsy.nl/register?ref={code}";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(BrandDeep));

                page.Header().Column(col =>
                {
                    col.Item().Background(BrandNavy).Padding(16).Row(row =>
                    {
                        if (logo is { Length: > 0 })
                        {
                            row.ConstantItem(56).Height(40).Image(logo).FitArea();
                            row.ConstantItem(12);
                        }

                        row.RelativeItem().Column(title =>
                        {
                            title.Item().Text(brand).FontSize(22).Bold().FontColor(Colors.White);
                            title.Item().Text("Hyper-lokaal werven op reistijd · Westland & Den Haag")
                                .FontSize(10).FontColor(Colors.White);
                        });
                    });
                });

                page.Content().PaddingVertical(18).Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Text("Waarom Lobsy?").FontSize(16).Bold().FontColor(BrandNavy);
                    col.Item().Text(
                        "Reistijd-matching in plaats van loze kliks. Je bereikt kandidaten die écht in de buurt wonen of studeren — zonder verspild mediabudget.");

                    col.Item().Background(SoftBlue).Padding(12).Column(usp =>
                    {
                        usp.Spacing(4);
                        usp.Item().Text("✓ Match op fiets, OV of auto — geen landelijke spill").Bold();
                        usp.Item().Text("✓ Funda-model: banenkaart + carrousel-highlight");
                        usp.Item().Text("✓ Tokens i.p.v. abonnementen — betaal alleen voor plaatsing");
                        usp.Item().Text(
                            $"✓ Start-highlight t.w.v. {catalog.StartHighlightBonusTokens.ToString("0.##", culture)} tokens bij aanmelding via salescode");
                    });

                    col.Item().Text("Actuele tarieven per vacaturetype").FontSize(14).Bold().FontColor(BrandNavy);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(BrandNavy).Padding(6).Text("Type").FontColor(Colors.White).Bold();
                            h.Cell().Background(BrandNavy).Padding(6).Text("Tokens").FontColor(Colors.White).Bold();
                            h.Cell().Background(BrandNavy).Padding(6).Text("Indicatie €").FontColor(Colors.White).Bold();
                        });

                        foreach (var cost in catalog.VacancyTypeCosts)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(6).Text(cost.Label);
                            table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(6)
                                .Text(cost.CostTokens.ToString("0.##", culture));
                            table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(6)
                                .Text(cost.PriceEuro.ToString("C", culture));
                        }
                    });

                    col.Item().Text(
                            $"Basis tokenwaarde: {catalog.BaseTokenValueEuro.ToString("C", culture)} · " +
                            $"Highlight carrousel ({catalog.HighlightCarouselDays} dagen): {catalog.HighlightCarouselTokens.ToString("0.##", culture)} tokens · " +
                            $"Pulse-marker: {catalog.HighlightPulseTokens.ToString("0.##", culture)} tokens")
                        .FontSize(9).FontColor(Colors.Grey.Darken2);

                    if (catalog.Packages.Count > 0)
                    {
                        col.Item().Text("Pakketten").FontSize(14).Bold().FontColor(BrandNavy);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background(BrandNavy).Padding(6).Text("Pakket").FontColor(Colors.White).Bold();
                                h.Cell().Background(BrandNavy).Padding(6).Text("Categorie").FontColor(Colors.White).Bold();
                                h.Cell().Background(BrandNavy).Padding(6).Text("Tokens").FontColor(Colors.White).Bold();
                                h.Cell().Background(BrandNavy).Padding(6).Text("Prijs").FontColor(Colors.White).Bold();
                            });

                            foreach (var pack in catalog.Packages)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(6).Text(pack.Name);
                                table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(6)
                                    .Text(CategoryLabel(pack.Category));
                                table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(6)
                                    .Text(pack.TokenAmount.ToString(culture));
                                table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(6)
                                    .Text(pack.PriceEuro.ToString("C", culture));
                            }
                        });
                    }

                    col.Item().Background(BrandNavy).Padding(14).Column(cta =>
                    {
                        cta.Spacing(4);
                        cta.Item().Text("Aan de slag").FontSize(13).Bold().FontColor(Colors.White);
                        cta.Item().Text(registerHint).FontColor(Colors.White);
                        if (code is not null)
                        {
                            cta.Item().Text($"Jouw salescode: {code}").FontSize(14).Bold().FontColor(Colors.White);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span($"{brand} · Westland & Den Haag · ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span(platform.Slogan ?? "Dichtbij genoeg om het pantser te laten vallen")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    private static string CategoryLabel(string category) => category switch
    {
        nameof(Core.Enums.SalesPackageCategory.FirstYearSupplier) => "First Year Supplier",
        nameof(Core.Enums.SalesPackageCategory.Enterprise) => "Enterprise",
        _ => "Standaard"
    };

    private static string? NormalizeTrackingCode(string? trackingCode)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return null;
        }

        var normalized = trackingCode.Trim().ToUpperInvariant();
        return System.Text.RegularExpressions.Regex.IsMatch(
            normalized,
            @"^SM-[A-Z0-9]{6}$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant)
            ? normalized
            : null;
    }
}
