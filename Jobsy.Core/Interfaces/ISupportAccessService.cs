using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public sealed record SupportAccessGrantDto(
    Guid Id,
    Guid AdminUserId,
    Guid? SubjectUserId,
    Guid? SubjectCompanyId,
    SupportAccessScope Scope,
    string Reason,
    string? TicketReference,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    Guid? RevokedByUserId,
    bool IsActive);

public sealed record SupportAccessRequest(
    Guid? SubjectUserId,
    Guid? SubjectCompanyId,
    SupportAccessScope Scope,
    string Reason,
    string? TicketReference,
    int DurationMinutes);

public interface ISupportAccessService
{
    Task<SupportAccessGrantDto> RequestAsync(
        Guid adminUserId,
        SupportAccessRequest request,
        bool mfaVerifiedInSession,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveGrantAsync(
        Guid adminUserId,
        Guid? subjectUserId,
        Guid? subjectCompanyId,
        SupportAccessScope requiredScope,
        CancellationToken cancellationToken = default);

    /// <summary>Active grant id for logging, or null.</summary>
    Task<Guid?> FindActiveGrantIdAsync(
        Guid adminUserId,
        Guid? subjectUserId,
        Guid? subjectCompanyId,
        SupportAccessScope requiredScope,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        Guid grantId,
        Guid revokedByUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupportAccessGrantDto>> ListRecentAsync(
        int take = 50,
        bool activeOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// UTC dates when support accessed this subject's data (for optional PrivacyData note).
    /// </summary>
    Task<IReadOnlyList<DateTime>> ListAccessDatesForSubjectAsync(
        Guid subjectUserId,
        int take = 10,
        CancellationToken cancellationToken = default);
}
