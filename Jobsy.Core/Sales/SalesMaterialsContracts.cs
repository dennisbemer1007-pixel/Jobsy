namespace Jobsy.Core.Sales;

public enum SalesMaterialKind
{
    Flyer = 0,
    Visitekaartje = 1,
    Prijskaart = 2,
    Presentatie = 3
}

public sealed record SalesLinkToolkitDto(
    string TrackingCode,
    string ShortLinkDisplay,
    string ShortLinkUrl,
    string QrUrl,
    string PartnerPreviewUrl,
    int AttributionCookieDays,
    decimal StartHighlightBonusTokens,
    decimal Year1Rate,
    decimal Year2Rate,
    decimal Year3Rate,
    bool IsReferredSalesManager,
    string EmailSubject,
    string EmailBody,
    string WhatsAppText,
    string PitchCostLine,
    string DisplayName,
    string CompanyName,
    string AccountEmail,
    SalesPriceQuote Quote,
    IReadOnlyList<SalesPackageQuoteRow> SalesPackages);

public sealed record SalesPackageQuoteRow(
    string Name,
    int TokenAmount,
    decimal PriceEuro,
    string Category);

public interface ISalesMaterialsPdfService
{
    Task<byte[]> FlyerA4Async(string trackingCode, CancellationToken cancellationToken = default);

    Task<byte[]> BusinessCardsAsync(string trackingCode, CancellationToken cancellationToken = default);

    Task<byte[]> PriceCardAsync(string trackingCode, CancellationToken cancellationToken = default);

    Task<byte[]> PresentationAsync(
        string trackingCode,
        string salesManagerName,
        string companyName,
        string accountEmail,
        CancellationToken cancellationToken = default);
}

public interface ISalesLinkToolkitService
{
    Task<SalesLinkToolkitDto?> GetAsync(Guid beneficiaryUserId, CancellationToken cancellationToken = default);
}
