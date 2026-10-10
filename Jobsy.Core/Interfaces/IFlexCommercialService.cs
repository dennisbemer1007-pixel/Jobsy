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
    decimal DeepTestPriceCompetenceEuro,
    decimal DeepTestPriceCareerEuro,
    decimal DeepTestPriceValuesEuro,
    decimal DeepTestPriceCultureEuro,
    decimal AgencyAnnualPriceEuro,
    decimal ContactUnlockCostTokens,
    decimal AcceptCandidatePilotCostTokens,
    DateOnly? AcceptCandidatePilotEndsOn,
    decimal AcceptCandidateStandardCostTokens);

public sealed record FlexCommercialSettingsDto(
    decimal MarginPerHourEuro,
    string BackofficePartnerName,
    decimal DeepTestPriceCompetenceEuro,
    decimal DeepTestPriceCareerEuro,
    decimal DeepTestPriceValuesEuro,
    decimal DeepTestPriceCultureEuro,
    decimal AgencyAnnualPriceEuro,
    decimal ContactUnlockCostTokens,
    decimal AcceptCandidatePilotCostTokens,
    DateOnly? AcceptCandidatePilotEndsOn,
    decimal AcceptCandidateStandardCostTokens,
    DateTime UpdatedAtUtc)
{
    /// <summary>Legacy alias — competence price (kept for landing/DTO compatibility).</summary>
    public decimal DeepAnalysisPriceEuro => DeepTestPriceCompetenceEuro;
}

public sealed record AgencySubscriptionDto(
    Guid Id,
    Guid CompanyId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    bool IsActive,
    decimal AnnualPriceEuro,
    string? Note);
