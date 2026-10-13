using Jobsy.Core.Enums;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Resolves Quick-Scan v1 vs v2 catalogs (counts, items, scoring, JSON helpers).
/// </summary>
public static class AssessmentQuickScanCatalog
{
    public static int QuestionCount(AssessmentKind kind, int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? kind switch
            {
                AssessmentKind.Culture => CulturePersonalityCatalogV2.QuestionCount,
                AssessmentKind.Competence => CompetencyTestCatalogV2.QuestionCount,
                AssessmentKind.Career => CareerTestCatalogV2.QuestionCount,
                AssessmentKind.Values => SchwartzValuesCatalogV2.QuestionCount,
                _ => CompetencyTestCatalogV2.QuestionCount
            }
            : kind switch
            {
                AssessmentKind.Culture => CulturePersonalityCatalog.QuestionCount,
                AssessmentKind.Competence => CompetencyTestCatalog.QuestionCount,
                AssessmentKind.Career => CareerTestCatalog.QuestionCount,
                AssessmentKind.Values => SchwartzValuesCatalog.QuestionCount,
                _ => CompetencyTestCatalog.QuestionCount
            };

    public static IReadOnlyList<CompetencyQuestion> CompetenceQuestions(int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? CompetencyTestCatalogV2.Questions
            : CompetencyTestCatalog.Questions;

    public static IReadOnlyList<CareerQuestion> CareerQuestions(int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? CareerTestCatalogV2.Questions
            : CareerTestCatalog.Questions;

    public static IReadOnlyList<CompetencyQuestion> CultureQuestions(int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? CulturePersonalityCatalogV2.Questions
            : CulturePersonalityCatalog.Questions;

    public static IReadOnlyList<CompetencyQuestion> ValuesQuestions(int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? SchwartzValuesCatalogV2.Questions
            : SchwartzValuesCatalog.Questions;

    public static CompetencyScores? ScoreCompetence(
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? CompetencyTestCatalogV2.Score(answers)
            : CompetencyTestCatalog.Score(answers);

    public static RiasecScores? ScoreCareer(
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? CareerTestCatalogV2.Score(answers)
            : CareerTestCatalog.Score(answers);

    public static CulturePersonalityScores? ScoreCulture(
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? CulturePersonalityCatalogV2.Score(answers)
            : CulturePersonalityCatalog.Score(answers);

    public static SchwartzValuesScores? ScoreValues(
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? SchwartzValuesCatalogV2.Score(answers)
            : SchwartzValuesCatalog.Score(answers);

    public static Dictionary<int, int> ParseAnswers(AssessmentKind kind, string? json, int quickScanVersion = AssessmentTestVersions.V1)
        => LikertAnswerJson.Parse(json, QuestionCount(kind, quickScanVersion));

    public static string SerializeAnswers(
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion = AssessmentTestVersions.V1)
        => LikertAnswerJson.Serialize(answers, QuestionCount(kind, quickScanVersion));

    public static string? ValidateAnswers(
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        bool requireComplete,
        int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) == AssessmentTestVersions.V2
            ? kind switch
            {
                AssessmentKind.Culture => CulturePersonalityCatalogV2.ValidateAnswers(answers, requireComplete),
                AssessmentKind.Competence => CompetencyTestCatalogV2.ValidateAnswers(answers, requireComplete),
                AssessmentKind.Career => CareerTestCatalogV2.ValidateAnswers(answers, requireComplete),
                AssessmentKind.Values => SchwartzValuesCatalogV2.ValidateAnswers(answers, requireComplete),
                _ => CompetencyTestCatalogV2.ValidateAnswers(answers, requireComplete)
            }
            : kind switch
            {
                AssessmentKind.Culture => CulturePersonalityCatalog.ValidateAnswers(answers, requireComplete),
                AssessmentKind.Competence => CompetencyTestCatalog.ValidateAnswers(answers, requireComplete),
                AssessmentKind.Career => CareerTestCatalog.ValidateAnswers(answers, requireComplete),
                AssessmentKind.Values => SchwartzValuesCatalog.ValidateAnswers(answers, requireComplete),
                _ => CompetencyTestCatalog.ValidateAnswers(answers, requireComplete)
            };
}
