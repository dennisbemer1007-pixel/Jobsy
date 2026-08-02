using Jobsy.Core.Enums;

namespace Jobsy.Api.Models;

public record UpdateSalesCommercialSettingsRequest(
    decimal BaseTokenValueEuro,
    decimal HighlightCarouselTokens,
    decimal HighlightPulseTokens,
    int HighlightCarouselDays,
    decimal StartHighlightBonusTokens,
    decimal? DirectCommissionRate = null,
    decimal? IndirectCommissionRate = null,
    int? CommissionDurationDays = null);

public record UpdateVacancyTypeCostRequest(
    VacancyKind Kind,
    decimal CostTokens,
    bool IsActive);

public record UpsertSalesPackageRequest(
    Guid? Id,
    string Name,
    string? Code,
    SalesPackageCategory Category,
    int TokenAmount,
    decimal PriceEuro,
    string? Description,
    bool IsActive,
    int SortOrder);
