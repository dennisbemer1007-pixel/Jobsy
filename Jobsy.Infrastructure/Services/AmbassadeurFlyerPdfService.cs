using System.Globalization;
using System.Text.RegularExpressions;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
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
    private static readonly Color SoftBlue = Color.FromHex("#e8eef7");
    private static readonly Color SoftGreen = Color.FromHex("#e6f4ea");

    private readonly JobsyDbContext _db;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly IPlatformFeatureService _features;

    static AmbassadeurFlyerPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public AmbassadeurFlyerPdfService(
        JobsyDbContext db,
        IPlatformCompanySettingsService companySettings,
        IPlatformFeatureService features)
    {
        _db = db;
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

        var targetUrl = kind == AmbassadeurFlyerKind.Candidate
            ? $"{baseUrl}/werven/{Uri.EscapeDataString(code)}"
            : $"{baseUrl}/register?ref={Uri.EscapeDataString(code)}";

        var qrPng = RenderQrPng(targetUrl);
        var isCandidate = kind == AmbassadeurFlyerKind.Candidate;
        var accent = isCandidate ? SoftBlue : SoftGreen;
        var title = isCandidate ? "Word kandidaat via Lobsy" : "Gratis start-highlight voor ondernemers";
        var lead = isCandidate
            ? "Scan de QR-code of gebruik de trackinglink om je te registreren als kandidaat. Banen dichtbij, match op reistijd."
            : "Scan de QR-code om te registreren als ondernemer. De Ambassadeur-code is al ingevuld — je ontvangt een gratis start-highlight.";
        var ctaHint = isCandidate
            ? $"Kandidatenlink: {baseUrl}/werven/{code}"
            : $"Registratie: {baseUrl}/register?ref={code}";

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

                        row.RelativeItem().Column(titleCol =>
                        {
                            titleCol.Item().Text(brand).FontSize(22).Bold().FontColor(Colors.White);
                            titleCol.Item().Text(isCandidate
                                    ? "Ambassadeur · kandidatenflyer"
                                    : "Ambassadeur · ondernemersflyer")
                                .FontSize(10).FontColor(Colors.White);
                        });
                    });
                });

                page.Content().PaddingVertical(18).Column(col =>
                {
                    col.Spacing(14);
                    col.Item().Text(title).FontSize(18).Bold().FontColor(BrandNavy);
                    col.Item().Text(lead);

                    col.Item().Background(accent).Padding(14).Row(row =>
                    {
                        row.RelativeItem().Column(info =>
                        {
                            info.Spacing(6);
                            info.Item().Text("Trackingcode").FontSize(9).FontColor(BrandNavy);
                            info.Item().Text(code).FontSize(22).Bold().FontColor(BrandNavy);
                            info.Item().Text(ctaHint).FontSize(9);
                        });
                        row.ConstantItem(140).Height(140).Image(qrPng).FitArea();
                    });

                    if (isCandidate)
                    {
                        col.Item().Text("Waarom Lobsy voor kandidaten?").FontSize(14).Bold().FontColor(BrandNavy);
                        col.Item().Text("✓ Vacatures die echt in de buurt liggen (reistijd)");
                        col.Item().Text("✓ Solliciteren zonder ellenlange formulieren");
                        col.Item().Text("✓ Overzicht van je sollicitaties op één plek");
                    }
                    else
                    {
                        col.Item().Text("Waarom Lobsy voor ondernemers?").FontSize(14).Bold().FontColor(BrandNavy);
                        col.Item().Text("✓ Hyper-lokaal werven op reistijd");
                        col.Item().Text("✓ Tokens i.p.v. abonnementen");
                        col.Item().Text("✓ Gratis start-highlight via Ambassadeur-code");
                    }
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span($"{brand} · Ambassadeur {code} · ")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    txt.Span(DateTime.UtcNow.ToString("d", CultureInfo.GetCultureInfo("nl-NL")))
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
