using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateValuesService
{
    Task<CandidateValuesStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CandidateValuesStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default);

    Task<SchwartzValuesScores?> GetCompletedScoresAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record CandidateValuesStateDto(
    string Status,
    IReadOnlyDictionary<int, int> Answers,
    SchwartzValuesScores? Scores,
    IReadOnlyList<string> MatchTags,
    DateTime? CompletedAtUtc,
    decimal DeepAnalysisPriceEuro);
