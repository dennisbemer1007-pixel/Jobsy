using Jobsy.Core.Rules;

namespace Jobsy.Core.Contracts;

/// <summary>Candidate snapshot used to score vacancies without extra DB round-trips.</summary>
public sealed class ProfileVacancyMatchContext
{
    public Guid UserId { get; init; }
    public double? HomeLatitude { get; init; }
    public double? HomeLongitude { get; init; }
    public required CandidatePreferencesDto Prefs { get; init; }
    public int? AgeYears { get; init; }
    public CompetencyScores? Competencies { get; init; }
    public IReadOnlyList<string> RiasecTags { get; init; } = [];
}
