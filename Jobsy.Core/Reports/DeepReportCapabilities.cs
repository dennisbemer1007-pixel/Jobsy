using Jobsy.Core.Enums;

namespace Jobsy.Core.Reports;

/// <summary>
/// Single source of truth for which deep-report cards the paid in-app view and PDF deliver per kind.
/// Comparison cards for Career/Culture/Values remain runtime-gated by Lobsy N ≥ 100.
/// </summary>
public static class DeepReportCapabilities
{
    public sealed record CardSpec(DeepReportCardKey Key, string TitleKey, string SubtitleKey);

    public sealed record KindCapabilities(
        AssessmentKind Kind,
        int PdfPageCount,
        IReadOnlyList<CardSpec> Cards,
        IReadOnlyList<string> CheckmarkKeys);

    private static readonly IReadOnlyList<string> DefaultChecks =
    [
        "TestResult.Gold.Check.Pages",
        "TestResult.Gold.Check.Applications",
        "TestResult.Gold.Check.Matches"
    ];

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
        CheckmarkKeys: DefaultChecks);

    private static readonly KindCapabilities CareerCaps = new(
        AssessmentKind.Career,
        PdfPageCount: 8,
        Cards:
        [
            new(DeepReportCardKey.RadarVsNorm, "TestResult.Card.Radar", "TestResult.Card.Radar.Sub.Lobsy"),
            new(DeepReportCardKey.HollandCode, "TestResult.Card.Holland", "TestResult.Card.Holland.Sub"),
            new(DeepReportCardKey.Occupations, "TestResult.Card.Occupations", "TestResult.Card.Occupations.Sub"),
            new(DeepReportCardKey.Comparison, "TestResult.Card.Comparison", "TestResult.Card.Comparison.Sub.Lobsy"),
            new(DeepReportCardKey.ActionPlan, "TestResult.Card.ActionPlan", "TestResult.Card.ActionPlan.Sub"),
            new(DeepReportCardKey.StrengthsPitfalls, "TestResult.Card.Strengths", "TestResult.Card.Strengths.Sub")
        ],
        CheckmarkKeys: DefaultChecks);

    private static readonly KindCapabilities CultureCaps = new(
        AssessmentKind.Culture,
        PdfPageCount: 8,
        Cards:
        [
            new(DeepReportCardKey.RadarVsNorm, "TestResult.Card.Radar", "TestResult.Card.Radar.Sub.Lobsy"),
            new(DeepReportCardKey.Employers, "TestResult.Card.Employers", "TestResult.Card.Employers.Sub"),
            new(DeepReportCardKey.Comparison, "TestResult.Card.Comparison", "TestResult.Card.Comparison.Sub.Lobsy"),
            new(DeepReportCardKey.Facets, "TestResult.Card.Facets", "TestResult.Card.Facets.Sub.Culture"),
            new(DeepReportCardKey.ActionPlan, "TestResult.Card.ActionPlan", "TestResult.Card.ActionPlan.Sub"),
            new(DeepReportCardKey.StrengthsPitfalls, "TestResult.Card.Strengths", "TestResult.Card.Strengths.Sub")
        ],
        CheckmarkKeys: DefaultChecks);

    private static readonly KindCapabilities ValuesCaps = new(
        AssessmentKind.Values,
        PdfPageCount: 7,
        Cards:
        [
            new(DeepReportCardKey.RadarVsNorm, "TestResult.Card.Radar", "TestResult.Card.Radar.Sub.Lobsy"),
            new(DeepReportCardKey.ValuesRanking, "TestResult.Card.ValuesRank", "TestResult.Card.ValuesRank.Sub"),
            new(DeepReportCardKey.Employers, "TestResult.Card.Employers", "TestResult.Card.Employers.Sub"),
            new(DeepReportCardKey.Comparison, "TestResult.Card.Comparison", "TestResult.Card.Comparison.Sub.Lobsy"),
            new(DeepReportCardKey.ActionPlan, "TestResult.Card.ActionPlan", "TestResult.Card.ActionPlan.Sub"),
            new(DeepReportCardKey.StrengthsPitfalls, "TestResult.Card.Strengths", "TestResult.Card.Strengths.Sub")
        ],
        CheckmarkKeys: DefaultChecks);

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

    /// <summary>
    /// Runtime filter: hide Comparison when Lobsy norms are not ready (Career/Culture/Values).
    /// Competence comparison always stays (Johnson 2014).
    /// </summary>
    public static IReadOnlyList<CardSpec> VisibleCards(AssessmentKind kind, bool lobsyNormsReady)
    {
        var caps = For(kind);
        if (kind is AssessmentKind.Competence || lobsyNormsReady)
        {
            return caps.Cards;
        }

        return caps.Cards.Where(c => c.Key != DeepReportCardKey.Comparison).ToList();
    }
}
