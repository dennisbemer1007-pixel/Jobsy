using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Unlock-card preview: readable titles + promise line for the next depth step.</summary>
public static class TestDepthValuePreviewCopy
{
    public sealed record PreviewModel(
        string PromiseNl,
        string PromiseEn,
        IReadOnlyList<string> ThemeTitleKeys);

    public static PreviewModel? NextPreview(
        TestDepthLevel? next,
        int quickScanVersion = AssessmentTestVersions.V1)
    {
        _ = quickScanVersion;
        return next switch
        {
            TestDepthLevel.First => new PreviewModel(
                "Bij 5 vragen: je eerste thema in je mini-spiegel.",
                "After 5 questions: your first theme in your mini mirror.",
                [CompetencyTestCatalog.Stressbestendigheid]),
            TestDepthLevel.Deeper => new PreviewModel(
                "Bij 10 vragen: drie thema's en waar je energie van krijgt.",
                "After 10 questions: three themes and what gives you energy.",
                [
                    CompetencyTestCatalog.Samenwerken,
                    CompetencyTestCatalog.Stressbestendigheid,
                    CompetencyTestCatalog.Extraversie
                ]),
            TestDepthLevel.Full => new PreviewModel(
                "Bij 25 vragen: je volledige spiegel, gratis.",
                "After 25 questions: your full mirror, free.",
                [
                    CompetencyTestCatalog.Samenwerken,
                    CompetencyTestCatalog.Resultaatgerichtheid,
                    CompetencyTestCatalog.Stressbestendigheid
                ]),
            TestDepthLevel.Bottom => new PreviewModel(
                "Bij 150 vragen: uitgebreid rapport in woorden met actieplan.",
                "After 150 questions: extended report in words with an action plan.",
                [
                    DeepAnalysisCompetenceItems.Consciëntieusheid,
                    DeepAnalysisCompetenceItems.Vriendelijkheid,
                    DeepAnalysisCompetenceItems.EmotioneleStabiliteit
                ]),
            _ => null
        };
    }
}
