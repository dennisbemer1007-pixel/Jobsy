using Jobsy.Core.Enums;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Resolves Quick-Scan v1 / legacy v2 / tier v3 catalogs (counts, items, scoring, JSON helpers).
/// </summary>
public static class AssessmentQuickScanCatalog
{
    public static int QuestionCount(AssessmentKind kind, int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) switch
        {
            AssessmentTestVersions.V3 => QuickScanFreeTierV3.QuestionCount,
            AssessmentTestVersions.V2 => kind switch
            {
                AssessmentKind.Culture => CulturePersonalityCatalogV2.QuestionCount,
                AssessmentKind.Competence => CompetencyTestCatalogV2.QuestionCount,
                AssessmentKind.Career => CareerTestCatalogV2.QuestionCount,
                AssessmentKind.Values => SchwartzValuesCatalogV2.QuestionCount,
                _ => CompetencyTestCatalogV2.QuestionCount
            },
            _ => kind switch
            {
                AssessmentKind.Culture => CulturePersonalityCatalog.QuestionCount,
                AssessmentKind.Competence => CompetencyTestCatalog.QuestionCount,
                AssessmentKind.Career => CareerTestCatalog.QuestionCount,
                AssessmentKind.Values => SchwartzValuesCatalog.QuestionCount,
                _ => CompetencyTestCatalog.QuestionCount
            }
        };

    public static IReadOnlyList<CompetencyQuestion> CompetenceQuestions(int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) switch
        {
            AssessmentTestVersions.V3 => QuickScanFreeTierV3.CompetenceQuestions(),
            AssessmentTestVersions.V2 => CompetencyTestCatalogV2.Questions,
            _ => CompetencyTestCatalog.Questions
        };

    public static IReadOnlyList<CareerQuestion> CareerQuestions(int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) switch
        {
            AssessmentTestVersions.V3 => QuickScanFreeTierV3.CareerQuestions(),
            AssessmentTestVersions.V2 => CareerTestCatalogV2.Questions,
            _ => CareerTestCatalog.Questions
        };

    public static IReadOnlyList<CompetencyQuestion> CultureQuestions(int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) switch
        {
            AssessmentTestVersions.V3 => QuickScanFreeTierV3.CultureQuestions(),
            AssessmentTestVersions.V2 => CulturePersonalityCatalogV2.Questions,
            _ => CulturePersonalityCatalog.Questions
        };

    public static IReadOnlyList<CompetencyQuestion> ValuesQuestions(int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) switch
        {
            AssessmentTestVersions.V3 => QuickScanFreeTierV3.ValuesQuestions(),
            AssessmentTestVersions.V2 => SchwartzValuesCatalogV2.Questions,
            _ => SchwartzValuesCatalog.Questions
        };

    public static CompetencyScores? ScoreCompetence(
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) switch
        {
            AssessmentTestVersions.V3 => QuickScanFreeTierV3.ScoreCompetence(answers),
            AssessmentTestVersions.V2 => CompetencyTestCatalogV2.Score(answers),
            _ => CompetencyTestCatalog.Score(answers)
        };

    public static RiasecScores? ScoreCareer(
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) switch
        {
            AssessmentTestVersions.V3 => QuickScanFreeTierV3.ScoreCareer(answers),
            AssessmentTestVersions.V2 => CareerTestCatalogV2.Score(answers),
            _ => CareerTestCatalog.Score(answers)
        };

    public static CulturePersonalityScores? ScoreCulture(
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) switch
        {
            AssessmentTestVersions.V3 => QuickScanFreeTierV3.ScoreCulture(answers),
            AssessmentTestVersions.V2 => CulturePersonalityCatalogV2.Score(answers),
            _ => CulturePersonalityCatalog.Score(answers)
        };

    public static SchwartzValuesScores? ScoreValues(
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) switch
        {
            AssessmentTestVersions.V3 => QuickScanFreeTierV3.ScoreValues(answers),
            AssessmentTestVersions.V2 => SchwartzValuesCatalogV2.Score(answers),
            _ => SchwartzValuesCatalog.Score(answers)
        };

    public static Dictionary<int, int> ParseAnswers(AssessmentKind kind, string? json, int quickScanVersion = AssessmentTestVersions.V1)
    {
        var version = AssessmentTestVersions.Normalize(quickScanVersion);
        var maxId = version == AssessmentTestVersions.V3
            ? QuickScanFreeTierV3.MaxStoredQuestionId(kind)
            : QuestionCount(kind, quickScanVersion);
        return LikertAnswerJson.Parse(json, maxId);
    }

    public static string SerializeAnswers(
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        int quickScanVersion = AssessmentTestVersions.V1)
    {
        var version = AssessmentTestVersions.Normalize(quickScanVersion);
        var maxId = version == AssessmentTestVersions.V3
            ? QuickScanFreeTierV3.MaxStoredQuestionId(kind)
            : QuestionCount(kind, quickScanVersion);
        return LikertAnswerJson.Serialize(answers, maxId);
    }

    public static string? ValidateAnswers(
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        bool requireComplete,
        int quickScanVersion = AssessmentTestVersions.V1)
        => AssessmentTestVersions.Normalize(quickScanVersion) switch
        {
            AssessmentTestVersions.V3 => QuickScanFreeTierV3.ValidateAnswers(kind, answers, requireComplete),
            AssessmentTestVersions.V2 => kind switch
            {
                AssessmentKind.Culture => CulturePersonalityCatalogV2.ValidateAnswers(answers, requireComplete),
                AssessmentKind.Competence => CompetencyTestCatalogV2.ValidateAnswers(answers, requireComplete),
                AssessmentKind.Career => CareerTestCatalogV2.ValidateAnswers(answers, requireComplete),
                AssessmentKind.Values => SchwartzValuesCatalogV2.ValidateAnswers(answers, requireComplete),
                _ => CompetencyTestCatalogV2.ValidateAnswers(answers, requireComplete)
            },
            _ => kind switch
            {
                AssessmentKind.Culture => CulturePersonalityCatalog.ValidateAnswers(answers, requireComplete),
                AssessmentKind.Competence => CompetencyTestCatalog.ValidateAnswers(answers, requireComplete),
                AssessmentKind.Career => CareerTestCatalog.ValidateAnswers(answers, requireComplete),
                AssessmentKind.Values => SchwartzValuesCatalog.ValidateAnswers(answers, requireComplete),
                _ => CompetencyTestCatalog.ValidateAnswers(answers, requireComplete)
            }
        };
}
