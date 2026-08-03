using System.Globalization;
using System.Text.RegularExpressions;
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
        var baseUrl = features.PublicWebBaseUrl.TrimEnd('/');

        return kind == AmbassadeurFlyerKind.Candidate
            ? RenderCandidateFlyer(brand, logo, code, baseUrl)
            : await RenderEntrepreneurFlyerAsync(brand, logo, code, baseUrl, cancellationToken);
    }

    private static byte[] RenderCandidateFlyer(string brand, byte[]? logo, string code, string baseUrl)
    {
        var targetUrl = $"{baseUrl}/werven/{Uri.EscapeDataString(code)}";
        var qrPng = RenderQrPng(targetUrl);
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
                    // Inviting full-bleed hero band
                    root.Item().Background(SoftSky).Padding(36).Column(hero =>
                    {
                        hero.Spacing(10);
                        hero.Item().Row(row =>
                        {
                            if (logo is { Length: > 0 })
                            {
                                row.ConstantItem(52).Height(36).Image(logo).FitArea();
                                row.ConstantItem(12);
                            }

                            row.RelativeItem().AlignMiddle().Text(brand)
                                .FontSize(26).Bold().FontColor(BrandNavy);
                        });

                        hero.Item().PaddingTop(18)
                            .Text("Werk dichterbij dan je denkt")
                            .FontSize(28).Bold().FontColor(BrandNavy);

                        hero.Item().Text(
                                "Lokale banen in Westland & Den Haag — gematcht op jouw maximale reistijd. " +
                                "Geen eindeloos scrollen door landelijke vacatures.")
                            .FontSize(12).FontColor(BrandDeep);

                        hero.Item().PaddingTop(8).Background(Colors.White).Padding(10).Text(t =>
                        {
                            t.Span("Scan & start ").Bold().FontColor(AccentTeal);
                            t.Span("— gratis registreren als kandidaat").FontColor(Slate);
                        });
                    });

                    root.Item().Padding(36).Column(body =>
                    {
                        body.Spacing(16);

                        body.Item().Row(row =>
                        {
                            row.RelativeItem().PaddingRight(16).Column(copy =>
                            {
                                copy.Spacing(8);
                                copy.Item().Text("Waarom Lobsy?").FontSize(16).Bold().FontColor(BrandNavy);
                                copy.Item().Text("✓ Alleen vacatures binnen jouw reistijd (fiets, OV of auto)");
                                copy.Item().Text("✓ Banenkaart met clusters dichtbij huis of school");
                                copy.Item().Text("✓ Snel solliciteren — zonder ellenlange formulieren");
                                copy.Item().Text("✓ Overzicht van je sollicitaties op één plek");

                                copy.Item().PaddingTop(10).Background(WarmSand).Padding(12).Column(box =>
                                {
                                    box.Spacing(4);
                                    box.Item().Text("Jouw trackingcode").FontSize(9).FontColor(AccentCoral);
                                    box.Item().Text(code).FontSize(22).Bold().FontColor(BrandNavy);
                                    box.Item().Text($"{baseUrl}/werven/{code}").FontSize(8).FontColor(Colors.Grey.Darken2);
                                });
                            });

                            row.ConstantItem(150).Column(qr =>
                            {
                                qr.Item().Border(1).BorderColor(SoftSky).Padding(8).Column(inner =>
                                {
                                    inner.Item().Height(134).Image(qrPng).FitArea();
                                    inner.Item().PaddingTop(6).AlignCenter()
                                        .Text("Scan om te registreren")
                                        .FontSize(8).FontColor(AccentTeal);
                                });
                            });
                        });

                        body.Item().Background(SoftMint).Padding(12).Text(
                            "Tip: bewaar deze flyer of deel de QR met vrienden in de buurt die ook lokaal willen werken.");
                    });

                    root.Item().Background(BrandNavy).Padding(12).AlignCenter().Text(txt =>
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
        var targetUrl = $"{baseUrl}/register?ref={Uri.EscapeDataString(code)}";
        var qrPng = RenderQrPng(targetUrl);
        var culture = CultureInfo.GetCultureInfo("nl-NL");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(BrandDeep));

                page.Header().Column(col =>
                {
                    col.Item().Background(BrandDeep).Padding(16).Row(row =>
                    {
                        if (logo is { Length: > 0 })
                        {
                            row.ConstantItem(56).Height(40).Image(logo).FitArea();
                            row.ConstantItem(12);
                        }

                        row.RelativeItem().Column(title =>
                        {
                            title.Item().Text(brand).FontSize(20).Bold().FontColor(Colors.White);
                            title.Item().Text("Zakelijk partneraanbod · Westland & Den Haag")
                                .FontSize(10).FontColor(Colors.White);
                        });
                    });
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Text("Hyper-lokaal werven zonder abonnement")
                        .FontSize(18).Bold().FontColor(BrandNavy);
                    col.Item().Text(
                        "Lobsy matcht kandidaten op reistijd — niet op landelijke bereikcijfers. " +
                        "Je betaalt per plaatsing met tokens, krijgt clusterpopups op de banenkaart en " +
                        "een gratis start-highlight via Ambassadeur-code.");

                    col.Item().Background(SoftMint).Padding(12).Column(usp =>
                    {
                        usp.Spacing(4);
                        usp.Item().Text("USP’s t.o.v. traditionele platforms").Bold().FontColor(AccentTeal);
                        usp.Item().Text("✓ Reistijd-matching (fiets / OV / auto) — minder spill");
                        usp.Item().Text("✓ Clusterpopups op de banenkaart voor zichtbaarheid in de buurt");
                        usp.Item().Text("✓ Tokens i.p.v. vaste abonnementen");
                        usp.Item().Text(
                            $"✓ Gratis start-highlight t.w.v. {catalog.StartHighlightBonusTokens.ToString("0.##", culture)} tokens via Ambassadeur-code");
                    });

                    col.Item().Text("Pakketten & tarieven").FontSize(14).Bold().FontColor(BrandNavy);

                    if (catalog.Packages.Count > 0)
                    {
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
                                h.Cell().Background(BrandNavy).Padding(6).Text("Pakket").FontColor(Colors.White).Bold();
                                h.Cell().Background(BrandNavy).Padding(6).Text("Tokens").FontColor(Colors.White).Bold();
                                h.Cell().Background(BrandNavy).Padding(6).Text("Prijs").FontColor(Colors.White).Bold();
                            });
                            foreach (var pkg in catalog.Packages.Where(p => p.IsActive).OrderBy(p => p.SortOrder).Take(6))
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(SoftSky).Padding(6).Text(pkg.Name);
                                table.Cell().BorderBottom(0.5f).BorderColor(SoftSky).Padding(6)
                                    .Text(pkg.TokenAmount.ToString(culture));
                                table.Cell().BorderBottom(0.5f).BorderColor(SoftSky).Padding(6)
                                    .Text(pkg.PriceEuro.ToString("C", culture));
                            }
                        });
                    }

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
                            h.Cell().Background(AccentTeal).Padding(6).Text("Vacaturetype").FontColor(Colors.White).Bold();
                            h.Cell().Background(AccentTeal).Padding(6).Text("Tokens").FontColor(Colors.White).Bold();
                            h.Cell().Background(AccentTeal).Padding(6).Text("Indicatie €").FontColor(Colors.White).Bold();
                        });
                        foreach (var cost in catalog.VacancyTypeCosts.Where(c => c.IsActive).Take(8))
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(SoftSky).Padding(6).Text(cost.Label);
                            table.Cell().BorderBottom(0.5f).BorderColor(SoftSky).Padding(6)
                                .Text(cost.CostTokens.ToString("0.##", culture));
                            table.Cell().BorderBottom(0.5f).BorderColor(SoftSky).Padding(6)
                                .Text(cost.PriceEuro.ToString("C", culture));
                        }
                    });

                    col.Item().Background(WarmSand).Padding(14).Row(row =>
                    {
                        row.RelativeItem().Column(info =>
                        {
                            info.Spacing(5);
                            info.Item().Text("Gratis start-highlight").Bold().FontColor(AccentCoral);
                            info.Item().Text(
                                "Scan de QR of open het registratieformulier — de Ambassadeur-trackingcode is al vooraf ingevuld.");
                            info.Item().Text(code).FontSize(20).Bold().FontColor(BrandNavy);
                            info.Item().Text($"{baseUrl}/register?ref={code}").FontSize(8);
                        });
                        row.ConstantItem(130).Height(130).Image(qrPng).FitArea();
                    });
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span($"{brand} · ondernemersflyer · {code} · ")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    txt.Span(DateTime.UtcNow.ToString("d", culture))
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
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
