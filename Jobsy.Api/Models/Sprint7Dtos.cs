using Jobsy.Core.Enums;

namespace Jobsy.Api.Models;

public record SubmitRegistrationRequest(
    string KvkNumber,
    string KvkEstablishmentId,
    RegistrationScope Scope,
    string ContactName,
    string ContactEmail,
    string? ContactPhone = null,
    bool AcceptedTerms = false,
    string? ConsentVersion = null,
    string? SalesManagerTrackingCode = null,
    string? Password = null,
    bool AllowPendingKvkVerification = false,
    string? ManualEstablishmentName = null,
    string? ManualEstablishmentAddress = null,
    string? ManualEstablishmentNumber = null,
    double? ManualLatitude = null,
    double? ManualLongitude = null,
    bool? ManualIsIntermediarySbi = null);

public record KvkEstablishmentsLookupResponse(
    string Status,
    string? Message,
    IReadOnlyList<Jobsy.Core.Interfaces.KvkEstablishmentResult> Establishments);

public record RegistrationSubmitResponse(
    Guid RegistrationId,
    string Status,
    bool RequiresTakeover,
    string Message,
    string? ActivationUrl,
    DateTime? VerificationExpiresAt = null);

public record ConfirmRegistrationRequest(string VerificationCode);

public record RegistrationActivationResponse(
    Guid RegistrationId,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    IReadOnlyList<Guid> CompanyIds,
    string? TemporaryPassword,
    Guid? OrganizationCompanyId,
    Guid? BranchCompanyId,
    bool UsedChosenPassword = false,
    bool EmailVerifiedAwaitingTakeover = false,
    bool WelcomeTokenGranted = false,
    DateOnly? FreePublishUntil = null);

public record TakeoverInboxItemDto(
    Guid TakeoverId,
    Guid RegistrationId,
    Guid TargetCompanyId,
    string TargetCompanyName,
    string KvkEstablishmentId,
    string RequesterName,
    string RequesterEmail,
    string Scope,
    DateTime CreatedAt);

public record TakeoverDecisionResponse(
    Guid TakeoverId,
    string Status,
    string Message,
    Guid? OrganizationCompanyId,
    Guid? BranchCompanyId);

public record RejectTakeoverRequest(string? Note = null);

public record LocalLoginRequest(string Email, string Password);

public record LocalLoginResponse(
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    IReadOnlyList<Guid> CompanyIds,
    bool ShowCandidateHowTo = false,
    bool HasCandidateApplications = false,
    bool HasSalesReferral = false,
    /// <summary>HMAC session proof for Production DevelopmentAuth (non-demo emails).</summary>
    string? SessionToken = null);

public record EnsureExternalUserRequest(
    string Email,
    string? FullName,
    /// <summary>IdP key: <c>entra</c> or <c>google</c>.</summary>
    string? Provider = null,
    /// <summary>Stable subject (Entra OID / OIDC sub).</summary>
    string? ProviderSubject = null,
    /// <summary>Optional Ambassadeur tracking code (AM-…) for new candidates.</summary>
    string? ReferralCode = null);

public record EnsureExternalUserResponse(
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    IReadOnlyList<Guid> CompanyIds,
    bool IsNewUser,
    bool ShowCandidateHowTo,
    bool HasCandidateApplications,
    bool HasSalesReferral = false,
    string? SessionToken = null);

public record ExternalProvidersStatusResponse(bool Entra, bool Google);

public record ExternalProviderConfigResponse(
    string Provider,
    string ClientId,
    string ClientSecret,
    string? TenantId);
