using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateCareerInterestService
{
    Task<CandidateCareerInterestStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CandidateCareerInterestStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default);

    Task<RiasecScores?> GetCompletedScoresAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetCompletedTagsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record CandidateCareerInterestStateDto(
    string Status,
    IReadOnlyDictionary<int, int> Answers,
    int AnsweredCount,
    int QuestionCount,
    RiasecScores? Scores,
    RiasecScores? PreviewScores,
    string HollandCode,
    DateTime? CompletedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<CareerQuestionDto> Questions,
    IReadOnlyList<string> RiasecTags,
    IReadOnlyList<string> MatchTags,
    string DeepAnalysisUpsellCopy,
    IReadOnlyList<CandidateMatchedVacancyDto> TopVacancies,
    CareerCompassSnapshot Compass);

public sealed record CareerQuestionDto(
    int Id,
    string Category,
    bool Reverse,
    string TextKey);
