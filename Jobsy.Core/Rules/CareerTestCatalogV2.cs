using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Quick-Scan v2 career interests: 48 Likert items (8 per Holland RIASEC type).
/// </summary>
public static class CareerTestCatalogV2
{
    public const int QuestionCount = 48;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;
    public const int CategoryQuestionCount = 8;

    public const string Realistic = CareerTestCatalog.Realistic;
    public const string Investigative = CareerTestCatalog.Investigative;
    public const string Artistic = CareerTestCatalog.Artistic;
    public const string Social = CareerTestCatalog.Social;
    public const string Enterprising = CareerTestCatalog.Enterprising;
    public const string Conventional = CareerTestCatalog.Conventional;

    public static readonly string[] RiasecCodes = CareerTestCatalog.RiasecCodes;
    public static readonly IReadOnlyDictionary<string, char> HollandLetter = CareerTestCatalog.HollandLetter;

    public static readonly IReadOnlyList<CareerQuestion> Questions =
    [
        // Realistic 1–8
        new(1, Realistic, Reverse: false, "AssessmentV2.Career.Q01"),
        new(2, Realistic, Reverse: false, "AssessmentV2.Career.Q02"),
        new(3, Realistic, Reverse: false, "AssessmentV2.Career.Q03"),
        new(4, Realistic, Reverse: false, "AssessmentV2.Career.Q04"),
        new(5, Realistic, Reverse: true, "AssessmentV2.Career.Q05"),
        new(6, Realistic, Reverse: false, "AssessmentV2.Career.Q06"),
        new(7, Realistic, Reverse: true, "AssessmentV2.Career.Q07"),
        new(8, Realistic, Reverse: true, "AssessmentV2.Career.Q08"),
        // Investigative 9–16
        new(9, Investigative, Reverse: false, "AssessmentV2.Career.Q09"),
        new(10, Investigative, Reverse: false, "AssessmentV2.Career.Q10"),
        new(11, Investigative, Reverse: false, "AssessmentV2.Career.Q11"),
        new(12, Investigative, Reverse: true, "AssessmentV2.Career.Q12"),
        new(13, Investigative, Reverse: false, "AssessmentV2.Career.Q13"),
        new(14, Investigative, Reverse: true, "AssessmentV2.Career.Q14"),
        new(15, Investigative, Reverse: false, "AssessmentV2.Career.Q15"),
        new(16, Investigative, Reverse: true, "AssessmentV2.Career.Q16"),
        // Artistic 17–24
        new(17, Artistic, Reverse: false, "AssessmentV2.Career.Q17"),
        new(18, Artistic, Reverse: false, "AssessmentV2.Career.Q18"),
        new(19, Artistic, Reverse: false, "AssessmentV2.Career.Q19"),
        new(20, Artistic, Reverse: true, "AssessmentV2.Career.Q20"),
        new(21, Artistic, Reverse: false, "AssessmentV2.Career.Q21"),
        new(22, Artistic, Reverse: true, "AssessmentV2.Career.Q22"),
        new(23, Artistic, Reverse: false, "AssessmentV2.Career.Q23"),
        new(24, Artistic, Reverse: true, "AssessmentV2.Career.Q24"),
        // Social 25–32
        new(25, Social, Reverse: false, "AssessmentV2.Career.Q25"),
        new(26, Social, Reverse: false, "AssessmentV2.Career.Q26"),
        new(27, Social, Reverse: false, "AssessmentV2.Career.Q27"),
        new(28, Social, Reverse: true, "AssessmentV2.Career.Q28"),
        new(29, Social, Reverse: false, "AssessmentV2.Career.Q29"),
        new(30, Social, Reverse: true, "AssessmentV2.Career.Q30"),
        new(31, Social, Reverse: false, "AssessmentV2.Career.Q31"),
        new(32, Social, Reverse: true, "AssessmentV2.Career.Q32"),
        // Enterprising 33–40
        new(33, Enterprising, Reverse: false, "AssessmentV2.Career.Q33"),
        new(34, Enterprising, Reverse: false, "AssessmentV2.Career.Q34"),
        new(35, Enterprising, Reverse: false, "AssessmentV2.Career.Q35"),
        new(36, Enterprising, Reverse: true, "AssessmentV2.Career.Q36"),
        new(37, Enterprising, Reverse: false, "AssessmentV2.Career.Q37"),
        new(38, Enterprising, Reverse: true, "AssessmentV2.Career.Q38"),
        new(39, Enterprising, Reverse: false, "AssessmentV2.Career.Q39"),
        new(40, Enterprising, Reverse: true, "AssessmentV2.Career.Q40"),
        // Conventional 41–48
        new(41, Conventional, Reverse: false, "AssessmentV2.Career.Q41"),
        new(42, Conventional, Reverse: false, "AssessmentV2.Career.Q42"),
        new(43, Conventional, Reverse: false, "AssessmentV2.Career.Q43"),
        new(44, Conventional, Reverse: true, "AssessmentV2.Career.Q44"),
        new(45, Conventional, Reverse: false, "AssessmentV2.Career.Q45"),
        new(46, Conventional, Reverse: true, "AssessmentV2.Career.Q46"),
        new(47, Conventional, Reverse: false, "AssessmentV2.Career.Q47"),
        new(48, Conventional, Reverse: true, "AssessmentV2.Career.Q48")
    ];

    public static bool IsValidAnswer(int value) => LikertAnswerJson.IsValidAnswer(value);

    public static string? ValidateAnswers(IReadOnlyDictionary<int, int> answers, bool requireComplete)
    {
        foreach (var (id, value) in answers)
        {
            if (id is < 1 or > QuestionCount)
            {
                return "Onbekend vraagnummer in de beroepentest.";
            }

            if (!IsValidAnswer(value))
            {
                return "Elk antwoord moet tussen 1 (helemaal oneens) en 5 (helemaal eens) liggen.";
            }
        }

        if (requireComplete && !IsComplete(answers))
        {
            return "Beantwoord alle 48 vragen om de beroepentest af te ronden.";
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

    public static string SerializeTags(IEnumerable<string> tags)
        => CareerTestCatalog.SerializeTags(tags);

    public static IReadOnlyList<string> ParseTagsJson(string? json)
        => CareerTestCatalog.ParseTagsJson(json);

    public static RiasecScores? Score(IReadOnlyDictionary<int, int> answers)
        => CareerTestCatalog.Score(answers, Questions);

    public static RiasecScores? Score(
        IReadOnlyDictionary<int, int> answers,
        IEnumerable<CareerQuestion> items)
        => CareerTestCatalog.Score(answers, items);

    public static IReadOnlyList<string> DeriveRiasecTags(RiasecScores? scores)
        => CareerTestCatalog.DeriveRiasecTags(scores);

    public static string HollandCode(RiasecScores? scores)
        => CareerTestCatalog.HollandCode(scores);
}
