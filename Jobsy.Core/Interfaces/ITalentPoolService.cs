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

/// <summary>Anonymous talent card — no name, email, phone, or date of birth.</summary>
public sealed record AnonymousTalentCardDto(
    Guid CandidateUserId,
    IReadOnlyList<string> MatchTags,
    IReadOnlyList<string> RiasecTags,
    CompetencyScores? CompetencyScores,
    RiasecScores? CareerScores,
    string? HollandCode,
    string? AvailabilitySummary,
    IReadOnlyList<string> DrivingLicenses,
    int? TravelMinutes,
    string? RegionLabel,
    bool CompetenceDeepCompleted,
    bool CareerDeepCompleted);

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
    string? CompanyName = null);
