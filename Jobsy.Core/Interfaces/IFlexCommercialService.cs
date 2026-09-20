namespace Jobsy.Core.Interfaces;

public interface IFlexCommercialService
{
    Task<FlexCommercialSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<FlexCommercialSettingsDto> UpdateAsync(
        FlexCommercialSettingsUpdate update,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveAgencySubscriptionAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<AgencySubscriptionDto?> GetAgencySubscriptionAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<AgencySubscriptionDto> ActivateAgencySubscriptionAsync(
        Guid companyId,
        DateTime? startsAtUtc = null,
        string? note = null,
        CancellationToken cancellationToken = default);
}

public sealed record FlexCommercialSettingsUpdate(
    decimal MarginPerHourEuro,
    string BackofficePartnerName,
    decimal DeepAnalysisPriceEuro,
    decimal AgencyAnnualPriceEuro,
    decimal ContactUnlockCostTokens);

public sealed record FlexCommercialSettingsDto(
    decimal MarginPerHourEuro,
    string BackofficePartnerName,
    decimal DeepAnalysisPriceEuro,
    decimal AgencyAnnualPriceEuro,
    decimal ContactUnlockCostTokens,
    DateTime UpdatedAtUtc);

public sealed record AgencySubscriptionDto(
    Guid Id,
    Guid CompanyId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    bool IsActive,
    decimal AnnualPriceEuro,
    string? Note);
