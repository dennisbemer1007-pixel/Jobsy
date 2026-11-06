using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public sealed record CompanyVerificationOptionsView(
    Guid CompanyId,
    string CompanyName,
    string KvkNumber,
    CompanyVerificationStatus Status,
    CompanyVerificationMethod Method,
    bool EmailAvailable,
    IReadOnlyList<string> WebsiteDomains,
    string? SuggestedEmail,
    bool LetterAvailable,
    string? LetterAddressMasked,
    string? LetterUnavailableReason,
    bool ManualPending,
    Guid? ActiveLetterId,
    DateTime? LetterSentAtUtc,
    DateTime? LetterExpiresAtUtc,
    int LetterResendsRemaining,
    DateTime? LetterResendAvailableAtUtc);

public sealed record EmailVerificationStartResult(
    bool Ok,
    string? ErrorCode,
    string? Message,
    DateTime? ExpiresAtUtc,
    string? MaskedEmail);

public sealed record LetterVerificationStartResult(
    bool Ok,
    string? ErrorCode,
    string? Message,
    Guid? LetterId,
    string? AddressMasked,
    DateTime? ExpiresAtUtc,
    DateTime? SentAtUtc,
    DateTime? ResendAvailableAtUtc,
    int ResendsRemaining);

public sealed record VerificationConfirmResult(
    bool Ok,
    string? ErrorCode,
    string? Message,
    CompanyVerificationStatus? Status);

public sealed record ManualVerificationStartResult(
    bool Ok,
    string? ErrorCode,
    string? Message);

/// <summary>Employer-facing verification flows (business e-mail, letter, manual).</summary>
public interface ICompanyVerificationFlowService
{
    Task<CompanyVerificationOptionsView> GetOptionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationStartResult> StartEmailAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default);

    Task<VerificationConfirmResult> ConfirmEmailAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default);

    Task<LetterVerificationStartResult> RequestLetterAsync(
        Guid userId,
        bool isResend,
        CancellationToken cancellationToken = default);

    Task<VerificationConfirmResult> ConfirmLetterAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default);

    Task<ManualVerificationStartResult> RequestManualAsync(
        Guid userId,
        string reason,
        string? message,
        IReadOnlyList<string>? attachmentIds,
        CancellationToken cancellationToken = default);

    /// <summary>Admin override: send a letter even over the per-KvK limit.</summary>
    Task<LetterVerificationStartResult> AdminSendLetterAsync(
        Guid companyId,
        Guid adminUserId,
        CancellationToken cancellationToken = default);
}

public sealed record AdminVerificationQueueItem(
    Guid CompanyId,
    string CompanyName,
    string KvkNumber,
    string Tab,
    CompanyVerificationStatus Status,
    string? Reason,
    string RequesterName,
    string? RequesterEmail,
    string? RequesterFunction,
    DateTime CreatedAtUtc,
    IReadOnlyList<string> Heuristics,
    int PriorRejections,
    string? KvkAddress,
    IReadOnlyList<string> Websites,
    IReadOnlyList<string> SbiCodes,
    Guid? OpenManualRequestId,
    Guid? BlockedLetterId,
    bool StubLetterAvailable);

public interface ICompanyVerificationAdminService
{
    Task<IReadOnlyList<AdminVerificationQueueItem>> ListQueueAsync(
        string? tab,
        CancellationToken cancellationToken = default);

    Task<int> CountOpenAsync(CancellationToken cancellationToken = default);

    Task ApproveAsync(
        Guid companyId,
        Guid adminUserId,
        string? note,
        CancellationToken cancellationToken = default);

    Task RejectAsync(
        Guid companyId,
        Guid adminUserId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<byte[]?> GetStubLetterPdfAsync(
        Guid letterId,
        CancellationToken cancellationToken = default);
}
