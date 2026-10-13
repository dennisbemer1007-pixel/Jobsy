using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface IProactiveMatchingService
{
    Task<ProactiveMatchingSettingsDto?> GetSettingsAsync(Guid candidateUserId, CancellationToken cancellationToken = default);

    Task<ProactiveMatchingSettingsDto> UpsertSettingsAsync(
        Guid candidateUserId,
        ProactiveMatchingSettingsUpdate update,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProactiveMatchSuggestionDto>> ListSuggestionsAsync(
        Guid candidateUserId,
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<ProactiveMatchSuggestionDto?> SubmitFeedbackAsync(
        Guid candidateUserId,
        Guid suggestionId,
        ProactiveMatchFeedback feedback,
        CancellationToken cancellationToken = default);

    /// <summary>Daily batch: score and notify eligible candidates.</summary>
    Task<int> RunDailyMatchingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Score one newly active vacancy against eligible proactive candidates and notify on strong matches.
    /// Respects weekly caps and delivery history (same rules as the daily batch).
    /// </summary>
    Task<int> RunInstantMatchingForVacancyAsync(Guid vacancyId, CancellationToken cancellationToken = default);
}

public interface IProactiveVacancyShortlistService
{
    Task<IReadOnlyList<ProactiveVacancyShortlistEntryDto>> BuildShortlistAsync(
        Guid companyId,
        Guid vacancyId,
        CancellationToken cancellationToken = default);
}

public sealed record ProactiveMatchingSettingsDto(
    bool Enabled,
    ProactiveMatchingFrequency Frequency,
    DateTime? PausedUntilUtc,
    bool HasConsent,
    DateTime? ConsentAcceptedAtUtc,
    int MinScorePercent,
    int MaxNotificationsPerWeek,
    int SameVacancyCooldownDays);

public readonly record struct ProactiveDeliveryHistoryEntry(
    string Title,
    string? CompanyLabel,
    DateTime DeliveredAtUtc,
    ProactiveMatchFeedback? Feedback);

public sealed record ProactiveMatchingSettingsUpdate(
    bool? Enabled = null,
    ProactiveMatchingFrequency? Frequency = null,
    DateTime? PausedUntilUtc = null,
    bool? ClearPause = null,
    bool? AcceptConsent = null);

public sealed record ProactiveMatchSuggestionDto(
    Guid Id,
    Guid? VacancyId,
    Guid? ExternalVacancyId,
    int MatchPercent,
    string Rationale,
    string Title,
    string? CompanyLabel,
    DateTime DeliveredAtUtc,
    ProactiveMatchFeedback? Feedback,
    int SimilarVacancyCount = 1,
    IReadOnlyList<string>? MatchHighlights = null);

public sealed record ProactiveVacancyShortlistEntryDto(
    int Rank,
    Guid CandidateUserId,
    int MatchPercent,
    IReadOnlyList<string> MatchTags,
    IReadOnlyList<string> TopStrengths,
    string? AvailabilitySummary,
    int? TravelMinutes,
    string? RegionLabel);
