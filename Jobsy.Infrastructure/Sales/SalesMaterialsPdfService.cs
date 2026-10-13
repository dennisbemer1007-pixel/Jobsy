using System.Globalization;
using Jobsy.Core;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Sales;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesMaterialsPdfService : ISalesMaterialsPdfService
{
    private static readonly Color BrandNavy = Color.FromHex("#0f2d5c");
    private static readonly Color BrandDeep = Color.FromHex("#0a2044");
    private static readonly Color SoftBlue = Color.FromHex("#e8eef7");
    private static readonly Color SoftMint = Color.FromHex("#e8f5ef");
    private static readonly Color WarmSand = Color.FromHex("#f7f1e6");
    private static readonly Color AccentTeal = Color.FromHex("#1a7a6d");
    private static readonly Color AccentCoral = Color.FromHex("#c45c3e");
    private static readonly Color SoftSky = Color.FromHex("#dceef8");
    private static readonly Color Slate = Color.FromHex("#2c3a4a");

    private readonly ISalesPriceQuote _quote;
    private readonly ISalesCommercialService _sales;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly IPlatformFeatureService _features;
    private readonly ILegalIdentity _legal;

    static SalesMaterialsPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public SalesMaterialsPdfService(
        ISalesPriceQuote quote,
        ISalesCommercialService sales,
        IPlatformCompanySettingsService companySettings,
        IPlatformFeatureService features,
        ILegalIdentity legal)
    {
        _quote = quote;
        _sales = sales;
        _companySettings = companySettings;
        _features = features;
        _legal = legal;
    }

    public async Task<byte[]> FlyerA4Async(string trackingCode, CancellationToken cancellationToken = default)
    {
        var ctx = await BuildContextAsync(trackingCode, "flyer", cancellationToken);
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var quote = ctx.Quote;
        var bonus = ctx.StartHighlightBonus.ToString("0.##", culture);
        var costs = quote.Actions.Where(a => a.Key.StartsWith("vacancy.", StringComparison.Ordinal)).Take(3).ToList();
        var packs = quote.Packs.Take(4).ToList();

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
                            if (ctx.Logo is { Length: > 0 })
                            {
                                row.ConstantItem(44).Height(30).Image(ctx.Logo).FitArea();
                                row.ConstantItem(8);
                            }

                            row.RelativeItem().AlignMiddle().Column(title =>
                            {
                                title.Item().Text(ctx.Brand).FontSize(20).Bold().FontColor(Colors.White);
                                title.Item().Text("Lobsy Partner · uitnodiging voor werkgevers")
                                    .FontSize(9).FontColor(SoftSky);
                            });
                            row.ConstantItem(90).AlignMiddle().AlignRight()
                                .Background(AccentCoral).PaddingHorizontal(8).PaddingVertical(5)
                                .Text(SalesPdfCopy.FlyerBadge).FontSize(8).Bold().FontColor(Colors.White);
                        });

                        hero.Item().PaddingTop(8)
                            .Text(SalesPdfCopy.FlyerTitle)
                            .FontSize(20).Bold().FontColor(Colors.White);
                        hero.Item().Text(SalesPdfCopy.FlyerLead)
                            .FontSize(9).FontColor(SoftSky);
                    });

                    root.Item().Height(5).Background(AccentTeal);

                    root.Item().Padding(22).Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Background(SoftMint).Padding(9).Column(usp =>
                        {
                            usp.Spacing(2);
                            usp.Item().Text(SalesPdfCopy.FlyerWhyTitle).Bold().FontColor(AccentTeal).FontSize(10);
                            usp.Item().Text(SalesPdfCopy.FlyerWhy1).FontSize(8);
                            usp.Item().Text(SalesPdfCopy.FlyerWhy2).FontSize(8);
                            usp.Item().Text(SalesPdfCopy.FlyerWhy3).FontSize(8);
                            usp.Item().Text($"• Start-highlight t.w.v. {bonus} tokens bij aanmelding via salescode")
                                .FontSize(8);
                        });

                        if (costs.Count > 0)
                        {
                            col.Item().Text(SalesPdfCopy.FlyerRatesTitle).FontSize(11).Bold().FontColor(BrandNavy);
                            col.Item().Row(row =>
                            {
                                row.Spacing(6);
                                foreach (var cost in costs)
                                {
                                    row.RelativeItem().Background(SoftBlue).Padding(7).Column(card =>
                                    {
                                        card.Item().Text(cost.Label).Bold().FontSize(8).FontColor(BrandNavy);
                                        card.Item().Text(
                                                $"{cost.CostTokens.ToString("0.##", culture)} tokens · {SalesMoney.FormatPlain(cost.FromEuro)}")
                                            .FontSize(7).FontColor(Slate);
                                    });
                                }
                            });
                        }

                        if (packs.Count > 0)
                        {
                            col.Item().Text(SalesPdfCopy.FlyerPacksTitle).FontSize(11).Bold().FontColor(BrandNavy);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1.2f);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1.2f);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Background(BrandNavy).Padding(4).Text("Pakket").FontColor(Colors.White).Bold().FontSize(8);
                                    h.Cell().Background(BrandNavy).Padding(4).Text("Prijs").FontColor(Colors.White).Bold().FontSize(8);
                                    h.Cell().Background(BrandNavy).Padding(4).Text("Per token").FontColor(Colors.White).Bold().FontSize(8);
                                });
                                foreach (var pack in packs)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(4)
                                        .Text($"{pack.PackSize} tokens").FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(4)
                                        .Text(SalesMoney.FormatPlain(pack.PriceExVat)).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(4)
                                        .Text(SalesMoney.FormatPlain(pack.PricePerToken)).FontSize(8);
                                }
                            });
                        }

                        col.Item().Background(WarmSand).Padding(10).Row(cta =>
                        {
                            cta.RelativeItem().PaddingRight(10).Column(info =>
                            {
                                info.Spacing(3);
                                info.Item().Text(SalesPdfCopy.FlyerCta).Bold().FontColor(AccentCoral);
                                info.Item().Text(ctx.ShortDisplay).FontSize(9).FontColor(Slate);
                                info.Item().Text(ctx.Code).FontSize(20).Bold().FontColor(BrandNavy);
                            });
                            cta.ConstantItem(118).Background(Colors.White).Border(2).BorderColor(AccentTeal)
                                .Padding(7).Column(qr =>
                                {
                                    qr.Item().Height(100).Image(ctx.QrPng).FitArea();
                                    qr.Item().PaddingTop(3).AlignCenter()
                                        .Text(SalesPdfCopy.FlyerScan)
                                        .FontSize(8).Bold().FontColor(AccentTeal);
                                });
                        });
                    });

                    root.Item().ExtendVertical().AlignBottom().Background(BrandDeep)
                        .PaddingVertical(9).AlignCenter()
                        .Text($"{ctx.Brand} · {ctx.Code} · {ctx.ShortDisplay}")
                        .FontSize(8).FontColor(Colors.White);
                });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> BusinessCardsAsync(string trackingCode, CancellationToken cancellationToken = default)
    {
        var ctx = await BuildContextAsync(trackingCode, "flyer", cancellationToken);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(12);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(BrandDeep));

                page.Header().Column(h =>
                {
                    h.Item().Text(SalesPdfCopy.CardsTitle).Bold().FontSize(12).FontColor(BrandNavy);
                    h.Item().Text(SalesPdfCopy.CardsHelp).FontSize(8).FontColor(Slate);
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    for (var row = 0; row < 5; row++)
                    {
                        col.Item().Row(r =>
                        {
                            for (var colIdx = 0; colIdx < 2; colIdx++)
                            {
                                r.RelativeItem().Border(0.5f).BorderColor(SoftBlue).Padding(8).Height(92).Row(card =>
                                {
                                    card.RelativeItem().Column(info =>
                                    {
                                        info.Item().Text(ctx.Brand).Bold().FontSize(11).FontColor(BrandNavy);
                                        info.Item().Text("Partner · salescode").FontSize(7).FontColor(Slate);
                                        info.Item().PaddingTop(6).Text(ctx.Code).Bold().FontSize(14).FontColor(BrandDeep);
                                        info.Item().Text(ctx.ShortDisplay).FontSize(7).FontColor(Slate);
                                    });
                                    card.ConstantItem(64).AlignMiddle().Column(qr =>
                                    {
                                        qr.Item().Height(56).Image(ctx.QrPng).FitArea();
                                        qr.Item().AlignCenter().Text(SalesPdfCopy.CardsScan).FontSize(6).FontColor(AccentTeal);
                                    });
                                });
                                if (colIdx == 0)
                                {
                                    r.ConstantItem(6);
                                }
                            }
                        });
                        if (row < 4)
                        {
                            col.Item().Height(6);
                        }
                    }
                });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> PriceCardAsync(string trackingCode, CancellationToken cancellationToken = default)
    {
        var ctx = await BuildContextAsync(trackingCode, "flyer", cancellationToken);
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var packages = await _sales.GetPublicCatalogAsync(cancellationToken);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(BrandDeep));

                page.Header().Row(row =>
                {
                    if (ctx.Logo is { Length: > 0 })
                    {
                        row.ConstantItem(40).Height(28).Image(ctx.Logo).FitArea();
                        row.ConstantItem(8);
                    }

                    row.RelativeItem().Column(t =>
                    {
                        t.Item().Text(SalesPdfCopy.PriceTitle).Bold().FontSize(16).FontColor(BrandNavy);
                        t.Item().Text(SalesPdfCopy.PriceLead).FontSize(9).FontColor(Slate);
                    });
                    row.ConstantItem(80).AlignRight().Column(c =>
                    {
                        c.Item().Text(ctx.Code).Bold().FontSize(11).FontColor(BrandDeep);
                        c.Item().Text(ctx.ShortDisplay).FontSize(7).FontColor(Slate);
                    });
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text(SalesPdfCopy.PricePacks).Bold().FontSize(12).FontColor(BrandNavy);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Background(BrandNavy).Padding(5).Text("Pakket").FontColor(Colors.White).Bold().FontSize(8);
                            h.Cell().Background(BrandNavy).Padding(5).Text("Prijs excl. btw").FontColor(Colors.White).Bold().FontSize(8);
                            h.Cell().Background(BrandNavy).Padding(5).Text("Per token").FontColor(Colors.White).Bold().FontSize(8);
                        });
                        foreach (var pack in ctx.Quote.Packs)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(5)
                                .Text($"{pack.PackSize} tokens").FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(5)
                                .Text(SalesMoney.FormatPlain(pack.PriceExVat)).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor(SoftBlue).Padding(5)
                                .Text(SalesMoney.FormatPlain(pack.PricePerToken)).FontSize(9);
                        }
                    });

                    col.Item().Text(SalesPdfCopy.PriceActions).Bold().FontSize(12).FontColor(BrandNavy);
                    foreach (var action in ctx.Quote.Actions.Take(6))
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text(action.Label).FontSize(9);
                            r.ConstantItem(90).AlignRight()
                                .Text($"{action.CostTokens.ToString("0.##", culture)} tokens").FontSize(9).FontColor(Slate);
                            r.ConstantItem(100).AlignRight()
                                .Text(string.Format(culture, SalesPdfCopy.PriceFrom, SalesMoney.FormatPlain(action.FromEuro)))
                                .FontSize(9).Bold();
                        });
                    }

                    if (packages.Packages.Count > 0)
                    {
                        col.Item().PaddingTop(6).Text(SalesPdfCopy.PriceSalesPackages).Bold().FontSize(12).FontColor(BrandNavy);
                        foreach (var pack in packages.Packages.Take(6))
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"{pack.Name} · {pack.TokenAmount} tokens").FontSize(9);
                                r.ConstantItem(100).AlignRight().Text(SalesMoney.FormatPlain(pack.PriceEuro)).FontSize(9).Bold();
                            });
                        }
                    }

                    col.Item().PaddingTop(10).Background(WarmSand).Padding(10).Row(cta =>
                    {
                        cta.RelativeItem().Column(info =>
                        {
                            info.Item().Text(SalesPdfCopy.PriceFooter).FontSize(8).FontColor(Slate);
                            info.Item().Text(ctx.ShortDisplay).FontSize(9).Bold().FontColor(BrandDeep);
                        });
                        cta.ConstantItem(90).Height(80).Image(ctx.QrPng).FitArea();
                    });
                });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> PresentationAsync(
        string trackingCode,
        string salesManagerName,
        string companyName,
        string accountEmail,
        CancellationToken cancellationToken = default)
    {
        var ctx = await BuildContextAsync(trackingCode, "qr", cancellationToken);
        var min = SalesMoney.FormatPlain(ctx.Quote.MinPricePerToken);
        var bonus = ctx.StartHighlightBonus.ToString("0.##", CultureInfo.GetCultureInfo("nl-NL"));
        var name = string.IsNullOrWhiteSpace(salesManagerName) ? "Salesmanager" : salesManagerName.Trim();
        var company = string.IsNullOrWhiteSpace(companyName) ? name : companyName.Trim();
        var email = accountEmail?.Trim() ?? string.Empty;

        var slides = new (string Title, string Body, bool ShowQr)[]
        {
            (SalesPdfCopy.PresWelcome, $"{name} · {company}\n{ctx.Brand} Partner", false),
            (SalesPdfCopy.PresProblem, SalesPdfCopy.PresProblemBody, false),
            (SalesPdfCopy.PresHow, SalesPdfCopy.PresHowBody, false),
            (SalesPdfCopy.PresCost, string.Format(CultureInfo.GetCultureInfo("nl-NL"), SalesPdfCopy.PresCostBody, min), false),
            (SalesPdfCopy.PresGet, string.Format(CultureInfo.GetCultureInfo("nl-NL"), SalesPdfCopy.PresGetBody, bonus), false),
            (SalesPdfCopy.PresStart, $"{SalesPdfCopy.PresStartBody}\n\n{ctx.Code}\n{ctx.ShortDisplay}", true),
            (SalesPdfCopy.PresContact, $"{SalesPdfCopy.PresContactLead}\n\n{name}\n{company}\n{email}", false),
        };

        return Document.Create(container =>
        {
            foreach (var slide in slides)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(14).FontColor(BrandDeep));

                    page.Header().Row(row =>
                    {
                        if (ctx.Logo is { Length: > 0 })
                        {
                            row.ConstantItem(48).Height(32).Image(ctx.Logo).FitArea();
                            row.ConstantItem(10);
                        }

                        row.RelativeItem().AlignMiddle().Text(ctx.Brand).Bold().FontSize(14).FontColor(BrandNavy);
                        row.ConstantItem(120).AlignRight().AlignMiddle()
                            .Text(ctx.Code).FontSize(10).FontColor(Slate);
                    });

                    page.Content().AlignMiddle().Column(col =>
                    {
                        col.Item().Text(slide.Title).FontSize(28).Bold().FontColor(BrandNavy);
                        col.Item().PaddingTop(16).Text(slide.Body).FontSize(16).FontColor(Slate).LineHeight(1.35f);
                        if (slide.ShowQr)
                        {
                            col.Item().PaddingTop(20).Width(140).Height(140).Image(ctx.QrPng).FitArea();
                        }
                    });

                    page.Footer().AlignCenter()
                        .Text(ctx.ShortDisplay).FontSize(9).FontColor(Slate);
                });
            }
        }).GeneratePdf();
    }

    private async Task<MaterialContext> BuildContextAsync(
        string trackingCode,
        string channel,
        CancellationToken cancellationToken)
    {
        var code = SalesTrackingCodes.Normalize(trackingCode)
                   ?? throw new ArgumentException("Ongeldige salescode.");
        if (!SalesTrackingCodes.IsSalesManager(code))
        {
            throw new ArgumentException("Alleen SM-codes ontvangen persoonlijk materiaal.");
        }

        var quote = await _quote.GetAsync(cancellationToken);
        var settings = await _sales.GetSettingsAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        var logo = _companySettings.GetBrandLogoPng();
        var legal = await _legal.GetAsync(cancellationToken);
        var brand = string.IsNullOrWhiteSpace(legal.TradeName) ? legal.DisplayName : legal.TradeName.Trim();
        var baseUrl = JobsyPublicUrl.NormalizeOrigin(features.PublicWebBaseUrl).TrimEnd('/');
        var shortPath = $"/p/{Uri.EscapeDataString(code)}";
        var shortUrl = $"{baseUrl}{shortPath}?b={channel}";
        var host = Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host)
            ? (uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host)
            : "lobsy.nl";
        var shortDisplay = $"{host}/p/{code}";
        var qrPng = SalesQr.Png(shortUrl, 8);

        return new MaterialContext(
            code,
            shortDisplay,
            shortUrl,
            brand,
            logo,
            qrPng,
            quote,
            settings.StartHighlightBonusTokens);
    }

    private sealed record MaterialContext(
        string Code,
        string ShortDisplay,
        string QrTargetUrl,
        string Brand,
        byte[]? Logo,
        byte[] QrPng,
        SalesPriceQuote Quote,
        decimal StartHighlightBonus);
}
