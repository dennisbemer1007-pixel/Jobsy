using System.Globalization;
using System.Text.RegularExpressions;
using Jobsy.Core;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public sealed class AmbassadeurFlyerPdfService : IAmbassadeurFlyerPdfService
{
    private static readonly Regex CodeRegex = new(
        @"^AM-[A-Z0-9]{6,12}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Color BrandNavy = Color.FromHex("#0f2d5c");
    private static readonly Color BrandDeep = Color.FromHex("#0a2044");
    private static readonly Color SoftSky = Color.FromHex("#dceef8");
    private static readonly Color SoftMint = Color.FromHex("#e8f5ef");
    private static readonly Color WarmSand = Color.FromHex("#f7f1e6");
    private static readonly Color AccentTeal = Color.FromHex("#1a7a6d");
    private static readonly Color AccentCoral = Color.FromHex("#c45c3e");
    private static readonly Color SoftCoral = Color.FromHex("#f8e8e2");
    private static readonly Color SoftGold = Color.FromHex("#f3e6c8");
    private static readonly Color Slate = Color.FromHex("#2c3a4a");

    private readonly JobsyDbContext _db;
    private readonly ISalesCommercialService _sales;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly IPlatformFeatureService _features;

    static AmbassadeurFlyerPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public AmbassadeurFlyerPdfService(
        JobsyDbContext db,
        ISalesCommercialService sales,
        IPlatformCompanySettingsService companySettings,
        IPlatformFeatureService features)
    {
        _db = db;
        _sales = sales;
        _companySettings = companySettings;
        _features = features;
    }

    public async Task<byte[]> RenderAsync(
        string trackingCode,
        AmbassadeurFlyerKind kind,
        CancellationToken cancellationToken = default)
    {
        var code = NormalizeTrackingCode(trackingCode)
            ?? throw new ArgumentException("Ongeldige Ambassadeur-trackingcode.");

        var exists = await _db.AmbassadeurProfiles.AsNoTracking()
            .AnyAsync(
                p => p.TrackingCode != null
                     && p.TrackingCode.ToUpper() == code
                     && p.OnboardingCompletedAt != null,
                cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException("Trackingcode is onbekend of onboarding is niet afgerond.");
        }

        var platform = await _companySettings.GetAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        var logo = _companySettings.GetBrandLogoPng();
        var brand = string.IsNullOrWhiteSpace(platform.CompanyName) ? "Lobsy" : platform.CompanyName.Trim();
        var baseUrl = JobsyPublicUrl.NormalizeOrigin(features.PublicWebBaseUrl).TrimEnd('/');

        return kind == AmbassadeurFlyerKind.Candidate
            ? RenderCandidateFlyer(brand, logo, code, baseUrl)
            : await RenderEntrepreneurFlyerAsync(brand, logo, code, baseUrl, cancellationToken);
    }

    private static byte[] RenderCandidateFlyer(string brand, byte[]? logo, string code, string baseUrl)
    {
        var qrPng = RenderQrPng($"{baseUrl}/werven/{Uri.EscapeDataString(code)}");
        var culture = CultureInfo.GetCultureInfo("nl-NL");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Slate));

                page.Content().Column(root =>
                {
                    root.Item().Background(SoftSky).Padding(28).Column(hero =>
                    {
                        hero.Spacing(6);
                        hero.Item().Row(row =>
                        {
                            if (logo is { Length: > 0 })
                            {
                                row.ConstantItem(48).Height(32).Image(logo).FitArea();
                                row.ConstantItem(10);
                            }

                            row.RelativeItem().AlignMiddle().Text(brand)
                                .FontSize(24).Bold().FontColor(BrandNavy);
                            row.ConstantItem(108).AlignMiddle().AlignRight()
                                .Background(Colors.White).PaddingHorizontal(9).PaddingVertical(5)
                                .Text("Uitnodiging").FontSize(9).Bold().FontColor(AccentTeal);
                        });

                        hero.Item().PaddingTop(10)
                            .Text("Jij bent uitgenodigd").FontSize(13).FontColor(AccentCoral);
                        hero.Item().Text("Werk dichterbij dan je denkt")
                            .FontSize(28).Bold().FontColor(BrandNavy);
                        hero.Item().Text(
                                "Lokale banen in Westland & Den Haag — gematcht op jouw maximale reistijd. " +
                                "Geen eindeloos scrollen, wel snel starten.")
                            .FontSize(11).FontColor(BrandDeep);
                    });

                    root.Item().Height(5).Background(AccentTeal);

                    root.Item().Padding(28).Column(body =>
                    {
                        body.Spacing(12);
                        body.Item().Row(row =>
                        {
                            row.RelativeItem().PaddingRight(12).Column(copy =>
                            {
                                copy.Spacing(6);
                                copy.Item().Text("Wat krijg je?").FontSize(15).Bold().FontColor(BrandNavy);
                                copy.Item().Background(SoftMint).Padding(10).Column(bullets =>
                                {
                                    bullets.Spacing(3);
                                    bullets.Item().Text("• Vacatures binnen jouw reistijd (fiets, OV of auto)");
                                    bullets.Item().Text("• Banenkaart met clusters dichtbij huis of school");
                                    bullets.Item().Text("• Snel solliciteren — zonder ellenlange formulieren");
                                    bullets.Item().Text("• Overzicht van je sollicitaties op één plek");
                                });

                                copy.Item().Background(WarmSand).Padding(12).Column(box =>
                                {
                                    box.Spacing(3);
                                    box.Item().Text("Ambassadeur-code").FontSize(9).FontColor(AccentCoral);
                                    box.Item().Text(code).FontSize(22).Bold().FontColor(BrandNavy);
                                    box.Item().Text("Scan de QR — jouw start staat klaar.").FontSize(9);
                                });
                            });

                            row.ConstantItem(150).Background(SoftGold).Border(2).BorderColor(AccentTeal)
                                .Padding(10).Column(qr =>
                                {
                                    qr.Item().AlignCenter().Text("Start hier")
                                        .FontSize(10).Bold().FontColor(AccentTeal);
                                    qr.Item().PaddingTop(6).Height(124).Image(qrPng).FitArea();
                                    qr.Item().PaddingTop(6).AlignCenter()
                                        .Text("Scan & start gratis")
                                        .FontSize(9).Bold().FontColor(BrandNavy);
                                });
                        });

                        body.Item().Background(SoftCoral).Padding(11).Text(t =>
                        {
                            t.Span("Tip voor ambassadeurs: ").Bold().FontColor(AccentCoral);
                            t.Span(
                                "print op A4, hang op school/kantine of deel de QR in de buurt. " +
                                "Eén blad — klaar om te gebruiken.");
                        });
                    });

                    root.Item().ExtendVertical().AlignBottom().Background(BrandNavy)
                        .PaddingVertical(10).AlignCenter().Text(txt =>
                        {
                            txt.Span($"{brand} · kandidatenflyer · {code} · ")
                                .FontSize(8).FontColor(Colors.White);
                            txt.Span(DateTime.UtcNow.ToString("d", culture))
                                .FontSize(8).FontColor(Colors.White);
                        });
                });
            });
        }).GeneratePdf();
    }

    private async Task<byte[]> RenderEntrepreneurFlyerAsync(
        string brand,
        byte[]? logo,
        string code,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var catalog = await _sales.GetPublicCatalogAsync(cancellationToken);
        var qrPng = RenderQrPng($"{baseUrl}/register?ref={Uri.EscapeDataString(code)}");
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var packages = catalog.Packages.Where(p => p.IsActive).OrderBy(p => p.SortOrder).Take(4).ToList();
        var costs = catalog.VacancyTypeCosts.Where(c => c.IsActive).Take(3).ToList();
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
                    root.Item().Background(BrandDeep).Padding(24).Column(hero =>
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
                                title.Item().Text("Uitnodiging voor ondernemers · Westland & Den Haag")
                                    .FontSize(9).FontColor(SoftSky);
                            });
                            row.ConstantItem(90).AlignMiddle().AlignRight()
                                .Background(AccentCoral).PaddingHorizontal(8).PaddingVertical(5)
                                .Text("1 blad A4").FontSize(8).Bold().FontColor(Colors.White);
                        });

                        hero.Item().PaddingTop(8)
                            .Text("Werf hyper-lokaal — zonder abonnement")
                            .FontSize(22).Bold().FontColor(Colors.White);
                        hero.Item().Text(
                                $"Kandidaten op reistijd, clusterpopups op de banenkaart en gratis start-highlight " +
                                $"t.w.v. {bonus} tokens via Ambassadeur-code.")
                            .FontSize(9).FontColor(SoftSky);
                    });

                    root.Item().Height(5).Background(AccentCoral);

                    root.Item().Padding(22).Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Row(usps =>
                        {
                            usps.Spacing(6);
                            usps.RelativeItem().Background(SoftMint).Padding(8).Column(c =>
                            {
                                c.Item().Text("Reistijd-match").Bold().FontSize(9).FontColor(AccentTeal);
                                c.Item().Text("fiets / OV / auto").FontSize(8);
                            });
                            usps.RelativeItem().Background(SoftSky).Padding(8).Column(c =>
                            {
                                c.Item().Text("Zichtbaar in de buurt").Bold().FontSize(9).FontColor(BrandNavy);
                                c.Item().Text("clusterpopups").FontSize(8);
                            });
                            usps.RelativeItem().Background(WarmSand).Padding(8).Column(c =>
                            {
                                c.Item().Text("Betaal per plaatsing").Bold().FontSize(9).FontColor(AccentCoral);
                                c.Item().Text("tokens i.p.v. abonnement").FontSize(8);
                            });
                        });

                        if (packages.Count > 0)
                        {
                            col.Item().Text("Populaire startpakketten").FontSize(11).Bold().FontColor(BrandNavy);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2.2f);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Background(BrandNavy).Padding(4).Text("Pakket").FontColor(Colors.White).Bold().FontSize(8);
                                    h.Cell().Background(BrandNavy).Padding(4).Text("Tokens").FontColor(Colors.White).Bold().FontSize(8);
                                    h.Cell().Background(BrandNavy).Padding(4).Text("Prijs").FontColor(Colors.White).Bold().FontSize(8);
                                });
                                foreach (var pkg in packages)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor(SoftSky).Padding(4).Text(pkg.Name).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(SoftSky).Padding(4)
                                        .Text(pkg.TokenAmount.ToString(culture)).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(SoftSky).Padding(4)
                                        .Text(pkg.PriceEuro.ToString("C", culture)).FontSize(8);
                                }
                            });
                        }

                        if (costs.Count > 0)
                        {
                            col.Item().Text("Indicatie per vacaturetype").FontSize(11).Bold().FontColor(BrandNavy);
                            col.Item().Row(row =>
                            {
                                row.Spacing(6);
                                foreach (var cost in costs)
                                {
                                    row.RelativeItem().Background(SoftSky).Padding(7).Column(card =>
                                    {
                                        card.Item().Text(cost.Label).Bold().FontSize(8).FontColor(BrandNavy);
                                        card.Item().Text(
                                                $"{cost.CostTokens.ToString("0.##", culture)} tokens · {cost.PriceEuro.ToString("C", culture)}")
                                            .FontSize(7).FontColor(Slate);
                                    });
                                }
                            });
                        }

                        col.Item().Background(WarmSand).Padding(10).Row(row =>
                        {
                            row.RelativeItem().PaddingRight(10).Column(info =>
                            {
                                info.Spacing(3);
                                info.Item().Text("Jouw gratis start-highlight").Bold().FontColor(AccentCoral);
                                info.Item().Text(
                                        "Scan de QR — registratie opent met Ambassadeur-code al ingevuld.")
                                    .FontSize(8);
                                info.Item().Text(code).FontSize(20).Bold().FontColor(BrandNavy);
                                info.Item().Text("Ambassadeur-tool: print, deel of laat scannen bij het gesprek.")
                                    .FontSize(8).FontColor(Slate);
                            });
                            row.ConstantItem(118).Background(Colors.White).Border(2).BorderColor(AccentTeal)
                                .Padding(7).Column(qr =>
                                {
                                    qr.Item().Height(100).Image(qrPng).FitArea();
                                    qr.Item().PaddingTop(3).AlignCenter()
                                        .Text("Scan & registreer")
                                        .FontSize(8).Bold().FontColor(AccentTeal);
                                });
                        });
                    });

                    root.Item().ExtendVertical().AlignBottom().Background(BrandNavy)
                        .PaddingVertical(9).AlignCenter().Text(txt =>
                        {
                            txt.Span($"{brand} · ondernemersflyer · {code} · ")
                                .FontSize(8).FontColor(Colors.White);
                            txt.Span(DateTime.UtcNow.ToString("d", culture))
                                .FontSize(8).FontColor(Colors.White);
                        });
                });
            });
        }).GeneratePdf();
    }

    private static string? NormalizeTrackingCode(string? trackingCode)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return null;
        }

        var code = trackingCode.Trim().ToUpperInvariant();
        return CodeRegex.IsMatch(code) ? code : null;
    }

    private static byte[] RenderQrPng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(8);
    }
}
