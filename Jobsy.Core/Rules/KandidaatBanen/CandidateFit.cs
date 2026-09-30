namespace Jobsy.Core.Rules.KandidaatBanen;

/// <summary>Four DNA dimension scores (0–100) or null when that test/score is unavailable.</summary>
public sealed record CandidateFitDimensions(
    int? Culture,
    int? Values,
    int? Competencies,
    int? Interests);

/// <summary>
/// Candidate-facing fit display (D2). Null when the gate is closed — surfaces show "Maak je paspoort af".
/// </summary>
public sealed class CandidateFit
{
    public required int Percent { get; init; }
    public required KbFitBand Band { get; init; }

    /// <summary>
    /// Why-kind codes for localization (<c>Kb.Why.{kind}</c>), at most two, joined with " · ".
    /// Empty when there is no why data — omit the line.
    /// </summary>
    public IReadOnlyList<string> WhyKinds { get; init; } = [];

    public required CandidateFitDimensions Dimensions { get; init; }

    public string BandCss => Band switch
    {
        KbFitBand.Strong => "green",
        KbFitBand.Good => "orange",
        _ => "red"
    };
}
