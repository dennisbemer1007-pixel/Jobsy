using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;

namespace Jobsy.Core.Rules.KandidaatBanen;

/// <summary>
/// Applies <see cref="CandidateFitDisplay"/> onto candidate DTO field bags.
/// Employer/shared payloads must not call this.
/// </summary>
public static class CandidateFitApply
{
    public const string FitGateOpen = "open";
    public const string FitGateClosed = "closed";

    public static CandidateFitApplyResult Apply(ProfileVacancyMatch match, CandidateFitGate gate)
    {
        ArgumentNullException.ThrowIfNull(match);
        if (match.CalibrationV1 && gate.IsOpen)
        {
            return new CandidateFitApplyResult(
                FitGate: FitGateOpen,
                FitPercent: match.FitRawPercent,
                FitBand: match.ColorBand,
                FitWhyKinds: CandidateFitDisplay.MapWhyKinds(match),
                FitWhyLineNl: match.ConfidencePercent is int conf
                    ? MatchConfidenceCopy.NlHint(conf)
                    : null,
                FitDimensions: CandidateFitDisplay.BuildDimensions(match),
                MatchPercent: match.TotalPercent,
                MatchColorBand: match.ColorBand);
        }

        var fit = CandidateFitDisplay.Build(match, gate);
        if (fit is null)
        {
            var practicalKinds = CandidateFitDisplay.MapPracticalWhyKinds(match);
            var practicalLine = CandidateFitDisplay.FormatWhyLine(practicalKinds, NlWhyFallback);
            return new CandidateFitApplyResult(
                FitGate: FitGateClosed,
                FitPercent: null,
                FitBand: null,
                FitWhyKinds: practicalKinds,
                FitWhyLineNl: practicalLine,
                FitDimensions: null,
                MatchPercent: null,
                MatchColorBand: null);
        }

        var whyNl = CandidateFitDisplay.FormatWhyLine(
            fit.WhyKinds,
            key => NlWhyFallback(key));

        return new CandidateFitApplyResult(
            FitGate: FitGateOpen,
            FitPercent: fit.Percent,
            FitBand: fit.Band.ToString().ToLowerInvariant(),
            FitWhyKinds: fit.WhyKinds,
            FitWhyLineNl: whyNl,
            FitDimensions: fit.Dimensions,
            MatchPercent: fit.Percent,
            MatchColorBand: fit.BandCss);
    }

    /// <summary>NL fallbacks for API payloads (jobMap / clients without CultureState).</summary>
    private static string NlWhyFallback(string key) => key switch
    {
        "Kb.Why.culture" => "Je past bij de cultuur",
        "Kb.Why.values" => "Je waarden sluiten aan",
        "Kb.Why.competency" => "Je competenties passen",
        "Kb.Why.interest" => "Dit past bij je interesses",
        "Kb.Why.travel" => "Goede reistijd",
        "Kb.Why.hours" => "Je uren passen",
        _ => key
    };
}

public sealed record CandidateFitApplyResult(
    string FitGate,
    int? FitPercent,
    string? FitBand,
    IReadOnlyList<string> FitWhyKinds,
    string? FitWhyLineNl,
    CandidateFitDimensions? FitDimensions,
    int? MatchPercent,
    string? MatchColorBand);
