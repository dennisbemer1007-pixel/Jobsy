using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateOnboardingService
{
    Task<CandidateOnboardingStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CandidateOnboardingStateDto> SaveProgressAsync(
        Guid userId,
        CandidateOnboardingProgressRequest request,
        CancellationToken cancellationToken = default);

    Task<CandidateOnboardingStateDto> CompleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// True when the candidate should be redirected to <c>/candidate/start</c>
    /// (how-to not completed and profile not already complete).
    /// </summary>
    Task<bool> ShouldShowWizardAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record CandidateOnboardingStateDto(
    int CurrentStep,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    bool IsComplete,
    bool ShouldShow,
    string? Source,
    IReadOnlyList<OnboardingStepAnalytics.StepEvent> Steps,
    CandidateOnboardingImpressionDto? Impression = null);

public sealed record CandidateOnboardingProgressRequest(
    int CurrentStep,
    bool? StepCompleted = null,
    bool? StepSkipped = null,
    string? Source = null);

public sealed record CandidateOnboardingImpressionDto(
    string Label,
    IReadOnlyList<OnboardingImpressionItemDto> Strengths,
    IReadOnlyList<OnboardingImpressionItemDto> Riasec,
    OnboardingImpressionItemDto? CultureHighlight,
    OnboardingImpressionItemDto? TopValue,
    int MatchingVacancyCount,
    IReadOnlyList<OnboardingMatchCardDto> MatchCards,
    IReadOnlyList<string> DreamJobSuggestions,
    bool CompetencyProvisional,
    bool CareerProvisional,
    bool CultureProvisional,
    bool ValuesProvisional);

public sealed record OnboardingImpressionItemDto(
    string Code,
    string Label,
    string Sentence,
    int? Percent = null);

public sealed record OnboardingMatchCardDto(
    Guid VacancyId,
    string Title,
    string CompanyName,
    int MatchPercent,
    string WhyLine,
    bool IsProvisional);
