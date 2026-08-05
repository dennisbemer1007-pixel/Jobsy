using Jobsy.Core.Enums;

namespace Jobsy.Api.Models;

public record AdminCompanyDetailDto(
    Guid Id,
    string Name,
    string KvkNumber,
    string Address,
    string? LogoUrl,
    string Type,
    Guid? ParentCompanyId,
    int UserCount,
    int ActiveVacancyCount,
    int TotalVacancyCount,
    int ApplicationCount,
    decimal TokenBalance,
    Guid? SalesManagerUserId = null,
    string? SalesManagerName = null);

public record AdminUserDetailDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    string? CompanyName,
    string? CompanyType,
    bool IsEarlyAdapter,
    bool IsActive,
    IReadOnlyList<Guid> MembershipCompanyIds);

public record AdminVacancyDetailDto(
    Guid Id,
    string Title,
    string Status,
    Guid CompanyId,
    string CompanyName,
    string CompanyType,
    bool IsHighlighted,
    int ExtensionCount,
    DateOnly StartDate,
    DateOnly EndDate,
    int ImpressionCount,
    int ClickCount,
    int ShareCount,
    int ApplicationCount,
    int LikeCount,
    bool IsExtended,
    string CreatedVia = "Manual");

public record RegisterAdminCompanyRequest(
    string KvkNumber,
    string KvkEstablishmentId,
    CompanyType Type = CompanyType.Employer,
    Guid? ParentCompanyId = null);

public record UpdateTokenPackRequest(Guid Id, decimal PriceEuro, bool IsActive);

public record UpdateTokenSpendCostRequest(Guid Id, decimal CostTokens, bool IsActive);

public record UpdatePushBomSettingsRequest(double RadiusKm, int MaxTravelMinutes);

public record UpsertPushBomPricingTierRequest(
    Guid? Id,
    int MinCandidates,
    int? MaxCandidates,
    decimal CostTokens,
    bool IsActive);

public record UpsertEarlyAdapterRuleRequest(
    Guid? Id,
    string Name,
    int MonthlyGrantTokens,
    decimal PurchaseDiscountPercent,
    bool IsActive);

public record UpdateIntegrationCredentialRequest(
    string? ApiKey = null,
    string? Model = null,
    string? ClientId = null,
    string? ClientSecret = null,
    string? TenantId = null,
    string? BaseUrl = null,
    string? FromAddress = null,
    bool ClearApiKey = false,
    bool ClearClientSecret = false);

public record IntegrationCredentialDto(
    string Key,
    string DisplayName,
    string Description,
    bool HasApiKey,
    string? ApiKeyMasked,
    bool HasClientSecret,
    string? ClientSecretMasked,
    string? ClientId,
    string? TenantId,
    string? Model,
    string? BaseUrl,
    string? FromAddress,
    bool SupportsApiKey,
    bool SupportsModel,
    bool SupportsOAuth,
    bool SupportsTenantId,
    bool SupportsBaseUrl,
    bool SupportsFromAddress,
    bool? LastPingOk,
    string? LastPingMessage,
    DateTime? LastPingAtUtc,
    DateTime? UpdatedAtUtc);

public record UpdatePlatformFeatureRequest(
    bool VacancyContentModerationEnabled,
    bool AuthenticatorEnabled,
    bool ExposeRegistrationActivationLinks,
    string? PublicWebBaseUrl,
    int InactiveCompanyDays = 120,
    int SessionInactivityTimeoutMinutes = 30,
    DateOnly? FreePublishUntil = null,
    bool ClearFreePublishUntil = false);

public record PlatformFeatureDto(
    bool VacancyContentModerationEnabled,
    bool AuthenticatorEnabled,
    bool ExposeRegistrationActivationLinks,
    string PublicWebBaseUrl,
    DateTime? UpdatedAtUtc,
    int InactiveCompanyDays = 120,
    int SessionInactivityTimeoutMinutes = 30,
    DateOnly? FreePublishUntil = null);

public record SessionSecurityDto(int InactivityTimeoutMinutes);

public record FreePublishStatusDto(bool IsActive, DateOnly? FreePublishUntil);

public record SemiAnnualWageUpdateResultDto(
    DateOnly EffectiveFrom,
    int RatesUpdated,
    string Message);
