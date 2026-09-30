using Jobsy.Core;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Sales;

public sealed class SalesLinkToolkitService : ISalesLinkToolkitService
{
    private readonly JobsyDbContext _db;
    private readonly ISalesPriceQuote _quote;
    private readonly ISalesCommercialService _commercial;
    private readonly IPlatformFeatureService _features;

    public SalesLinkToolkitService(
        JobsyDbContext db,
        ISalesPriceQuote quote,
        ISalesCommercialService commercial,
        IPlatformFeatureService features)
    {
        _db = db;
        _quote = quote;
        _commercial = commercial;
        _features = features;
    }

    public async Task<SalesLinkToolkitDto?> GetAsync(
        Guid beneficiaryUserId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.SalesManagerProfiles.AsNoTracking()
            .Where(p => p.UserId == beneficiaryUserId)
            .Select(p => new
            {
                p.TrackingCode,
                p.CompanyName,
                p.ReferredBySalesManagerUserId,
                p.OnboardingCompletedAt,
                p.AgreementSignedAt,
                UserFullName = p.User.FullName,
                UserEmail = p.User.Email
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null
            || string.IsNullOrWhiteSpace(profile.TrackingCode)
            || profile.OnboardingCompletedAt is null
            || profile.AgreementSignedAt is null)
        {
            return null;
        }

        var code = profile.TrackingCode.Trim().ToUpperInvariant();
        var settings = await _commercial.GetSettingsAsync(cancellationToken);
        var quote = await _quote.GetAsync(cancellationToken);
        var features = await _features.GetAsync(cancellationToken);
        var baseUrl = JobsyPublicUrl.NormalizeOrigin(features.PublicWebBaseUrl).TrimEnd('/');

        var shortPath = $"/p/{Uri.EscapeDataString(code)}";
        var shortUrl = $"{baseUrl}{shortPath}";
        var qrUrl = $"{shortUrl}?b=qr";
        var previewUrl = $"{baseUrl}/partner/{Uri.EscapeDataString(code)}?preview=1";
        var displayHost = HostLabel(baseUrl);
        var shortDisplay = $"{displayHost}/p/{code}";

        var isReferred = profile.ReferredBySalesManagerUserId is not null;
        var year1 = isReferred
            ? settings.ReferredYear1DirectCommissionRate
            : settings.DirectCommissionRate;
        var year2 = settings.Year2DirectCommissionRate;
        var year3 = settings.Year3DirectCommissionRate;

        var emailBody = SalesMaterialsCopy.FillLink(SalesMaterialsCopy.EmailBodyTemplate, shortUrl);
        var whatsApp = SalesMaterialsCopy.FillLink(SalesMaterialsCopy.WhatsAppTemplate, shortUrl);
        var pitchCost = SalesMaterialsCopy.PitchCost(quote.MinPricePerToken);

        var packages = await _db.SalesPackages.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new SalesPackageQuoteRow(p.Name, p.TokenAmount, p.PriceEuro, p.Category.ToString()))
            .ToListAsync(cancellationToken);

        var displayName = string.IsNullOrWhiteSpace(profile.UserFullName)
            ? "Salesmanager"
            : profile.UserFullName.Trim();
        var company = string.IsNullOrWhiteSpace(profile.CompanyName)
            ? displayName
            : profile.CompanyName.Trim();

        return new SalesLinkToolkitDto(
            code,
            shortDisplay,
            shortUrl,
            qrUrl,
            previewUrl,
            settings.AttributionCookieDays > 0 ? settings.AttributionCookieDays : 30,
            settings.StartHighlightBonusTokens,
            year1,
            year2,
            year3,
            isReferred,
            SalesMaterialsCopy.EmailSubject,
            emailBody,
            whatsApp,
            pitchCost,
            displayName,
            company,
            profile.UserEmail ?? string.Empty,
            quote,
            packages);
    }

    private static string HostLabel(string baseUrl)
    {
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
            && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? uri.Host[4..]
                : uri.Host;
        }

        return "lobsy.nl";
    }
}
