using System.Globalization;
using Jobsy.Core;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public sealed class EmployerRaamflyerService : IEmployerRaamflyerService
{
    private static readonly Color BrandNavy = Color.FromHex("#0f2d5c");
    private static readonly Color BrandDeep = Color.FromHex("#0a2044");
    private static readonly Color SoftSky = Color.FromHex("#dceef8");
    private static readonly Color SoftMint = Color.FromHex("#e8f5ef");
    private static readonly Color AccentTeal = Color.FromHex("#1a7a6d");
    private static readonly Color AccentCoral = Color.FromHex("#c45c3e");
    private static readonly Color WarmSand = Color.FromHex("#f7f1e6");
    private static readonly Color SoftCoral = Color.FromHex("#f8e8e2");
    private static readonly Color SoftGold = Color.FromHex("#f3e6c8");
    private static readonly Color Slate = Color.FromHex("#2c3a4a");

    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;
    private readonly IPlatformCompanySettingsService _companySettings;

    static EmployerRaamflyerService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public EmployerRaamflyerService(
        JobsyDbContext db,
        IPlatformFeatureService features,
        IPlatformCompanySettingsService companySettings)
    {
        _db = db;
        _features = features;
        _companySettings = companySettings;
    }

    public async Task<RaamflyerQrTarget> ResolveBranchQrTargetAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.Companies.AsNoTracking()
            .AnyAsync(c => c.Id == companyId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException("Vestiging niet gevonden.");
        }

        var baseUrl = await GetBaseUrlAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeIds = await _db.Vacancies.AsNoTracking()
            .Where(v => v.CompanyId == companyId
                        && v.Status == VacancyStatus.Active
                        && v.StartDate <= today
                        && v.EndDate >= today)
            .OrderBy(v => v.Title)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        var posterUrl = $"{baseUrl}/vestiging/{companyId:D}";
        var shortUrl = ShortenDisplay(posterUrl, baseUrl);

        if (activeIds.Count == 1)
        {
            return new RaamflyerQrTarget(
                posterUrl,
                shortUrl,
                1,
                activeIds[0],
                RaamflyerQrKind.VacancyDetail);
        }

        return new RaamflyerQrTarget(
            posterUrl,
            shortUrl,
            activeIds.Count,
            null,
            activeIds.Count == 0 ? RaamflyerQrKind.MapEmptyBranch : RaamflyerQrKind.MapCompanyCluster);
    }

    public async Task<byte[]> RenderBranchFlyerAsync(
        Guid companyId,
        RaamflyerFormat format = RaamflyerFormat.A4,
        CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new KeyNotFoundException("Vestiging niet gevonden.");

        var target = await ResolveBranchQrTargetAsync(companyId, cancellationToken);
        var platform = await _companySettings.GetAsync(cancellationToken);
        var platformBrand = string.IsNullOrWhiteSpace(platform.CompanyName) ? "Lobsy" : platform.CompanyName.Trim();
        var logo = _companySettings.GetBrandLogoPng();
        var vacancyTitles = await LoadActiveTitlesAsync(companyId, cancellationToken);

        return RenderPoster(
            format,
            platformBrand,
            logo,
            employerName: company.Name,
            locationLine: company.Address,
            target,
            vacancyTitles,
            isOverview: false);
    }

    public async Task<byte[]> RenderOverviewFlyerAsync(
        IReadOnlyList<Guid> companyIds,
        string title,
        RaamflyerFormat format = RaamflyerFormat.A4,
        CancellationToken cancellationToken = default)
    {
        if (companyIds.Count == 0)
        {
            throw new ArgumentException("Geen vestigingen geselecteerd voor de overzichtsflyer.");
        }

        var baseUrl = await GetBaseUrlAsync(cancellationToken);
        var platform = await _companySettings.GetAsync(cancellationToken);
        var platformBrand = string.IsNullOrWhiteSpace(platform.CompanyName) ? "Lobsy" : platform.CompanyName.Trim();
        var logo = _companySettings.GetBrandLogoPng();

        var csv = string.Join(',', companyIds.Select(id => id.ToString("N")));
        var url = $"{baseUrl}/?companies={Uri.EscapeDataString(csv)}";
        var target = new RaamflyerQrTarget(
            url,
            ShortenDisplay($"{baseUrl}/?companies=…", baseUrl),
            await CountActiveAsync(companyIds, cancellationToken),
            null,
            RaamflyerQrKind.MapCompanyCluster);

        var branches = await _db.Companies.AsNoTracking()
            .Where(c => companyIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .Take(8)
            .ToListAsync(cancellationToken);

        var employerName = string.IsNullOrWhiteSpace(title) ? "onze vestigingen" : title.Trim();

        return RenderPoster(
            format,
            platformBrand,
            logo,
            employerName: employerName,
            locationLine: string.Join(" · ", branches),
            target,
            branches,
            isOverview: true);
    }

    private async Task<IReadOnlyList<string>> LoadActiveTitlesAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.Vacancies.AsNoTracking()
            .Where(v => v.CompanyId == companyId
                        && v.Status == VacancyStatus.Active
                        && v.StartDate <= today
                        && v.EndDate >= today)
            .OrderBy(v => v.Title)
            .Select(v => v.Title)
            .Take(8)
            .ToListAsync(cancellationToken);
    }

    private async Task<int> CountActiveAsync(
        IReadOnlyList<Guid> companyIds,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.Vacancies.AsNoTracking()
            .CountAsync(
                v => companyIds.Contains(v.CompanyId)
                     && v.Status == VacancyStatus.Active
                     && v.StartDate <= today
                     && v.EndDate >= today,
                cancellationToken);
    }

    private async Task<string> GetBaseUrlAsync(CancellationToken cancellationToken)
    {
        var features = await _features.GetAsync(cancellationToken);
        return JobsyPublicUrl.NormalizeOrigin(features.PublicWebBaseUrl).TrimEnd('/');
    }

    private static string ShortenDisplay(string absoluteUrl, string baseUrl)
    {
        var host = baseUrl.Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');
        if (absoluteUrl.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
        {
            return host + absoluteUrl[baseUrl.Length..];
        }

        return absoluteUrl;
    }

    private static byte[] RenderPoster(
        RaamflyerFormat format,
        string platformBrand,
        byte[] logo,
        string employerName,
        string? locationLine,
        RaamflyerQrTarget target,
        IReadOnlyList<string> lines,
        bool isOverview)
    {
        var pageSize = format == RaamflyerFormat.A3 ? PageSizes.A3 : PageSizes.A4;
        var qrPng = RenderQrPng(target.AbsoluteUrl);
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var displayName = string.IsNullOrWhiteSpace(employerName) ? "ons" : employerName.Trim();
        var headline = $"Kom werken bij {displayName}";
        var sub = isOverview
            ? "Je bent uitgenodigd — scan de QR en bekijk openstaande functies bij onze vestigingen."
            : target.Kind switch
            {
                RaamflyerQrKind.VacancyDetail =>
                    "Je bent uitgenodigd — scan en solliciteer direct op onze openstaande functie.",
                RaamflyerQrKind.MapCompanyCluster =>
                    $"Je bent uitgenodigd — scan voor {target.ActiveVacancyCount} openstaande functies bij deze vestiging.",
                _ => "Je bent uitgenodigd — scan voor actuele vacatures bij deze vestiging."
            };

        var pad = format == RaamflyerFormat.A3 ? 32f : 26f;
        var titleSize = format == RaamflyerFormat.A3 ? 32f : 26f;
        var listLimit = format == RaamflyerFormat.A3 ? 8 : 6;
        var qrBox = format == RaamflyerFormat.A3 ? 180f : 150f;
        var qrImage = format == RaamflyerFormat.A3 ? 150f : 124f;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(pageSize);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(format == RaamflyerFormat.A3 ? 12 : 10).FontColor(BrandDeep));

                page.Content().Column(root =>
                {
                    root.Item().Background(BrandNavy).Padding(pad).Column(hero =>
                    {
                        hero.Spacing(6);
                        hero.Item().Row(row =>
                        {
                            if (logo is { Length: > 0 })
                            {
                                row.ConstantItem(format == RaamflyerFormat.A3 ? 60 : 48)
                                    .Height(format == RaamflyerFormat.A3 ? 42 : 32)
                                    .Image(logo).FitArea();
                                row.ConstantItem(10);
                            }

                            row.RelativeItem().AlignMiddle().Column(brandCol =>
                            {
                                brandCol.Item().Text(platformBrand)
                                    .FontSize(format == RaamflyerFormat.A3 ? 24 : 18)
                                    .Bold().FontColor(Colors.White);
                                brandCol.Item().Text(displayName)
                                    .FontSize(format == RaamflyerFormat.A3 ? 14 : 12)
                                    .FontColor(SoftSky);
                            });

                            row.ConstantItem(format == RaamflyerFormat.A3 ? 110 : 92)
                                .AlignMiddle().AlignRight()
                                .Background(AccentCoral).PaddingHorizontal(9).PaddingVertical(5)
                                .Text("Raamflyer").FontSize(9).Bold().FontColor(Colors.White);
                        });

                        hero.Item().PaddingTop(10).Text("Uitnodiging")
                            .FontSize(format == RaamflyerFormat.A3 ? 14 : 12)
                            .FontColor(SoftGold);
                        hero.Item().Text(headline)
                            .FontSize(titleSize)
                            .Bold().FontColor(Colors.White);
                        hero.Item().Text("dichtbij huis!")
                            .FontSize(format == RaamflyerFormat.A3 ? 20 : 16)
                            .Bold().FontColor(SoftSky);

                        if (!string.IsNullOrWhiteSpace(locationLine))
                        {
                            hero.Item().PaddingTop(4).Text(locationLine)
                                .FontSize(format == RaamflyerFormat.A3 ? 11 : 9)
                                .FontColor(Colors.White);
                        }
                    });

                    root.Item().Height(5).Background(AccentTeal);

                    root.Item().Background(SoftSky).Padding(pad).Column(body =>
                    {
                        body.Spacing(10);
                        body.Item().Text(sub)
                            .FontSize(format == RaamflyerFormat.A3 ? 13 : 11);

                        body.Item().Row(row =>
                        {
                            row.RelativeItem().PaddingRight(12).Column(left =>
                            {
                                left.Spacing(6);
                                left.Item().Text(isOverview ? "Onze vestigingen" : "Openstaande functies")
                                    .FontSize(format == RaamflyerFormat.A3 ? 14 : 13)
                                    .Bold().FontColor(BrandNavy);

                                if (lines.Count == 0)
                                {
                                    left.Item().Background(WarmSand).Padding(10)
                                        .Text("Binnenkort nieuwe vacatures — houd de QR in de gaten.")
                                        .FontColor(Slate);
                                }
                                else
                                {
                                    left.Item().Background(Colors.White).Border(1).BorderColor(AccentTeal)
                                        .Padding(10).Column(list =>
                                        {
                                            list.Spacing(3);
                                            foreach (var line in lines.Take(listLimit))
                                            {
                                                list.Item().Text($"• {line}");
                                            }
                                        });
                                }

                                left.Item().Background(SoftCoral).Padding(10).Text(t =>
                                {
                                    t.Span("Tip voor managers: ").Bold().FontColor(AccentCoral);
                                    t.Span(
                                        "print op A4/A3, hang in de etalage of bij de kassa. " +
                                        "Alleen scannen — geen getypte link nodig.").FontColor(Slate);
                                });
                            });

                            row.ConstantItem(qrBox).Background(SoftGold).Border(2).BorderColor(AccentTeal)
                                .Padding(10).Column(qr =>
                                {
                                    qr.Item().AlignCenter().Text("Solliciteer hier")
                                        .FontSize(10).Bold().FontColor(AccentTeal);
                                    qr.Item().PaddingTop(6).Height(qrImage).Image(qrPng).FitArea();
                                    qr.Item().PaddingTop(6).AlignCenter()
                                        .Text("Scan met je telefoon")
                                        .FontSize(9).Bold().FontColor(BrandNavy);
                                });
                        });

                        body.Item().Background(SoftMint).Padding(10).Text(
                            $"{platformBrand} matcht op reistijd — vacatures dichtbij in Westland & Den Haag.");
                    });

                    root.Item().ExtendVertical().AlignBottom().Background(BrandDeep)
                        .PaddingVertical(10).AlignCenter().Text(txt =>
                        {
                            txt.Span($"{platformBrand} · raamflyer · {DateTime.UtcNow.ToString("d", culture)}")
                                .FontSize(8).FontColor(Colors.White);
                        });
                });
            });
        }).GeneratePdf();
    }

    private static byte[] RenderQrPng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(20);
    }
}
