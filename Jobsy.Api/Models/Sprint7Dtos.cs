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
    string? PartnerTrackingCode = null,
    string? Password = null,
    bool AllowPendingKvkVerification = false,
    string? ManualEstablishmentName = null,
    string? ManualEstablishmentAddress = null,
    string? ManualEstablishmentNumber = null,
    double? ManualLatitude = null,
    double? ManualLongitude = null,
    bool? ManualIsIntermediarySbi = null,
    IReadOnlyList<string>? SelectedEstablishmentIds = null,
    Guid? SalesManagerUserId = null,
    DateTime? RepresentationConsentAtUtc = null,
    string? RepresentationConsentVersion = null,
    string? PreferredLoginProvider = null,
    bool LocationUnknown = false,
    bool AcceptedRepresentation = false,
    string? CookieTrackingCode = null);

public record KvkEstablishmentsLookupResponse(
    string Status,
    string? Message,
    IReadOnlyList<Jobsy.Core.Interfaces.KvkEstablishmentResult> Establishments);

public record KvkAddressLineDto(
    string Street,
    string HouseNumber,
    string? HouseLetter,
    string Postcode,
    string Place,
    string FormattedLine);

public record KvkEstablishmentProfileDto(
    string KvkNumber,
    string EstablishmentNumber,
    string KvkEstablishmentId,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    bool IsInUse,
    IReadOnlyList<string> SbiCodes,
    KvkAddressLineDto? VisitingAddress,
    KvkAddressLineDto? PostalAddress);

public record KvkCompanyProfileResponse(
    string Status,
    string KvkNumber,
    string Name,
    string Address,
    string? LegalForm,
    IReadOnlyList<string> SbiCodes,
    IReadOnlyList<string> Websites,
    IReadOnlyList<KvkEstablishmentProfileDto> Establishments,
    string? Message = null);

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
    Guid? OrganizationCompanyId,
    Guid? BranchCompanyId,
    bool UsedChosenPassword = false,
    bool EmailVerifiedAwaitingTakeover = false,
    bool WelcomeTokenGranted = false,
    DateOnly? FreePublishUntil = null,
    string? SessionToken = null,
    bool InstantlyVerified = false,
    string? PreferredLoginProvider = null);

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

public record SessionLoginRequest(string SessionToken);

public record LocalLoginRequest(string Email, string Password, bool RememberDevice = true);

public record LocalLoginResponse(
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    IReadOnlyList<Guid> CompanyIds,
    bool ShowCandidateHowTo = false,
    bool HasCandidateApplications = false,
    bool HasSalesReferral = false,
    /// <summary>Legacy HMAC (unused for API auth; kept for transitional clients).</summary>
    string? SessionToken = null,
    int SessionVersion = 0,
    Guid? DeviceSessionId = null,
    string? DeviceRefreshToken = null,
    DateTime? DeviceExpiresAtUtc = null,
    Guid? UserId = null,
    bool RequiresMfa = false,
    bool MfaEnrolled = false,
    string? MfaChallengeToken = null,
    bool MfaVerified = false,
    IReadOnlyList<string>? RecoveryCodes = null,
    Guid? SchoolId = null,
    int? RecoveryCodesLeft = null,
    bool UsedRecoveryCode = false);

public record MfaEnrollmentRequest(string ChallengeToken);

public record MfaEnrollmentResponse(string Secret, string ProvisioningUri, string QrSvgDataUri);

public record MfaStateRequest(string ChallengeToken);

public record MfaStateResponse(bool Enrolled, string Email);

public record MfaVerifyRequest(
    string ChallengeToken,
    string? Code = null,
    string? RecoveryCode = null);

public record AdminMfaResetRequest(string Reason, string? ConfirmCode = null);

public record EnsureExternalUserRequest(
    string Email,
    string? FullName,
    /// <summary>IdP key: <c>entra</c> or <c>google</c>.</summary>
    string? Provider = null,
    /// <summary>Stable subject (Entra OID / OIDC sub).</summary>
    string? ProviderSubject = null,
    /// <summary>Entra tenant id (<c>tid</c>); null for Google.</summary>
    string? ProviderTenantId = null,
    /// <summary>Optional Ambassadeur tracking code (AM-…) for new candidates.</summary>
    string? ReferralCode = null,
    bool RememberDevice = true,
    string? ReturnUrl = null,
    string? UserAgent = null);

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
    string? SessionToken = null,
    int SessionVersion = 0,
    /// <summary>One-time code for in-scope PWA cookie exchange (external login).</summary>
    string? HandoffCode = null,
    Guid? UserId = null,
    bool RequiresMfa = false,
    bool MfaEnrolled = false,
    string? MfaChallengeToken = null,
    string? AuthMethod = null);

public record ExternalProvidersStatusResponse(bool Entra, bool Google);

public record ExternalProviderConfigResponse(
    string Provider,
    string ClientId,
    string ClientSecret,
    string? TenantId);

public record EmailCodeStartRequest(
    string Email,
    string? FirstName = null,
    string? ReferralCode = null,
    string? ReturnUrl = null,
    string? Culture = null);

public record EmailCodeStartResponse(Guid ChallengeId);

public record EmailCodeVerifyRequest(
    Guid ChallengeId,
    string Code,
    bool RememberDevice = true,
    string? UserAgent = null);
