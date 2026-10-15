namespace Jobsy.Core.Contracts;

/// <summary>Single payload for Ontdekkingsreis “Verder ontdekken” — avoids N+1 test GETs from the Web circuit.</summary>
public sealed record CandidateDiscoveryOverviewPanelDto(
    string ProfileRevision,
    int CompetencyAnswered,
    int CareerAnswered,
    int CultureAnswered,
    int ValuesAnswered);
