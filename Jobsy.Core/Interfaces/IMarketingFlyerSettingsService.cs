namespace Jobsy.Core.Interfaces;

public interface IMarketingFlyerSettingsService
{
    Task<MarketingFlyerSnapshot> GetAsync(CancellationToken cancellationToken = default);

    Task<MarketingFlyerSnapshot> UpdateAsync(
        MarketingFlyerUpdate update,
        CancellationToken cancellationToken = default);

    Task<MarketingFlyerSnapshot> ResetToDefaultsAsync(CancellationToken cancellationToken = default);
}

public interface IMarketingFlyerPdfService
{
    Task<byte[]> RenderAsync(CancellationToken cancellationToken = default);
}

public sealed record MarketingFlyerSnapshot(
    string Headline,
    string Subheadline,
    string Intro,
    IReadOnlyList<string> BulletPoints,
    string PromoFreeText,
    string PromoDiscountText,
    string CtaTitle,
    string CtaBody,
    string QrCaption,
    string QrPath,
    string FooterNote,
    DateTime? UpdatedAtUtc);

public sealed record MarketingFlyerUpdate(
    string Headline,
    string Subheadline,
    string Intro,
    string BulletPoints,
    string PromoFreeText,
    string PromoDiscountText,
    string CtaTitle,
    string CtaBody,
    string QrCaption,
    string QrPath,
    string FooterNote);
