using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ITalentPoolService
{
    Task<IReadOnlyList<AnonymousTalentCardDto>> SearchAsync(
        Guid companyId,
        TalentPoolSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<TalentContactRequestDto> UnlockAsync(
        Guid companyId,
        Guid employerUserId,
        Guid candidateUserId,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Candidate answer. Accepting shares the PII; declining stores why (D14).
    /// </summary>
    Task<TalentContactRequestDto> CandidateRespondAsync(
        Guid candidateUserId,
        Guid requestId,
        bool accept,
        bool alreadyPlaced,
        CancellationToken cancellationToken = default);

    Task<TalentContactRequestDto> WithdrawAndRefundAsync(
        Guid companyId,
        Guid employerUserId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TalentContactRequestDto>> ListForEmployerAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TalentContactRequestDto>> ListForCandidateAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exactly the fields the employer receives when the candidate accepts (04 §1).
    /// Null when the request does not exist or belongs to another candidate.
    /// </summary>
    Task<TalentContactSharePreviewDto?> GetSharePreviewAsync(
        Guid candidateUserId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    /// <summary>Marks pending requests past the 48h window as RefundEligible.</summary>
    Task<int> MarkExpiredAsRefundEligibleAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Search filters for the anonymous talent pool. Age filters are intentionally absent.
/// </summary>
public sealed record TalentPoolSearchQuery(
    IReadOnlyList<string>? Tags = null,
    int? MaxTravelMinutes = null,
    TransportMode Transport = TransportMode.Bike,
    string? AvailabilityHint = null,
    string? DrivingLicense = null,
    int Take = 50);

/// <summary>
/// Anonymous talent card — no name, email, phone, date of birth, scores,
/// match percentages, rankings, or AI output (AI Act).
/// </summary>
public sealed record AnonymousTalentCardDto(
    Guid CandidateUserId,
    IReadOnlyList<string> MatchTags,
    IReadOnlyList<string> RiasecTags,
    string? HollandCode,
    string? AvailabilitySummary,
    IReadOnlyList<string> DrivingLicenses,
    int? TravelMinutes,
    string? RegionLabel);

public sealed record TalentContactRequestDto(
    Guid Id,
    Guid CompanyId,
    Guid CandidateUserId,
    TalentContactStatus Status,
    string Message,
    DateTime CreatedAtUtc,
    DateTime RespondByUtc,
    DateTime? RespondedAtUtc,
    DateTime? ContactSharedAtUtc,
    bool PiiRevealed,
    string? CandidateFullName,
    string? CandidateEmail,
    string? CandidatePhone,
    string? CompanyName = null,
    /// <summary>D14: <c>NotInterested</c> or <c>AlreadyPlaced</c>; null when not declined.</summary>
    string? CandidateDeclineReason = null);

/// <summary>
/// What the employer gets when the candidate says yes — shown in the share-confirm dialog
/// before anything is sent. Missing phone stays null ("niet ingevuld" in the dialog).
/// </summary>
public sealed record TalentContactSharePreviewDto(
    string? CompanyName,
    string? Name,
    string? Email,
    string? Phone);
