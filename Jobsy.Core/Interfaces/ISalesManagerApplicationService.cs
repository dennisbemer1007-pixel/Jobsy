namespace Jobsy.Core.Interfaces;

public interface ISalesManagerApplicationService
{
    /// <summary>
    /// Active (recruiting) salesmanager submits a recommendation for a new salesmanager.
    /// </summary>
    Task<SalesManagerApplicationDto> SubmitAsync(
        Guid referrerSalesManagerUserId,
        string candidateEmail,
        string candidateFullName,
        string motivation,
        bool referrerConfirmedPermission,
        CancellationToken cancellationToken = default);

    Task<SalesRecommendOverviewDto> GetRecommendOverviewAsync(
        Guid referrerSalesManagerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesManagerApplicationDto>> ListMineAsync(
        Guid referrerSalesManagerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesManagerApplicationDto>> ListPendingAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesManagerApplicationDto>> ListAllAsync(
        CancellationToken cancellationToken = default);

    Task<SalesManagerApplicationDto> ApproveAsync(
        Guid applicationId,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    Task<SalesManagerApplicationDto> RejectAsync(
        Guid applicationId,
        Guid adminUserId,
        string? reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// One-time objection link from the recommended-person notice mail (D15).
    /// Clears PII immediately and marks the application Rejected with reason "Bezwaar".
    /// </summary>
    Task<bool> ObjectByTokenAsync(string plaintextToken, CancellationToken cancellationToken = default);
}

public sealed record SalesManagerApplicationDto(
    Guid Id,
    Guid ReferrerSalesManagerUserId,
    string ReferrerFullName,
    string ReferrerEmail,
    string ReferrerTrackingCode,
    string CandidateEmail,
    string CandidateFullName,
    string Motivation,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc,
    Guid? ProvisionedUserId,
    string? RejectionReason,
    DateTime? SubjectNotifiedAtUtc = null,
    DateTime? SubjectObjectedAtUtc = null,
    DateTime? PersonalDataClearedAtUtc = null,
    bool ReferrerConfirmedPermission = false,
    string StatusLabelKey = "");

public sealed record SalesRecommendOverviewDto(
    decimal IndirectRatePercent,
    decimal ReferredYear1RatePercent,
    decimal IndirectEarnedEuro,
    bool CanRecruit,
    bool IsOnboardingComplete,
    string? TrackingCode,
    IReadOnlyList<SalesRecommendListItemDto> Applications);

/// <summary>Portal list row — no candidate e-mail (shown only at submit time).</summary>
public sealed record SalesRecommendListItemDto(
    Guid Id,
    DateTime CreatedAtUtc,
    string DisplayName,
    string Status,
    string StatusLabelKey,
    string? RejectionReason,
    bool PersonalDataCleared);
