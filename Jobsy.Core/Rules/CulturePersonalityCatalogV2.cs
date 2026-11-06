using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Quick-Scan v2 culture &amp; personality: 48 Likert items
/// (12 dimensions × 4 items; reverses balanced per dimension).
/// </summary>
public static class CulturePersonalityCatalogV2
{
    public const int QuestionCount = 48;
    public const int ItemsPerDimension = 4;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;

    public const string Autonomy = CulturePersonalityCatalog.Autonomy;
    public const string Informal = CulturePersonalityCatalog.Informal;
    public const string Collaboration = CulturePersonalityCatalog.Collaboration;
    public const string Flexibility = CulturePersonalityCatalog.Flexibility;
    public const string Innovation = CulturePersonalityCatalog.Innovation;
    public const string PeopleFirst = CulturePersonalityCatalog.PeopleFirst;

    public const string Openness = CulturePersonalityCatalog.Openness;
    public const string Conscientiousness = CulturePersonalityCatalog.Conscientiousness;
    public const string Extraversion = CulturePersonalityCatalog.Extraversion;
    public const string Agreeableness = CulturePersonalityCatalog.Agreeableness;
    public const string EmotionalStability = CulturePersonalityCatalog.EmotionalStability;

    /// <summary>Sixth personality facet (items 45–48); scored together with <see cref="Openness"/>.</summary>
    public const string Curiosity = "Curiosity";

    public static readonly string[] CultureDimensionCodes = CulturePersonalityCatalog.CultureDimensionCodes;

    public static readonly string[] PersonalityFacetCodes =
    [
        Openness, Conscientiousness, Extraversion, Agreeableness, EmotionalStability, Curiosity
    ];

    public static readonly string[] CategoryCodes =
    [
        .. CultureDimensionCodes,
        .. PersonalityFacetCodes
    ];

    public static readonly IReadOnlyList<CompetencyQuestion> Questions = BuildQuestions();

    private static IReadOnlyList<CompetencyQuestion> BuildQuestions()
    {
        var rows = new (string Category, bool Reverse, string Key)[]
        {
            // Autonomy 1–4
            (Autonomy, false, "AssessmentV2.Culture.Q01"),
            (Autonomy, true, "AssessmentV2.Culture.Q02"),
            (Autonomy, false, "AssessmentV2.Culture.Q25"),
            (Autonomy, true, "AssessmentV2.Culture.Q26"),
            // Informal 5–8
            (Informal, false, "AssessmentV2.Culture.Q03"),
            (Informal, true, "AssessmentV2.Culture.Q04"),
            (Informal, false, "AssessmentV2.Culture.Q27"),
            (Informal, true, "AssessmentV2.Culture.Q28"),
            // Collaboration 9–12
            (Collaboration, false, "AssessmentV2.Culture.Q05"),
            (Collaboration, true, "AssessmentV2.Culture.Q06"),
            (Collaboration, false, "AssessmentV2.Culture.Q29"),
            (Collaboration, true, "AssessmentV2.Culture.Q30"),
            // Flexibility 13–16
            (Flexibility, false, "AssessmentV2.Culture.Q07"),
            (Flexibility, true, "AssessmentV2.Culture.Q08"),
            (Flexibility, false, "AssessmentV2.Culture.Q31"),
            (Flexibility, true, "AssessmentV2.Culture.Q32"),
            // Innovation 17–20
            (Innovation, false, "AssessmentV2.Culture.Q09"),
            (Innovation, true, "AssessmentV2.Culture.Q10"),
            (Innovation, false, "AssessmentV2.Culture.Q33"),
            (Innovation, true, "AssessmentV2.Culture.Q34"),
            // PeopleFirst 21–24
            (PeopleFirst, false, "AssessmentV2.Culture.Q11"),
            (PeopleFirst, true, "AssessmentV2.Culture.Q12"),
            (PeopleFirst, false, "AssessmentV2.Culture.Q35"),
            (PeopleFirst, true, "AssessmentV2.Culture.Q36"),
            // Openness 25–28 (catalog ids 25–28 map to Q13,Q14,Q37,Q38)
            (Openness, false, "AssessmentV2.Culture.Q13"),
            (Openness, true, "AssessmentV2.Culture.Q14"),
            (Openness, false, "AssessmentV2.Culture.Q37"),
            (Openness, true, "AssessmentV2.Culture.Q38"),
            // Conscientiousness 29–32
            (Conscientiousness, false, "AssessmentV2.Culture.Q15"),
            (Conscientiousness, true, "AssessmentV2.Culture.Q16"),
            (Conscientiousness, false, "AssessmentV2.Culture.Q39"),
            (Conscientiousness, true, "AssessmentV2.Culture.Q40"),
            // Extraversion 33–36
            (Extraversion, false, "AssessmentV2.Culture.Q17"),
            (Extraversion, true, "AssessmentV2.Culture.Q18"),
            (Extraversion, false, "AssessmentV2.Culture.Q41"),
            (Extraversion, true, "AssessmentV2.Culture.Q42"),
            // Agreeableness 37–40
            (Agreeableness, false, "AssessmentV2.Culture.Q19"),
            (Agreeableness, true, "AssessmentV2.Culture.Q20"),
            (Agreeableness, false, "AssessmentV2.Culture.Q43"),
            (Agreeableness, true, "AssessmentV2.Culture.Q44"),
            // EmotionalStability 41–44
            (EmotionalStability, false, "AssessmentV2.Culture.Q21"),
            (EmotionalStability, true, "AssessmentV2.Culture.Q22"),
            (EmotionalStability, false, "AssessmentV2.Culture.Q45"),
            (EmotionalStability, true, "AssessmentV2.Culture.Q46"),
            // Curiosity 45–48
            (Curiosity, false, "AssessmentV2.Culture.Q23"),
            (Curiosity, true, "AssessmentV2.Culture.Q24"),
            (Curiosity, false, "AssessmentV2.Culture.Q47"),
            (Curiosity, true, "AssessmentV2.Culture.Q48"),
        };

        var list = new List<CompetencyQuestion>(QuestionCount);
        for (var i = 0; i < rows.Length; i++)
        {
            var (cat, rev, key) = rows[i];
            list.Add(new CompetencyQuestion(i + 1, cat, rev, key));
        }

        return list;
    }

    public static bool IsValidAnswer(int value) => LikertAnswerJson.IsValidAnswer(value);

    public static string? ValidateAnswers(IReadOnlyDictionary<int, int> answers, bool requireComplete)
    {
        foreach (var (id, value) in answers)
        {
            if (id is < 1 or > QuestionCount)
            {
                return "Onbekend vraagnummer in de cultuurscan.";
            }

            if (!IsValidAnswer(value))
            {
                return "Elk antwoord moet tussen 1 (helemaal oneens) en 5 (helemaal eens) liggen.";
            }
        }

        if (requireComplete && !IsComplete(answers))
        {
            return "Beantwoord alle 48 stellingen om de cultuurscan af te ronden.";
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

    public static CulturePersonalityScores? Score(IReadOnlyDictionary<int, int> answers)
        => Score(answers, Questions);

    public static CulturePersonalityScores? Score(
        IReadOnlyDictionary<int, int> answers,
        IEnumerable<CompetencyQuestion> items)
    {
        var list = items
            .Select(q => (q.Id, MapScoringCategory(q.Category), q.Reverse))
            .ToList();

        var autonomy = LikertCategoryScorer.ScoreCategory(list, answers, Autonomy);
        var informal = LikertCategoryScorer.ScoreCategory(list, answers, Informal);
        var collaboration = LikertCategoryScorer.ScoreCategory(list, answers, Collaboration);
        var flexibility = LikertCategoryScorer.ScoreCategory(list, answers, Flexibility);
        var innovation = LikertCategoryScorer.ScoreCategory(list, answers, Innovation);
        var peopleFirst = LikertCategoryScorer.ScoreCategory(list, answers, PeopleFirst);
        var openness = LikertCategoryScorer.ScoreCategory(list, answers, Openness);
        var conscientiousness = LikertCategoryScorer.ScoreCategory(list, answers, Conscientiousness);
        var extraversion = LikertCategoryScorer.ScoreCategory(list, answers, Extraversion);
        var agreeableness = LikertCategoryScorer.ScoreCategory(list, answers, Agreeableness);
        var emotionalStability = LikertCategoryScorer.ScoreCategory(list, answers, EmotionalStability);

        if (autonomy is null && informal is null && collaboration is null && flexibility is null
            && innovation is null && peopleFirst is null && openness is null && conscientiousness is null
            && extraversion is null && agreeableness is null && emotionalStability is null)
        {
            return null;
        }

        return new CulturePersonalityScores(
            Autonomy: autonomy,
            Informal: informal,
            Collaboration: collaboration,
            Flexibility: flexibility,
            Innovation: innovation,
            PeopleFirst: peopleFirst,
            Openness: openness,
            Conscientiousness: conscientiousness,
            Extraversion: extraversion,
            Agreeableness: agreeableness,
            EmotionalStability: emotionalStability);
    }

    private static string MapScoringCategory(string category)
        => string.Equals(category, Curiosity, StringComparison.OrdinalIgnoreCase)
            ? Openness
            : category;

    public static IReadOnlyList<string> DeriveMatchTags(CulturePersonalityScores scores)
        => CulturePersonalityCatalog.DeriveMatchTags(scores);

    public static string SerializeTags(IEnumerable<string> tags)
        => CulturePersonalityCatalog.SerializeTags(tags);

    public static IReadOnlyList<string> ParseTags(string? json)
        => CulturePersonalityCatalog.ParseTags(json);

    public static IReadOnlyList<CompetencyQuestion> QuestionsFor(string category)
        => Questions.Where(q => string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
}
