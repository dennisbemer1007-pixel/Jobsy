namespace Jobsy.Core.Rules;

/// <summary>
/// Quick-Scan v2 values: 40 Likert items (8 per Schwartz workplace driver).
/// </summary>
public static class SchwartzValuesCatalogV2
{
    public const int QuestionCount = 40;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;
    public const int CategoryQuestionCount = 8;

    public const string Autonomy = SchwartzValuesCatalog.Autonomy;
    public const string Connection = SchwartzValuesCatalog.Connection;
    public const string Achievement = SchwartzValuesCatalog.Achievement;
    public const string Stability = SchwartzValuesCatalog.Stability;
    public const string Impact = SchwartzValuesCatalog.Impact;

    public static readonly string[] CategoryCodes = SchwartzValuesCatalog.CategoryCodes;

    public static readonly IReadOnlyList<CompetencyQuestion> Questions =
    [
        // Autonomy 1–8
        new(1, Autonomy, Reverse: false, "AssessmentV2.Values.Q01"),
        new(2, Autonomy, Reverse: false, "AssessmentV2.Values.Q02"),
        new(3, Autonomy, Reverse: true, "AssessmentV2.Values.Q03"),
        new(4, Autonomy, Reverse: false, "AssessmentV2.Values.Q04"),
        new(5, Autonomy, Reverse: true, "AssessmentV2.Values.Q05"),
        new(6, Autonomy, Reverse: false, "AssessmentV2.Values.Q06"),
        new(7, Autonomy, Reverse: true, "AssessmentV2.Values.Q07"),
        new(8, Autonomy, Reverse: true, "AssessmentV2.Values.Q08"),
        // Connection 9–16
        new(9, Connection, Reverse: false, "AssessmentV2.Values.Q09"),
        new(10, Connection, Reverse: false, "AssessmentV2.Values.Q10"),
        new(11, Connection, Reverse: true, "AssessmentV2.Values.Q11"),
        new(12, Connection, Reverse: false, "AssessmentV2.Values.Q12"),
        new(13, Connection, Reverse: true, "AssessmentV2.Values.Q13"),
        new(14, Connection, Reverse: false, "AssessmentV2.Values.Q14"),
        new(15, Connection, Reverse: true, "AssessmentV2.Values.Q15"),
        new(16, Connection, Reverse: true, "AssessmentV2.Values.Q16"),
        // Achievement 17–24
        new(17, Achievement, Reverse: false, "AssessmentV2.Values.Q17"),
        new(18, Achievement, Reverse: false, "AssessmentV2.Values.Q18"),
        new(19, Achievement, Reverse: true, "AssessmentV2.Values.Q19"),
        new(20, Achievement, Reverse: false, "AssessmentV2.Values.Q20"),
        new(21, Achievement, Reverse: true, "AssessmentV2.Values.Q21"),
        new(22, Achievement, Reverse: false, "AssessmentV2.Values.Q22"),
        new(23, Achievement, Reverse: true, "AssessmentV2.Values.Q23"),
        new(24, Achievement, Reverse: true, "AssessmentV2.Values.Q24"),
        // Stability 25–32
        new(25, Stability, Reverse: false, "AssessmentV2.Values.Q25"),
        new(26, Stability, Reverse: false, "AssessmentV2.Values.Q26"),
        new(27, Stability, Reverse: true, "AssessmentV2.Values.Q27"),
        new(28, Stability, Reverse: false, "AssessmentV2.Values.Q28"),
        new(29, Stability, Reverse: true, "AssessmentV2.Values.Q29"),
        new(30, Stability, Reverse: false, "AssessmentV2.Values.Q30"),
        new(31, Stability, Reverse: true, "AssessmentV2.Values.Q31"),
        new(32, Stability, Reverse: true, "AssessmentV2.Values.Q32"),
        // Impact 33–40
        new(33, Impact, Reverse: false, "AssessmentV2.Values.Q33"),
        new(34, Impact, Reverse: false, "AssessmentV2.Values.Q34"),
        new(35, Impact, Reverse: true, "AssessmentV2.Values.Q35"),
        new(36, Impact, Reverse: false, "AssessmentV2.Values.Q36"),
        new(37, Impact, Reverse: true, "AssessmentV2.Values.Q37"),
        new(38, Impact, Reverse: false, "AssessmentV2.Values.Q38"),
        new(39, Impact, Reverse: true, "AssessmentV2.Values.Q39"),
        new(40, Impact, Reverse: true, "AssessmentV2.Values.Q40")
    ];

    public static bool IsValidAnswer(int value) => LikertAnswerJson.IsValidAnswer(value);

    public static string? ValidateAnswers(IReadOnlyDictionary<int, int> answers, bool requireComplete)
    {
        foreach (var (id, value) in answers)
        {
            if (id is < 1 or > QuestionCount)
            {
                return "Onbekend vraagnummer in de waardenscan.";
            }

            if (!IsValidAnswer(value))
            {
                return "Elk antwoord moet tussen 1 (helemaal oneens) en 5 (helemaal eens) liggen.";
            }
        }

        if (requireComplete && !IsComplete(answers))
        {
            return "Beantwoord alle 40 stellingen om de waardenscan af te ronden.";
        }

        return null;
    }

    public static bool IsComplete(IReadOnlyDictionary<int, int> answers)
    {
        for (var i = 1; i <= QuestionCount; i++)
        {
            if (!answers.TryGetValue(i, out var v) || !IsValidAnswer(v))
            {
                return false;
            }
        }

        return true;
    }

    public static IReadOnlyDictionary<int, int> ParseAnswers(string? json)
        => LikertAnswerJson.Parse(json, QuestionCount);

    public static string SerializeAnswers(IReadOnlyDictionary<int, int> answers)
        => LikertAnswerJson.Serialize(answers, QuestionCount);

    public static SchwartzValuesScores? Score(IReadOnlyDictionary<int, int> answers)
        => SchwartzValuesCatalog.Score(answers, Questions);

    public static SchwartzValuesScores? Score(
        IReadOnlyDictionary<int, int> answers,
        IEnumerable<CompetencyQuestion> items)
        => SchwartzValuesCatalog.Score(answers, items);

    public static IReadOnlyList<string> DeriveMatchTags(SchwartzValuesScores scores)
        => SchwartzValuesCatalog.DeriveMatchTags(scores);

    public static string SerializeTags(IEnumerable<string> tags)
        => SchwartzValuesCatalog.SerializeTags(tags);

    public static IReadOnlyList<string> ParseTags(string? json)
        => SchwartzValuesCatalog.ParseTags(json);

    public static IReadOnlyList<CompetencyQuestion> QuestionsFor(string category)
        => Questions.Where(q => string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
}
