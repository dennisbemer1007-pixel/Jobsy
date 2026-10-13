using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface ICompanyRegistrationService
{
    Task<RegistrationSubmitResult> SubmitAsync(
        RegistrationSubmitRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a pending registration with the 6-digit e-mail OTP (primary activation path).
    /// Expired or locked registrations are deleted.
    /// </summary>
    Task<RegistrationActivationResult> ConfirmAsync(
        Guid registrationId,
        string verificationCode,
        CancellationToken cancellationToken = default);

    Task<RegistrationActivationResult> ActivateAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes pending registrations whose confirmation window has elapsed (AVG minimization).
    /// </summary>
    Task<int> PurgeExpiredUnconfirmedAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TakeoverInboxItem>> ListPendingTakeoversAsync(
        IReadOnlyCollection<Guid> accessibleCompanyIds,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<TakeoverDecisionResult> ApproveTakeoverAsync(
        Guid takeoverId,
        Guid actorUserId,
        UserRole actorRole,
        IReadOnlyCollection<Guid>? accessibleCompanyIds,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<TakeoverDecisionResult> RejectTakeoverAsync(
        Guid takeoverId,
        Guid actorUserId,
        IReadOnlyCollection<Guid>? accessibleCompanyIds,
        bool isAdmin,
        string? note = null,
        CancellationToken cancellationToken = default);

    /// <summary>Confirms the ownership-transfer letter code before admin approval (07.5).</summary>
    Task ConfirmOwnershipTransferLetterAsync(
        Guid takeoverId,
        string code,
        CancellationToken cancellationToken = default);
}

public sealed record RegistrationSubmitRequest(
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
    /// <summary>
    /// When true and the KVK API is unavailable, continue with manual establishment data
    /// and mark the account as KVK-verificatie in afwachting.
    /// </summary>
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
    /// <summary>Code from the first-click <c>lobsy_sales_ref</c> cookie (typed code still wins).</summary>
    string? CookieTrackingCode = null,
    /// <summary>Employer invite token from external vacancy apply flow (<c>extToken</c> query).</summary>
    string? ExternalVacancyInviteToken = null);

public sealed record RegistrationSubmitResult(
    Guid RegistrationId,
    CompanyRegistrationStatus Status,
    bool RequiresTakeover,
    string Message,
    string? ActivationUrl,
    DateTime? VerificationExpiresAt = null);

public sealed record RegistrationActivationResult(
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
    /// <summary>
    /// True when the token only confirmed the contact e-mail for a pending takeover
    /// (no user provisioned yet; owner must still approve).
    /// </summary>
    bool EmailVerifiedAwaitingTakeover = false,
    /// <summary>True when the one-time welcome ledger credit was granted.</summary>
    bool WelcomeTokenGranted = false,
    /// <summary>Inclusive free-publish end date when the promo is active at activation time.</summary>
    DateOnly? FreePublishUntil = null,
    /// <summary>When set, the Web UI should redirect here after sign-in (external vacancy onboarding).</summary>
    string? PostActivationWebPath = null);

public sealed record TakeoverInboxItem(
    Guid TakeoverId,
    Guid RegistrationId,
    Guid TargetCompanyId,
    string TargetCompanyName,
    string KvkEstablishmentId,
    string RequesterName,
    string RequesterEmail,
    RegistrationScope Scope,
    DateTime CreatedAt);

public sealed record TakeoverDecisionResult(
    Guid TakeoverId,
    TakeoverRequestStatus Status,
    string Message,
    Guid? OrganizationCompanyId,
    Guid? BranchCompanyId);
