using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// The one place that changes company verification status and runs the post-verify pipeline
/// (welcome token, klaar → publish, discovery invalidate, e-mail).
/// </summary>
public interface ICompanyVerificationService
{
    Task MarkVerifiedAsync(
        Guid rootCompanyId,
        CompanyVerificationMethod method,
        Guid? actorUserId,
        string? note,
        CancellationToken cancellationToken = default);

    Task MarkPendingAsync(
        Guid rootCompanyId,
        CompanyVerificationMethod method,
        Guid? actorUserId,
        string? note,
        CancellationToken cancellationToken = default);

    Task MarkRejectedAsync(
        Guid rootCompanyId,
        CompanyVerificationMethod method,
        Guid? actorUserId,
        string reason,
        CancellationToken cancellationToken = default);
}
