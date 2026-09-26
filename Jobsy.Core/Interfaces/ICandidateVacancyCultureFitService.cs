using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateVacancyCultureFitService
{
    /// <summary>
    /// Resolve culture-fit for GET: stored AI result when fingerprint matches,
    /// otherwise local fallback + enqueue AI refine (never blocks on OpenAI).
    /// </summary>
    Task<(CultureFitResult? Result, string Status)> ResolveForGetAsync(
        Guid userId,
        Guid vacancyId,
        IReadOnlyList<string> culturePillars,
        CompetencyScores? competencies,
        CulturePersonalityScores? cultureScores,
        CultureFitResult? localResult,
        CancellationToken cancellationToken = default);

    /// <summary>Background AI refine for one candidate×vacancy pair.</summary>
    Task RefineAsync(Guid userId, Guid vacancyId, CancellationToken cancellationToken = default);

    Task InvalidateForVacancyAsync(Guid vacancyId, CancellationToken cancellationToken = default);

    Task<CultureFitResult?> GetStoredAsync(
        Guid userId,
        Guid vacancyId,
        CancellationToken cancellationToken = default);
}

public sealed record VacancyCultureFitDto(
    int? Percent,
    string? Band,
    string? Label,
    string? Why,
    string Status,
    bool FromOpenAi);
