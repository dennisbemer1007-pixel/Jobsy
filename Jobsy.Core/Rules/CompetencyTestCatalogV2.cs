using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Quick-Scan v2 competence: 40 Likert items (8 per workplace dimension).
/// Same scoring model as <see cref="CompetencyTestCatalog"/> with expanded item coverage (COTAN alignment).
/// </summary>
public static class CompetencyTestCatalogV2
{
    public const int QuestionCount = 40;
    public const int BigFiveQuestionCount = 40;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;
    public const int CategoryQuestionCount = 8;

    public const string Samenwerken = CompetencyTestCatalog.Samenwerken;
    public const string Resultaatgerichtheid = CompetencyTestCatalog.Resultaatgerichtheid;
    public const string Stressbestendigheid = CompetencyTestCatalog.Stressbestendigheid;
    public const string Innovatie = CompetencyTestCatalog.Innovatie;
    public const string Extraversie = CompetencyTestCatalog.Extraversie;

    public static readonly string[] CategoryCodes = CompetencyTestCatalog.CategoryCodes;
    public static readonly string[] QuickScanCategories = CompetencyTestCatalog.QuickScanCategories;

    public static readonly IReadOnlyList<CompetencyQuestion> Questions =
    [
        // Samenwerken 1–8
        new(1, Samenwerken, Reverse: false, "AssessmentV2.Competency.Q01"),
        new(2, Samenwerken, Reverse: false, "AssessmentV2.Competency.Q02"),
        new(3, Samenwerken, Reverse: false, "AssessmentV2.Competency.Q03"),
        new(4, Samenwerken, Reverse: true, "AssessmentV2.Competency.Q04"),
        new(5, Samenwerken, Reverse: true, "AssessmentV2.Competency.Q05"),
        new(6, Samenwerken, Reverse: false, "AssessmentV2.Competency.Q06"),
        new(7, Samenwerken, Reverse: true, "AssessmentV2.Competency.Q07"),
        new(8, Samenwerken, Reverse: true, "AssessmentV2.Competency.Q08"),
        // Resultaatgerichtheid 9–16
        new(9, Resultaatgerichtheid, Reverse: false, "AssessmentV2.Competency.Q09"),
        new(10, Resultaatgerichtheid, Reverse: false, "AssessmentV2.Competency.Q10"),
        new(11, Resultaatgerichtheid, Reverse: false, "AssessmentV2.Competency.Q11"),
        new(12, Resultaatgerichtheid, Reverse: true, "AssessmentV2.Competency.Q12"),
        new(13, Resultaatgerichtheid, Reverse: true, "AssessmentV2.Competency.Q13"),
        new(14, Resultaatgerichtheid, Reverse: false, "AssessmentV2.Competency.Q14"),
        new(15, Resultaatgerichtheid, Reverse: true, "AssessmentV2.Competency.Q15"),
        new(16, Resultaatgerichtheid, Reverse: true, "AssessmentV2.Competency.Q16"),
        // Stressbestendigheid 17–24
        new(17, Stressbestendigheid, Reverse: false, "AssessmentV2.Competency.Q17"),
        new(18, Stressbestendigheid, Reverse: false, "AssessmentV2.Competency.Q18"),
        new(19, Stressbestendigheid, Reverse: false, "AssessmentV2.Competency.Q19"),
        new(20, Stressbestendigheid, Reverse: true, "AssessmentV2.Competency.Q20"),
        new(21, Stressbestendigheid, Reverse: true, "AssessmentV2.Competency.Q21"),
        new(22, Stressbestendigheid, Reverse: false, "AssessmentV2.Competency.Q22"),
        new(23, Stressbestendigheid, Reverse: true, "AssessmentV2.Competency.Q23"),
        new(24, Stressbestendigheid, Reverse: true, "AssessmentV2.Competency.Q24"),
        // Innovatie 25–32
        new(25, Innovatie, Reverse: false, "AssessmentV2.Competency.Q25"),
        new(26, Innovatie, Reverse: false, "AssessmentV2.Competency.Q26"),
        new(27, Innovatie, Reverse: false, "AssessmentV2.Competency.Q27"),
        new(28, Innovatie, Reverse: true, "AssessmentV2.Competency.Q28"),
        new(29, Innovatie, Reverse: true, "AssessmentV2.Competency.Q29"),
        new(30, Innovatie, Reverse: false, "AssessmentV2.Competency.Q30"),
        new(31, Innovatie, Reverse: true, "AssessmentV2.Competency.Q31"),
        new(32, Innovatie, Reverse: true, "AssessmentV2.Competency.Q32"),
        // Extraversie 33–40
        new(33, Extraversie, Reverse: false, "AssessmentV2.Competency.Q33"),
        new(34, Extraversie, Reverse: false, "AssessmentV2.Competency.Q34"),
        new(35, Extraversie, Reverse: false, "AssessmentV2.Competency.Q35"),
        new(36, Extraversie, Reverse: true, "AssessmentV2.Competency.Q36"),
        new(37, Extraversie, Reverse: true, "AssessmentV2.Competency.Q37"),
        new(38, Extraversie, Reverse: false, "AssessmentV2.Competency.Q38"),
        new(39, Extraversie, Reverse: true, "AssessmentV2.Competency.Q39"),
        new(40, Extraversie, Reverse: true, "AssessmentV2.Competency.Q40")
    ];

    public static bool IsValidAnswer(int value) => LikertAnswerJson.IsValidAnswer(value);

    public static string? ValidateAnswers(IReadOnlyDictionary<int, int> answers, bool requireComplete)
    {
        foreach (var (id, value) in answers)
        {
            if (id is < 1 or > QuestionCount)
            {
                return "Onbekend vraagnummer in de competentietest.";
            }

            if (!IsValidAnswer(value))
            {
                return "Elk antwoord moet tussen 1 (helemaal oneens) en 5 (helemaal eens) liggen.";
            }
        }

        if (requireComplete && !IsComplete(answers))
        {
            return "Beantwoord alle 40 vragen om de Quick-Scan af te ronden.";
        }

        return null;
    }

    public static bool IsComplete(IReadOnlyDictionary<int, int> answers)
    {
        if (answers.Count < QuestionCount)
        {
            return false;
        }

        for (var i = 1; i <= QuestionCount; i++)
        {
            if (!answers.TryGetValue(i, out var value) || !IsValidAnswer(value))
            {
                return false;
            }
        }

        return true;
    }

    public static Dictionary<int, int> ParseAnswersJson(string? json)
        => LikertAnswerJson.Parse(json, QuestionCount);

    public static string SerializeAnswers(IReadOnlyDictionary<int, int> answers)
        => LikertAnswerJson.Serialize(answers, QuestionCount);

    public static CompetencyScores? Score(IReadOnlyDictionary<int, int> answers)
        => CompetencyTestCatalog.Score(answers, Questions);

    public static CompetencyScores? Score(
        IReadOnlyDictionary<int, int> answers,
        IEnumerable<CompetencyQuestion> items)
        => CompetencyTestCatalog.Score(answers, items);

    public static IReadOnlyList<string> DeriveMatchTags(
        CompetencyScores? scores,
        IReadOnlyList<string>? extraTags = null)
        => CompetencyTestCatalog.DeriveMatchTags(scores, extraTags);

    public static string SerializeTags(IEnumerable<string> tags)
        => CompetencyTestCatalog.SerializeTags(tags);

    public static IReadOnlyList<string> ParseTagsJson(string? json)
        => CompetencyTestCatalog.ParseTagsJson(json);
}
