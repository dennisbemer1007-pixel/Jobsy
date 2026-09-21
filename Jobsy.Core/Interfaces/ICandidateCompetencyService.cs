using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateCompetencyService
{
    Task<CandidateCompetencyStateDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<CandidateCompetencyStateDto> SaveAsync(
        Guid userId,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default);

    Task<CompetencyScores?> GetCompletedScoresAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CandidateMatchedVacancyDto>> GetTopMatchesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record CandidateCompetencyStateDto(
    string Status,
    IReadOnlyDictionary<int, int> Answers,
    int AnsweredCount,
    int QuestionCount,
    CompetencyScores? Scores,
    CompetencyScores? PreviewScores,
    DateTime? CompletedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<CompetencyQuestionDto> Questions,
    IReadOnlyList<string> MatchTags,
    string DeepAnalysisUpsellCopy);

public sealed record CompetencyQuestionDto(
    int Id,
    string Category,
    bool Reverse,
    string TextKey,
    bool IsRiasec = false);

public sealed record CandidateMatchedVacancyDto(
    Guid Id,
    string Title,
    string CompanyName,
    string? ImageUrl,
    string? CompanyLogoUrl,
    int MatchPercent,
    string ColorBand,
    IReadOnlyList<string> Why,
    IReadOnlyList<string> Gaps,
    bool IsBroadMatch = false,
    string? MatchRationale = null);
