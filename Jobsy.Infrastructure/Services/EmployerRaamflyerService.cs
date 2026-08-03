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
        var logo = _companySettings.GetBrandLogoPng();
        var brand = string.IsNullOrWhiteSpace(platform.CompanyName) ? "Lobsy" : platform.CompanyName.Trim();
        var vacancyTitles = await LoadActiveTitlesAsync(companyId, cancellationToken);

        return RenderPoster(
            format,
            brand,
            logo,
            company.Name,
            company.Address,
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
        var logo = _companySettings.GetBrandLogoPng();
        var brand = string.IsNullOrWhiteSpace(platform.CompanyName) ? "Lobsy" : platform.CompanyName.Trim();

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

        return RenderPoster(
            format,
            brand,
            logo,
            string.IsNullOrWhiteSpace(title) ? "Onze vestigingen" : title.Trim(),
            string.Join(" · ", branches),
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
        string brand,
        byte[] logo,
        string heading,
        string? addressLine,
        RaamflyerQrTarget target,
        IReadOnlyList<string> lines,
        bool isOverview)
    {
        var pageSize = format == RaamflyerFormat.A3 ? PageSizes.A3 : PageSizes.A4;
        var qrPng = RenderQrPng(target.AbsoluteUrl);
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var headline = isOverview ? "Werken dichtbij huis!" : "Werken dichtbij huis!";
        var sub = isOverview
            ? "Bekijk alle openstaande functies in onze vestigingen — scan de QR-code."
            : target.Kind switch
            {
                RaamflyerQrKind.VacancyDetail => "Scan en solliciteer direct op onze openstaande functie.",
                RaamflyerQrKind.MapCompanyCluster =>
                    $"Scan voor {target.ActiveVacancyCount} openstaande functies bij deze vestiging.",
                _ => "Scan voor actuele vacatures bij deze vestiging."
            };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(pageSize);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontSize(format == RaamflyerFormat.A3 ? 13 : 11).FontColor(BrandDeep));

                page.Content().Column(root =>
                {
                    root.Item().Background(BrandNavy).Padding(format == RaamflyerFormat.A3 ? 40 : 32).Column(hero =>
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

                            row.RelativeItem().AlignMiddle().Text(brand)
                                .FontSize(format == RaamflyerFormat.A3 ? 28 : 22)
                                .Bold().FontColor(Colors.White);
                        });

                        hero.Item().PaddingTop(18).Text(headline)
                            .FontSize(format == RaamflyerFormat.A3 ? 36 : 30)
                            .Bold().FontColor(Colors.White);

                        hero.Item().Text(heading)
                            .FontSize(format == RaamflyerFormat.A3 ? 20 : 16)
                            .FontColor(SoftSky);

                        if (!string.IsNullOrWhiteSpace(addressLine))
                        {
                            hero.Item().Text(addressLine).FontSize(10).FontColor(Colors.White);
                        }
                    });

                    root.Item().Padding(format == RaamflyerFormat.A3 ? 40 : 32).Column(body =>
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
                            "Lobsy matcht op reistijd — vacatures dichtbij in Westland & Den Haag.");
                    });

                    root.Item().Background(BrandDeep).Padding(12).AlignCenter().Text(txt =>
                    {
                        txt.Span($"{brand} · raamflyer · {DateTime.UtcNow.ToString("d", culture)}")
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
        return png.GetGraphic(8);
    }
}
