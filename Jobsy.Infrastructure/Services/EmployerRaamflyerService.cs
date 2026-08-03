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
    private static readonly Color AccentTeal = Color.FromHex("#1a7a6d");
    private static readonly Color WarmSand = Color.FromHex("#f7f1e6");

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

        // Stable poster QR: /vestiging/{id} resolves at scan-time (1→vacancy, 2+→map cluster).
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

        // Overview QR: map filtered to all listed companies (comma-separated).
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
            .Take(12)
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
        var headline = $"Kom werken bij {displayName} dichtbij huis!";
        var sub = isOverview
            ? "Scan de QR-code en bekijk alle openstaande functies in onze vestigingen."
            : target.Kind switch
            {
                RaamflyerQrKind.VacancyDetail =>
                    "Scan en solliciteer direct op onze openstaande functie.",
                RaamflyerQrKind.MapCompanyCluster =>
                    $"Scan voor {target.ActiveVacancyCount} openstaande functies bij deze vestiging.",
                _ => "Scan voor actuele vacatures bij deze vestiging."
            };

        var pad = format == RaamflyerFormat.A3 ? 40f : 32f;
        var titleSize = format == RaamflyerFormat.A3 ? 34f : 28f;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(pageSize);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(format == RaamflyerFormat.A3 ? 13 : 11).FontColor(BrandDeep));

                page.Content().Column(root =>
                {
                    root.Item().Background(BrandNavy).Padding(pad).Column(hero =>
                    {
                        hero.Spacing(10);
                        hero.Item().Row(row =>
                        {
                            if (logo is { Length: > 0 })
                            {
                                row.ConstantItem(format == RaamflyerFormat.A3 ? 72 : 56)
                                    .Height(format == RaamflyerFormat.A3 ? 52 : 40)
                                    .Image(logo).FitArea();
                                row.ConstantItem(14);
                            }

                            row.RelativeItem().AlignMiddle().Column(brandCol =>
                            {
                                brandCol.Item().Text(platformBrand)
                                    .FontSize(format == RaamflyerFormat.A3 ? 26 : 20)
                                    .Bold().FontColor(Colors.White);
                                brandCol.Item().Text(displayName)
                                    .FontSize(format == RaamflyerFormat.A3 ? 16 : 13)
                                    .FontColor(SoftSky);
                            });
                        });

                        hero.Item().PaddingTop(16).Text(headline)
                            .FontSize(titleSize)
                            .Bold().FontColor(Colors.White);

                        if (!string.IsNullOrWhiteSpace(locationLine))
                        {
                            hero.Item().Text(locationLine).FontSize(10).FontColor(Colors.White);
                        }
                    });

                    root.Item().Padding(pad).Column(body =>
                    {
                        body.Spacing(14);
                        body.Item().Text(sub).FontSize(format == RaamflyerFormat.A3 ? 14 : 12);

                        body.Item().Row(row =>
                        {
                            row.RelativeItem().PaddingRight(16).Column(left =>
                            {
                                left.Spacing(8);
                                left.Item().Text(isOverview ? "Vestigingen" : "Openstaande functies")
                                    .FontSize(14).Bold().FontColor(BrandNavy);

                                if (lines.Count == 0)
                                {
                                    left.Item().Text("Binnenkort nieuwe vacatures — houd de QR in de gaten.")
                                        .FontColor(Colors.Grey.Darken2);
                                }
                                else
                                {
                                    foreach (var line in lines.Take(8))
                                    {
                                        left.Item().Text($"• {line}");
                                    }
                                }

                                left.Item().PaddingTop(12).Background(WarmSand).Padding(12).Column(box =>
                                {
                                    box.Spacing(4);
                                    box.Item().Text("Korte link").FontSize(9).FontColor(AccentTeal);
                                    box.Item().Text(target.ShortDisplayUrl)
                                        .FontSize(format == RaamflyerFormat.A3 ? 14 : 12)
                                        .Bold().FontColor(BrandNavy);
                                });
                            });

                            row.ConstantItem(format == RaamflyerFormat.A3 ? 180 : 150).Column(qr =>
                            {
                                qr.Item().Border(2).BorderColor(AccentTeal).Padding(10).Column(inner =>
                                {
                                    inner.Item().Height(format == RaamflyerFormat.A3 ? 160 : 130)
                                        .Image(qrPng).FitArea();
                                    inner.Item().PaddingTop(8).AlignCenter()
                                        .Text("Scan met je telefoon")
                                        .FontSize(9).FontColor(AccentTeal);
                                });
                            });
                        });

                        body.Item().Background(SoftSky).Padding(12).Text(
                            $"{platformBrand} matcht op reistijd — vacatures dichtbij in Westland & Den Haag.");
                    });

                    root.Item().Background(BrandDeep).Padding(12).AlignCenter().Text(txt =>
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
        // Higher module size keeps the QR sharp on A3 window prints.
        return png.GetGraphic(20);
    }
}
