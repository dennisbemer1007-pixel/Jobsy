using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public sealed record PartnerSalesCatalogDto(
    decimal BaseTokenValueEuro,
    decimal HighlightCarouselTokens,
    decimal HighlightPulseTokens,
    int HighlightCarouselDays,
    decimal StartHighlightBonusTokens,
    IReadOnlyList<VacancyTypeCostDto> VacancyTypeCosts,
    IReadOnlyList<SalesPackageDto> Packages);

public sealed record VacancyTypeCostDto(
    string Kind,
    string Label,
    decimal CostTokens,
    decimal PriceEuro,
    bool IsActive);

public sealed record SalesPackageDto(
    Guid Id,
    string Name,
    string? Code,
    string Category,
    int TokenAmount,
    decimal PriceEuro,
    string? Description,
    bool IsActive,
    int SortOrder);

public sealed record SalesCommercialAdminDto(
    Guid SettingsId,
    decimal BaseTokenValueEuro,
    decimal HighlightCarouselTokens,
    decimal HighlightPulseTokens,
    int HighlightCarouselDays,
    decimal StartHighlightBonusTokens,
    DateTime UpdatedAtUtc,
    IReadOnlyList<VacancyTypeCostDto> VacancyTypeCosts,
    IReadOnlyList<SalesPackageDto> Packages,
    decimal DirectCommissionRate = 0.15m,
    decimal IndirectCommissionRate = 0.03m,
    int CommissionDurationDays = 365);

public interface ISalesCommercialService
{
    Task<SalesCommercialSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<PartnerSalesCatalogDto> GetPublicCatalogAsync(CancellationToken cancellationToken = default);

    Task<SalesCommercialAdminDto> GetAdminAsync(CancellationToken cancellationToken = default);

    Task<SalesCommercialSettings> UpdateSettingsAsync(
        decimal baseTokenValueEuro,
        decimal highlightCarouselTokens,
        decimal highlightPulseTokens,
        int highlightCarouselDays,
        decimal startHighlightBonusTokens,
        decimal? directCommissionRate = null,
        decimal? indirectCommissionRate = null,
        int? commissionDurationDays = null,
        CancellationToken cancellationToken = default);

    Task<VacancyTypeTokenCost> UpdateVacancyTypeCostAsync(
        VacancyKind kind,
        decimal costTokens,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<decimal> GetPublishCostTokensAsync(VacancyKind kind, CancellationToken cancellationToken = default);

    Task<decimal> GetHighlightCostTokensAsync(CancellationToken cancellationToken = default);

    Task<int> GetHighlightDaysAsync(CancellationToken cancellationToken = default);

    Task<SalesPackage> UpsertPackageAsync(SalesPackage package, CancellationToken cancellationToken = default);

    Task DeletePackageAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPartnerFlyerPdfService
{
    Task<byte[]> RenderAsync(string? trackingCode, CancellationToken cancellationToken = default);
}
