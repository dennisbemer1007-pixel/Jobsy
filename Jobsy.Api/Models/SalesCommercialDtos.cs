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
    int? CommissionDurationDays = null,
    decimal? PartnerCommissionRate = null,
    decimal? Year2DirectCommissionRate = null,
    decimal? Year3DirectCommissionRate = null,
    decimal? ReferredYear1DirectCommissionRate = null,
    int? CommissionHoldDays = null,
    decimal? PayoutMinimumEuro = null,
    int? IbanChangeHoldDays = null,
    int? AttributionCookieDays = null);

public record SalesReferralVisitRequest(
    string Code,
    string? Channel = null,
    bool CountClick = true);

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
