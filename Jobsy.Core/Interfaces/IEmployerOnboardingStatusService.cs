using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public sealed record EmployerVisibilityRowDto(string Key, bool Visible);

public sealed record EmployerChecklistItemDto(
    string Key,
    bool Done,
    string? Href,
    string? Detail);

public sealed record EmployerVestigingSuggestionDto(
    string KvkEstablishmentId,
    string Name,
    string Address);

public sealed record EmployerOnboardingStatusDto(
    Guid RootCompanyId,
    string CompanyName,
    CompanyVerificationStatus VerificationStatus,
    CompanyVerificationMethod VerificationMethod,
    string? RejectionReason,
    bool ManualPending,
    DateTime? ManualReplyByUtc,
    bool LetterUnderway,
    Guid? ActiveLetterId,
    DateTime? LetterSentAtUtc,
    DateTime? LetterExpiresAtUtc,
    int LetterFailedAttempts,
    int LetterAttemptsRemaining,
    DateTime? LetterResendAvailableAtUtc,
    int LetterResendsRemaining,
    string? LetterAddressMasked,
    bool EmailAvailable,
    bool ShowUnverifiedBanner,
    bool ShowSuccessBanner,
    int LastAutoPublishedVacancyCount,
    DateTime? VerifiedAtUtc,
    bool IsBureau,
    bool LenderRegistrationPending,
    bool HasReadyVacancy,
    IReadOnlyList<EmployerChecklistItemDto> Checklist,
    IReadOnlyList<EmployerVisibilityRowDto> Visibility,
    IReadOnlyList<EmployerVestigingSuggestionDto> SuggestedVestigingen,
    bool ChecklistComplete);

public interface IEmployerOnboardingStatusService
{
    Task<EmployerOnboardingStatusDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IVestigingSuggestionService
{
    /// <summary>Scan verified Organization-scope roots for new free KVK vestigingen (budget-aware).</summary>
    Task<int> RefreshSuggestionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployerVestigingSuggestionDto>> ListOpenAsync(
        Guid rootCompanyId,
        CancellationToken cancellationToken = default);

    Task DismissAsync(
        Guid rootCompanyId,
        string kvkEstablishmentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AcceptAsync(
        Guid rootCompanyId,
        string kvkEstablishmentId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
