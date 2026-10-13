using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Version-aware Quick-Scan helpers for candidate assessment services.</summary>
public static class QuickScanAssessmentBridge
{
    public static int FullCount(AssessmentKind kind, int quickScanVersion)
        => AssessmentQuickScanCatalog.QuestionCount(kind, quickScanVersion);

    public static Dictionary<int, int> ParseAnswers(AssessmentKind kind, string? json, int quickScanVersion)
        => AssessmentQuickScanCatalog.ParseAnswers(kind, json, quickScanVersion);

    public static string SerializeAnswers(
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion)
        => AssessmentQuickScanCatalog.SerializeAnswers(kind, answers, quickScanVersion);

    public static string? ValidateAnswers(
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        bool requireComplete,
        int quickScanVersion)
        => AssessmentQuickScanCatalog.ValidateAnswers(kind, answers, requireComplete, quickScanVersion);

    public static bool IsComplete(AssessmentKind kind, IReadOnlyDictionary<int, int> answers, int quickScanVersion)
        => answers.Count >= FullCount(kind, quickScanVersion);

    public static IReadOnlyList<string> DeriveCompetencyTags(CompetencyScores scores, int quickScanVersion)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? CompetencyTestCatalogV2.DeriveMatchTags(scores)
            : CompetencyTestCatalog.DeriveMatchTags(scores);

    public static IReadOnlyList<string> DeriveCultureTags(CulturePersonalityScores scores, int quickScanVersion)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? CulturePersonalityCatalogV2.DeriveMatchTags(scores)
            : CulturePersonalityCatalog.DeriveMatchTags(scores);

    public static IReadOnlyList<string> DeriveValuesTags(SchwartzValuesScores scores, int quickScanVersion)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? SchwartzValuesCatalogV2.DeriveMatchTags(scores)
            : SchwartzValuesCatalog.DeriveMatchTags(scores);

    public static IReadOnlyList<CompetencyQuestion> CompetenceQuestions(int quickScanVersion)
        => AssessmentQuickScanCatalog.CompetenceQuestions(quickScanVersion);

    public static IReadOnlyList<CareerQuestion> CareerQuestions(int quickScanVersion)
        => AssessmentQuickScanCatalog.CareerQuestions(quickScanVersion);

    public static RiasecScores? ScoreCareer(IReadOnlyDictionary<int, int> answers, int quickScanVersion)
        => AssessmentQuickScanCatalog.ScoreCareer(answers, quickScanVersion);

    public static IReadOnlyList<string> DeriveCareerTags(RiasecScores scores, int quickScanVersion)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? CareerTestCatalogV2.DeriveRiasecTags(scores)
            : CareerTestCatalog.DeriveRiasecTags(scores);

    public static string HollandCode(RiasecScores scores, int quickScanVersion)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? CareerTestCatalogV2.HollandCode(scores)
            : CareerTestCatalog.HollandCode(scores);

    public static CulturePersonalityScores? ScoreCulture(
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion)
        => AssessmentQuickScanCatalog.ScoreCulture(answers, quickScanVersion);

    public static SchwartzValuesScores? ScoreValues(
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion)
        => AssessmentQuickScanCatalog.ScoreValues(answers, quickScanVersion);

    public static IReadOnlyList<CompetencyQuestion> CultureQuestions(int quickScanVersion)
        => AssessmentQuickScanCatalog.CultureQuestions(quickScanVersion);

    public static IReadOnlyList<CompetencyQuestion> ValuesQuestions(int quickScanVersion)
        => AssessmentQuickScanCatalog.ValuesQuestions(quickScanVersion);
}
