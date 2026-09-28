using Jobsy.Core.Enums;

namespace Jobsy.Core.Reports;

/// <summary>
/// Single source of truth for which deep-report cards the paid in-app view and PDF deliver per kind.
/// Locked teaser cards and gold-block checkmarks read from here. PR B expands Career/Culture/Values.
/// </summary>
public static class DeepReportCapabilities
{
    public sealed record CardSpec(DeepReportCardKey Key, string TitleKey, string SubtitleKey);

    public sealed record KindCapabilities(
        AssessmentKind Kind,
        int PdfPageCount,
        IReadOnlyList<CardSpec> Cards,
        IReadOnlyList<string> CheckmarkKeys);

    private static readonly KindCapabilities CompetenceCaps = new(
        AssessmentKind.Competence,
        PdfPageCount: 9,
        Cards:
        [
            new(DeepReportCardKey.RadarVsNorm, "TestResult.Card.Radar", "TestResult.Card.Radar.Sub.Competence"),
            new(DeepReportCardKey.Comparison, "TestResult.Card.Comparison", "TestResult.Card.Comparison.Sub.Competence"),
            new(DeepReportCardKey.Facets, "TestResult.Card.Facets", "TestResult.Card.Facets.Sub"),
            new(DeepReportCardKey.Occupations, "TestResult.Card.Occupations", "TestResult.Card.Occupations.Sub"),
            new(DeepReportCardKey.ActionPlan, "TestResult.Card.ActionPlan", "TestResult.Card.ActionPlan.Sub"),
            new(DeepReportCardKey.StrengthsPitfalls, "TestResult.Card.Strengths", "TestResult.Card.Strengths.Sub")
        ],
        CheckmarkKeys:
        [
            "TestResult.Gold.Check.Pages",
            "TestResult.Gold.Check.Applications",
            "TestResult.Gold.Check.Matches"
        ]);

    // Until PR B delivers full Career deep view + PDF sections, only the real occupations card is gated in.
    private static readonly KindCapabilities CareerCaps = new(
        AssessmentKind.Career,
        PdfPageCount: 1,
        Cards:
        [
            new(DeepReportCardKey.Occupations, "TestResult.Card.Occupations", "TestResult.Card.Occupations.Sub")
        ],
        CheckmarkKeys:
        [
            "TestResult.Gold.Check.Pages",
            "TestResult.Gold.Check.Applications",
            "TestResult.Gold.Check.Matches"
        ]);

    // Culture/Values paid parity lands in PR B — show no locked promises until then.
    private static readonly KindCapabilities CultureCaps = new(
        AssessmentKind.Culture,
        PdfPageCount: 1,
        Cards: [],
        CheckmarkKeys:
        [
            "TestResult.Gold.Check.Pages",
            "TestResult.Gold.Check.Applications",
            "TestResult.Gold.Check.Matches"
        ]);

    private static readonly KindCapabilities ValuesCaps = new(
        AssessmentKind.Values,
        PdfPageCount: 1,
        Cards: [],
        CheckmarkKeys:
        [
            "TestResult.Gold.Check.Pages",
            "TestResult.Gold.Check.Applications",
            "TestResult.Gold.Check.Matches"
        ]);

    public static KindCapabilities For(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Career => CareerCaps,
        AssessmentKind.Culture => CultureCaps,
        AssessmentKind.Values => ValuesCaps,
        _ => CompetenceCaps
    };

    public static bool HasCard(AssessmentKind kind, DeepReportCardKey key)
        => For(kind).Cards.Any(c => c.Key == key);

    public static IReadOnlyList<DeepReportCardKey> CardKeys(AssessmentKind kind)
        => For(kind).Cards.Select(c => c.Key).ToList();
}
