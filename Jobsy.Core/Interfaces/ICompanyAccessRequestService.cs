using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface ICompanyAccessRequestService
{
    Task<AccessRequestSubmitResult> SubmitAsync(
        AccessRequestSubmitRequest request,
        CancellationToken cancellationToken = default);

    Task<AccessRequestConfirmResult> ConfirmEmailAsync(
        Guid requestId,
        string code,
        CancellationToken cancellationToken = default);

    Task WithdrawAsync(
        Guid requestId,
        string requesterToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccessRequestInboxItem>> ListInboxAsync(
        IReadOnlyCollection<Guid> accessibleCompanyIds,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<AccessRequestDecisionResult> ApproveAsync(
        Guid requestId,
        Guid actorUserId,
        UserRole actorRole,
        IReadOnlyCollection<Guid>? accessibleCompanyIds,
        bool isAdmin,
        UserRole? grantedRole,
        IReadOnlyList<Guid>? grantedCompanyIds,
        CancellationToken cancellationToken = default);

    Task<AccessRequestDecisionResult> RejectAsync(
        Guid requestId,
        Guid actorUserId,
        IReadOnlyCollection<Guid>? accessibleCompanyIds,
        bool isAdmin,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccessRequestAdminItem>> ListEscalatedForAdminAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OwnershipTransferAdminItem>> ListOwnershipTransfersForAdminAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Reminder day 3 / escalate day 5 / expire day 30. Returns counts.</summary>
    Task<(int Reminders, int Escalations, int Expiries)> ProcessEscalationsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}

public sealed record AccessRequestSubmitRequest(
    string KvkNumber,
    string? KvkEstablishmentId,
    IReadOnlyList<Guid> RequestedCompanyIds,
    UserRole RequestedRole,
    string RequesterName,
    string? RequesterFunction,
    string RequesterEmail,
    string? RequesterPhone,
    string? Message,
    Guid? TargetCompanyId = null);

public sealed record AccessRequestSubmitResult(
    Guid RequestId,
    CompanyAccessRequestStatus Status,
    string Message,
    DateTime? CodeExpiresAtUtc);

public sealed record AccessRequestConfirmResult(
    Guid RequestId,
    CompanyAccessRequestStatus Status,
    string Message,
    string CompanyName);

public sealed record AccessRequestInboxItem(
    Guid RequestId,
    Guid TargetCompanyId,
    string TargetCompanyName,
    string RequesterName,
    string? RequesterFunction,
    string RequesterEmail,
    UserRole RequestedRole,
    IReadOnlyList<Guid> RequestedCompanyIds,
    string? Message,
    CompanyAccessRequestStatus Status,
    DateTime CreatedAtUtc,
    int AgeWorkingDays);

public sealed record AccessRequestDecisionResult(
    Guid RequestId,
    CompanyAccessRequestStatus Status,
    string Message,
    Guid? CreatedUserId);

public sealed record AccessRequestAdminItem(
    Guid RequestId,
    Guid TargetCompanyId,
    string TargetCompanyName,
    string KvkNumber,
    string RequesterName,
    string RequesterEmail,
    string? RequesterPhone,
    UserRole RequestedRole,
    string? Message,
    DateTime CreatedAtUtc,
    DateTime? EscalatedAtUtc,
    IReadOnlyList<AccessRequestManagerContact> ManagerContacts);

public sealed record AccessRequestManagerContact(
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    UserRole Role);

public sealed record OwnershipTransferAdminItem(
    Guid TakeoverId,
    Guid TargetCompanyId,
    string TargetCompanyName,
    string KvkNumber,
    string RequesterName,
    string RequesterEmail,
    bool LetterVerified,
    DateTime CreatedAt,
    DateTime? LetterVerifiedAtUtc);
