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
    string? TemporaryPassword);
