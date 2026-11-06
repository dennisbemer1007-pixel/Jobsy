using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

/// <summary>What a visitor sent from <c>/melden</c>. The target is always resolved server-side.</summary>
public sealed record ContentReportSubmission(
    string? TargetType,
    string? TargetRef,
    ContentReportReason Reason,
    string? Details,
    string? ReporterEmail,
    Guid? ReporterUserId = null,
    string? Language = null);

/// <summary>
/// Always "accepted" for the visitor — an unknown or non-public target gets the same answer so the
/// form cannot be used to probe which vacancies exist.
/// </summary>
public sealed record ContentReportSubmitResult(bool EmailConfirmationSent, bool Duplicate);

public sealed record ContentReportListItem(
    Guid Id,
    ContentReportTargetType TargetType,
    Guid TargetId,
    string? TargetKvk,
    string? TargetLabel,
    ContentReportReason Reason,
    string? Details,
    /// <summary>Masked with <c>PersonalDataMasker</c>; the clear address never leaves the server.</summary>
    string? ReporterEmailMasked,
    DateTime CreatedAtUtc,
    ContentReportStatus Status,
    string? DecisionReason,
    DateTime? DecidedAtUtc,
    int TargetReportCount);

public sealed record ContentReportDecisionRequest(
    ContentReportTargetType TargetType,
    Guid TargetId,
    ContentReportStatus Decision,
    string? Reason,
    Guid? ActorUserId = null,
    string? ActorRole = null);

public sealed record ContentReportDecisionResult(
    bool Succeeded,
    string? ErrorCode,
    int ClosedReportCount);

public interface IContentReportService
{
    Task<ContentReportSubmitResult> SubmitAsync(
        ContentReportSubmission submission,
        CancellationToken cancellationToken = default);

    /// <summary>Open reports first, then newest. <paramref name="openOnly"/> null = everything.</summary>
    Task<IReadOnlyList<ContentReportListItem>> ListAsync(
        bool? openOnly,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContentReportListItem>> ListForTargetAsync(
        ContentReportTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);

    Task<ContentReportDecisionResult> DecideAsync(
        ContentReportDecisionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Name of the reported page for the <c>/melden</c> heading, or null when the target is not
    /// public. The form then falls back to a generic label instead of showing an error.
    /// </summary>
    Task<string?> DescribeTargetAsync(
        string? targetType,
        string? targetRef,
        CancellationToken cancellationToken = default);
}
