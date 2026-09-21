using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateDiscService
{
    Task<CandidateDiscStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CandidateDiscStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default);

    Task<DiscScores?> GetCompletedScoresAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record CandidateDiscStateDto(
    string Status,
    IReadOnlyDictionary<int, int> Answers,
    int AnsweredCount,
    int QuestionCount,
    DiscScores? Scores,
    DiscScores? PreviewScores,
    DateTime? CompletedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<CompetencyQuestionDto> Questions,
    IReadOnlyList<string> MatchTags,
    string DeepAnalysisUpsellCopy);
