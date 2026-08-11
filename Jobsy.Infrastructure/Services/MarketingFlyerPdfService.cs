using Jobsy.Core;
using Jobsy.Core.Interfaces;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Professional A4 employer marketing flyer — logo-forward, admin-editable copy.
/// </summary>
public sealed class MarketingFlyerPdfService : IMarketingFlyerPdfService
{
    private static readonly Color BrandNavy = Color.FromHex("#0f2d5c");
    private static readonly Color BrandDeep = Color.FromHex("#0a2044");
    private static readonly Color SoftSky = Color.FromHex("#dceef8");
    private static readonly Color SoftMint = Color.FromHex("#e8f5ef");
    private static readonly Color WarmSand = Color.FromHex("#f7f1e6");
    private static readonly Color AccentTeal = Color.FromHex("#1a7a6d");
    private static readonly Color AccentCoral = Color.FromHex("#c45c3e");
    private static readonly Color SoftCoral = Color.FromHex("#f8e8e2");
    private static readonly Color Slate = Color.FromHex("#2c3a4a");

    private readonly IMarketingFlyerSettingsService _flyerSettings;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly IPlatformFeatureService _features;

    static MarketingFlyerPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public MarketingFlyerPdfService(
        IMarketingFlyerSettingsService flyerSettings,
        IPlatformCompanySettingsService companySettings,
        IPlatformFeatureService features)
    {
        _flyerSettings = flyerSettings;
        _companySettings = companySettings;
        _features = features;
    }

    public async Task<byte[]> RenderAsync(CancellationToken cancellationToken = default)
    {
        var content = await _flyerSettings.GetAsync(cancellationToken);
        var platform = await _companySettings.GetAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        var logo = _companySettings.GetBrandLogoPng();
        var brand = string.IsNullOrWhiteSpace(platform.CompanyName) ? "Lobsy" : platform.CompanyName.Trim();
        var baseUrl = JobsyPublicUrl.NormalizeOrigin(features.PublicWebBaseUrl).TrimEnd('/');
        var qrTarget = BuildQrTarget(baseUrl, content.QrPath);
        var qrPng = RenderQrPng(qrTarget);
        var bullets = content.BulletPoints.Take(10).ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Slate));

                page.Content().Column(root =>
                {
                    // Brand-first hero — logo is the visual anchor.
                    root.Item().Background(BrandNavy).Padding(26).Column(hero =>
                    {
                        hero.Spacing(10);

                        hero.Item().Row(row =>
                        {
                            if (logo is { Length: > 0 })
                            {
                                row.ConstantItem(92).Height(64)
                                    .Background(Colors.White)
                                    .Padding(8)
                                    .Image(logo).FitArea();
                                row.ConstantItem(14);
                            }

                            row.RelativeItem().AlignMiddle().Column(brandCol =>
                            {
                                brandCol.Item().Text(brand)
                                    .FontSize(34).Bold().FontColor(Colors.White);
                                brandCol.Item().Text("Werkgeversflyer · Westland & omgeving")
                                    .FontSize(9).FontColor(SoftSky);
                            });
                        });

                        hero.Item().PaddingTop(4)
                            .Text(content.Headline)
                            .FontSize(22).Bold().FontColor(Colors.White);

                        hero.Item().Text(content.Subheadline)
                            .FontSize(12).FontColor(SoftSky);

                        if (!string.IsNullOrWhiteSpace(content.Intro))
                        {
                            hero.Item().PaddingTop(4).Text(content.Intro)
                                .FontSize(9).FontColor(SoftSky);
                        }
                    });

                    root.Item().Height(5).Background(AccentTeal);

                    // Launch promo strip
                    root.Item().Background(AccentCoral).PaddingHorizontal(22).PaddingVertical(10).Row(promo =>
                    {
                        promo.RelativeItem().Column(col =>
                        {
                            col.Item().Text(content.PromoFreeText)
                                .FontSize(10).Bold().FontColor(Colors.White);
                            col.Item().Text(content.PromoDiscountText)
                                .FontSize(9).FontColor(SoftCoral);
                        });
                        promo.ConstantItem(88).AlignMiddle().AlignRight()
                            .Background(Colors.White).PaddingHorizontal(8).PaddingVertical(6)
                            .Text("INTRO 2026").FontSize(8).Bold().FontColor(AccentCoral);
                    });

                    root.Item().Padding(20).Column(body =>
                    {
                        body.Spacing(8);

                        body.Item().Text("Waarom ondernemers voor Lobsy kiezen")
                            .FontSize(12).Bold().FontColor(BrandNavy);

                        // Two-column USP grid
                        for (var i = 0; i < bullets.Count; i += 2)
                        {
                            var left = bullets[i];
                            var right = i + 1 < bullets.Count ? bullets[i + 1] : null;
                            body.Item().Row(row =>
                            {
                                row.Spacing(8);
                                row.RelativeItem().Element(e => UspCard(e, left));
                                if (right is not null)
                                {
                                    row.RelativeItem().Element(e => UspCard(e, right));
                                }
                                else
                                {
                                    row.RelativeItem();
                                }
                            });
                        }

                        body.Item().PaddingTop(4).Background(WarmSand).Padding(12).Row(cta =>
                        {
                            cta.RelativeItem().PaddingRight(12).Column(info =>
                            {
                                info.Spacing(4);
                                info.Item().Text(content.CtaTitle)
                                    .FontSize(14).Bold().FontColor(BrandNavy);
                                info.Item().Text(content.CtaBody)
                                    .FontSize(9).FontColor(Slate);
                                info.Item().PaddingTop(4)
                                    .Text(qrTarget.Replace("https://", "", StringComparison.OrdinalIgnoreCase)
                                        .Replace("http://", "", StringComparison.OrdinalIgnoreCase))
                                    .FontSize(8).FontColor(AccentTeal);
                            });

                            cta.ConstantItem(118).Background(Colors.White)
                                .Border(2).BorderColor(AccentTeal)
                                .Padding(7).Column(qr =>
                                {
                                    qr.Item().AlignCenter().Width(96).Height(96).Image(qrPng).FitArea();
                                    qr.Item().PaddingTop(4).AlignCenter()
                                        .Text(content.QrCaption)
                                        .FontSize(7).Bold().FontColor(BrandNavy);
                                });
                        });

                        body.Item().AlignCenter()
                            .Text(content.FooterNote)
                            .FontSize(8).FontColor(BrandDeep);
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void UspCard(IContainer container, string text)
    {
        container.Background(SoftMint).Padding(8).Row(row =>
        {
            row.ConstantItem(10).AlignTop()
                .Text("●").FontSize(7).FontColor(AccentTeal);
            row.RelativeItem().Text(text).FontSize(8).FontColor(BrandDeep);
        });
    }

    private static string BuildQrTarget(string baseUrl, string qrPath)
    {
        if (qrPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || qrPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return qrPath;
        }

        var path = qrPath.StartsWith('/') ? qrPath : "/" + qrPath;
        return $"{baseUrl.TrimEnd('/')}{path}";
    }

    private static byte[] RenderQrPng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(8);
    }
}
