using System.Globalization;
using Jobsy.Core;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public sealed class PartnerFlyerPdfService : IPartnerFlyerPdfService
{
    private readonly ISalesCommercialService _sales;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly IPlatformFeatureService _features;

    private static readonly Color BrandNavy = Color.FromHex("#0f2d5c");
    private static readonly Color BrandDeep = Color.FromHex("#0a2044");
    private static readonly Color SoftBlue = Color.FromHex("#e8eef7");
    private static readonly Color SoftMint = Color.FromHex("#e8f5ef");
    private static readonly Color WarmSand = Color.FromHex("#f7f1e6");
    private static readonly Color AccentTeal = Color.FromHex("#1a7a6d");
    private static readonly Color AccentCoral = Color.FromHex("#c45c3e");
    private static readonly Color SoftSky = Color.FromHex("#dceef8");
    private static readonly Color Slate = Color.FromHex("#2c3a4a");

    static PartnerFlyerPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public PartnerFlyerPdfService(
        ISalesCommercialService sales,
        IPlatformCompanySettingsService companySettings,
        IPlatformFeatureService features)
    {
        _sales = sales;
        _companySettings = companySettings;
        _features = features;
    }

    public PartnerFlyerPdfService(
        ISalesCommercialService sales,
        IPlatformCompanySettingsService companySettings)
        : this(sales, companySettings, NullFeatures.Instance)
    {
    }

    public async Task<byte[]> RenderAsync(string? trackingCode, CancellationToken cancellationToken = default)
    {
        var catalog = await _sales.GetPublicCatalogAsync(cancellationToken);
        var platform = await _companySettings.GetAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        var logo = _companySettings.GetBrandLogoPng();
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var code = NormalizeTrackingCode(trackingCode);
        var brand = string.IsNullOrWhiteSpace(platform.CompanyName) ? "Lobsy" : platform.CompanyName.Trim();
        var baseUrl = JobsyPublicUrl.NormalizeOrigin(features.PublicWebBaseUrl).TrimEnd('/');
        var qrTarget = code is null
            ? $"{baseUrl}/register"
            : $"{baseUrl}/register?ref={Uri.EscapeDataString(code)}";
        var qrPng = RenderQrPng(qrTarget);
        var packages = catalog.Packages.Take(4).ToList();
        var costs = catalog.VacancyTypeCosts.Take(3).ToList();
        var bonus = catalog.StartHighlightBonusTokens.ToString("0.##", culture);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(BrandDeep));

                page.Content().Column(root =>
                {
                    root.Item().Background(BrandNavy).Padding(24).Column(hero =>
                    {
                        hero.Spacing(5);
                        hero.Item().Row(row =>
                        {
                            if (logo is { Length: > 0 })
                            {
                                row.ConstantItem(44).Height(30).Image(logo).FitArea();
                                row.ConstantItem(8);
                            }

                            row.RelativeItem().AlignMiddle().Column(title =>
                            {
                                title.Item().Text(brand).FontSize(20).Bold().FontColor(Colors.White);
                                title.Item().Text("Sales-toolkit · uitnodiging voor werkgevers")
                                    .FontSize(9).FontColor(SoftSky);
                            });
                            row.ConstantItem(90).AlignMiddle().AlignRight()
                                .Background(AccentCoral).PaddingHorizontal(8).PaddingVertical(5)
                                .Text("1 blad A4").FontSize(8).Bold().FontColor(Colors.White);
                        });

                        hero.Item().PaddingTop(8)
                            .Text("Nodig werkgevers uit voor hyper-lokaal werven")
                            .FontSize(20).Bold().FontColor(Colors.White);
                        hero.Item().Text(
                                "Reistijd-matching in Westland & Den Haag — bereik kandidaten die écht in de buurt " +
                                "wonen of studeren, zonder abonnement.")
                            .FontSize(9).FontColor(SoftSky);
                    });

                    root.Item().Height(5).Background(AccentTeal);

                    root.Item().Padding(22).Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Background(SoftMint).Padding(9).Column(usp =>
                        {
                            usp.Spacing(2);
                            usp.Item().Text("Waarom Lobsy?").Bold().FontColor(AccentTeal).FontSize(10);
                            usp.Item().Text("• Match op fiets, OV of auto — geen landelijke spill").FontSize(8);
                            usp.Item().Text("• Banenkaart + carrousel-highlight (Funda-model)").FontSize(8);
                            usp.Item().Text("• Tokens i.p.v. abonnementen — betaal alleen voor plaatsing").FontSize(8);
                            usp.Item().Text($"• Start-highlight t.w.v. {bonus} tokens bij aanmelding via salescode")
                                .FontSize(8);
                        });

                        if (costs.Count > 0)
                        {
                            col.Item().Text("Indicatie per vacaturetype").FontSize(11).Bold().FontColor(BrandNavy);
                            col.Item().Row(row =>
                            {
                                row.Spacing(6);
                                foreach (var cost in costs)
                                {
                                    row.RelativeItem().Background(SoftBlue).Padding(7).Column(card =>
                                    {
                                        card.Item().Text(cost.Label).Bold().FontSize(8).FontColor(BrandNavy);
                                        card.Item().Text(
                                                $"{cost.CostTokens.ToString("0.##", culture)} tokens · {cost.PriceEuro.ToString("C", culture)}")
                                            .FontSize(7).FontColor(Slate);
                                    });
                                }
                            });
                        }

                        if (packages.Count > 0)
                        {
                            col.Item().Text("Populaire pakketten").FontSize(11).Bold().FontColor(BrandNavy);
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
                                    h.Cell().Background(BrandNavy).Padding(4).Text("Pakket").FontColor(Colors.White).Bold().FontSize(8);
                                    h.Cell().Background(BrandNavy).Padding(4).Text("Categorie").FontColor(Colors.White).Bold().FontSize(8);
                                    h.Cell().Background(BrandNavy).Padding(4).Text("Tokens").FontColor(Colors.White).Bold().FontSize(8);
                                    h.Cell().Background(BrandNavy).Padding(4).Text("Prijs").FontColor(Colors.White).Bold().FontSize(8);
                                });

                                foreach (var pack in packages)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(4).Text(pack.Name).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(4)
                                        .Text(CategoryLabel(pack.Category)).FontSize(7);
                                    table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(4)
                                        .Text(pack.TokenAmount.ToString(culture)).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(4)
                                        .Text(pack.PriceEuro.ToString("C", culture)).FontSize(8);
                                }
                            });
                        }

                        col.Item().Background(WarmSand).Padding(10).Row(cta =>
                        {
                            cta.RelativeItem().PaddingRight(10).Column(info =>
                            {
                                info.Spacing(3);
                                info.Item().Text("Aan de slag met jouw salescode").Bold().FontColor(AccentCoral);
                                info.Item().Text(
                                        "Scan de QR bij het gesprek — registratie opent met jouw code vooraf ingevuld. " +
                                        "Handig voor salesmanagers én managers die meewerken.")
                                    .FontSize(8);
                                if (code is not null)
                                {
                                    info.Item().Text(code).FontSize(20).Bold().FontColor(BrandNavy);
                                }
                                else
                                {
                                    info.Item().Text("Vraag je salesmanager om een code.")
                                        .FontSize(9).FontColor(Slate);
                                }
                            });
                            cta.ConstantItem(118).Background(Colors.White).Border(2).BorderColor(AccentTeal)
                                .Padding(7).Column(qr =>
                                {
                                    qr.Item().Height(100).Image(qrPng).FitArea();
                                    qr.Item().PaddingTop(3).AlignCenter()
                                        .Text("Scan & registreer")
                                        .FontSize(8).Bold().FontColor(AccentTeal);
                                });
                        });
                    });

                    root.Item().ExtendVertical().AlignBottom().Background(BrandDeep)
                        .PaddingVertical(9).AlignCenter().Text(text =>
                        {
                            text.Span($"{brand} · partnerflyer")
                                .FontSize(8).FontColor(Colors.White);
                            if (code is not null)
                            {
                                text.Span($" · {code}").FontSize(8).FontColor(Colors.White);
                            }

                            text.Span(" · ").FontSize(8).FontColor(Colors.White);
                            text.Span(platform.Slogan ?? "Dichtbij genoeg om het pantser te laten vallen")
                                .FontSize(8).FontColor(SoftSky);
                        });
                });
            });
        }).GeneratePdf();
    }

    private static string CategoryLabel(string category) => category switch
    {
        nameof(Core.Enums.SalesPackageCategory.FirstYearSupplier) => "First Year",
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
            @"^(SM|BM|IM)-[A-Z0-9]{6}$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant)
            ? normalized
            : null;
    }

    private static byte[] RenderQrPng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(8);
    }

    private sealed class NullFeatures : IPlatformFeatureService
    {
        public static readonly NullFeatures Instance = new();

        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(false, false, false, "https://lobsy.nl", null));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
