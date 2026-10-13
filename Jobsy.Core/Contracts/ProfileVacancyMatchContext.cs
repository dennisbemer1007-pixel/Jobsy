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
    public RiasecScores? RiasecScores { get; init; }
    public bool CareerDeepCompleted { get; init; }
    public IReadOnlyList<CareerOccupationMatch> CareerOccupations { get; init; } = [];
    public CulturePersonalityScores? CultureScores { get; init; }
    public SchwartzValuesScores? ValuesScores { get; init; }

    /// <summary>True when any assessment score came from wizard/draft answers rather than a completed test.</summary>
    public bool IsProvisional { get; init; }

    /// <summary>
    /// Candidate ticked "Beschikbaar voor werk". Matching and applying stay off until this is true.
    /// Defaults to true so hand-built test contexts still score.
    /// </summary>
    public bool OpenForWork { get; init; } = true;
}
