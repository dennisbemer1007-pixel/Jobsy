using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface ICompanyRegistrationService
{
    Task<RegistrationSubmitResult> SubmitAsync(
        RegistrationSubmitRequest request,
        CancellationToken cancellationToken = default);

    Task<RegistrationActivationResult> ActivateAsync(
        string token,
        CancellationToken cancellationToken = default);

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
}

public sealed record RegistrationSubmitRequest(
    string KvkNumber,
    string KvkEstablishmentId,
    RegistrationScope Scope,
    string ContactName,
    string ContactEmail,
    string? ContactPhone,
    bool AcceptedTerms = false,
    string? ConsentVersion = null);

public sealed record RegistrationSubmitResult(
    Guid RegistrationId,
    CompanyRegistrationStatus Status,
    bool RequiresTakeover,
    string Message,
    string? ActivationUrl);

public sealed record RegistrationActivationResult(
    Guid RegistrationId,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    IReadOnlyList<Guid> CompanyIds,
    /// <summary>One-time temporary password shown only at activation (never stored plaintext).</summary>
    string TemporaryPassword,
    Guid? OrganizationCompanyId,
    Guid? BranchCompanyId);

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
