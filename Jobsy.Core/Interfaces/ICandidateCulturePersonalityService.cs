using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateCulturePersonalityService
{
    Task<CandidateCulturePersonalityStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CandidateCulturePersonalityStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default);

    Task<CulturePersonalityScores?> GetCompletedScoresAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record CandidateCulturePersonalityStateDto(
    string Status,
    IReadOnlyDictionary<int, int> Answers,
    CulturePersonalityScores? Scores,
    IReadOnlyList<string> MatchTags,
    DateTime? CompletedAtUtc,
    decimal DeepAnalysisPriceEuro);

public interface ICompanyCultureService
{
    Task<CompanyCultureStateDto> GetAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<CompanyCultureStateDto> SaveAsync(
        Guid companyId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default);

    Task<CulturePersonalityScores?> GetCompletedScoresAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}

public sealed record CompanyCultureStateDto(
    string Status,
    IReadOnlyDictionary<int, int> Answers,
    CulturePersonalityScores? Scores,
    DateTime? CompletedAtUtc);
