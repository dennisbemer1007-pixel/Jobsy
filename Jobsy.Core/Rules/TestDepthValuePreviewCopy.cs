using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Unlock-card preview: readable titles + promise line for the next depth step.</summary>
public static class TestDepthValuePreviewCopy
{
    public sealed record PreviewModel(
        string PromiseNl,
        string PromiseEn,
        IReadOnlyList<string> ThemeTitleKeys,
        bool FullMirrorSections = false);

    private const string PracticeSectionKey = "TestReflection.PracticeLabel";
    private const string EnergySectionKey = "TestReflection.EnergyLabel";
    private const string WatchSectionKey = "TestReflection.WatchLabel";

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
                [PracticeSectionKey, EnergySectionKey, WatchSectionKey],
                FullMirrorSections: true),
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

    /// <summary>Blurred placeholder under mirror section titles in unlock preview.</summary>
    public static string MirrorSectionBody(string sectionTitleKey, bool dutch)
    {
        return sectionTitleKey switch
        {
            PracticeSectionKey => dutch
                ? "Overzicht houden als de planning verschuift. Beleefd blijven als het druk wordt."
                : "Keeping overview when plans shift. Staying polite when it gets busy.",
            EnergySectionKey => dutch
                ? "Geeft energie: duidelijke afspraken en een rustig ritme. Kost energie: chaos zonder houvast."
                : "Gives energy: clear agreements and a steady rhythm. Drains energy: chaos without structure.",
            WatchSectionKey => dutch
                ? "Let op of je te snel ja zegt. Vraag om hulp voordat het te vol wordt."
                : "Notice if you say yes too quickly. Ask for help before it gets too much.",
            _ => dutch
                ? "Meer uitleg en voorbeelden uit je volledige spiegel."
                : "More explanation and examples from your full mirror."
        };
    }
}
