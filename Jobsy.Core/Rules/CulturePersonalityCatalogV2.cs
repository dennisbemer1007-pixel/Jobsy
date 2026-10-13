using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Quick-Scan v2 culture &amp; personality: 24 Likert items
/// (6 culture dimensions × 2 + 6 personality facets × 2, reverses on opposing poles).
/// </summary>
public static class CulturePersonalityCatalogV2
{
    public const int QuestionCount = 24;
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

    /// <summary>Sixth personality facet (items 23–24); scored together with <see cref="Openness"/>.</summary>
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

    public static readonly IReadOnlyList<CompetencyQuestion> Questions =
    [
        new(1, Autonomy, Reverse: false, "AssessmentV2.Culture.Q01"),
        new(2, Autonomy, Reverse: true, "AssessmentV2.Culture.Q02"),
        new(3, Informal, Reverse: false, "AssessmentV2.Culture.Q03"),
        new(4, Informal, Reverse: true, "AssessmentV2.Culture.Q04"),
        new(5, Collaboration, Reverse: false, "AssessmentV2.Culture.Q05"),
        new(6, Collaboration, Reverse: true, "AssessmentV2.Culture.Q06"),
        new(7, Flexibility, Reverse: false, "AssessmentV2.Culture.Q07"),
        new(8, Flexibility, Reverse: true, "AssessmentV2.Culture.Q08"),
        new(9, Innovation, Reverse: false, "AssessmentV2.Culture.Q09"),
        new(10, Innovation, Reverse: true, "AssessmentV2.Culture.Q10"),
        new(11, PeopleFirst, Reverse: false, "AssessmentV2.Culture.Q11"),
        new(12, PeopleFirst, Reverse: true, "AssessmentV2.Culture.Q12"),
        new(13, Openness, Reverse: false, "AssessmentV2.Culture.Q13"),
        new(14, Openness, Reverse: true, "AssessmentV2.Culture.Q14"),
        new(15, Conscientiousness, Reverse: false, "AssessmentV2.Culture.Q15"),
        new(16, Conscientiousness, Reverse: true, "AssessmentV2.Culture.Q16"),
        new(17, Extraversion, Reverse: false, "AssessmentV2.Culture.Q17"),
        new(18, Extraversion, Reverse: true, "AssessmentV2.Culture.Q18"),
        new(19, Agreeableness, Reverse: false, "AssessmentV2.Culture.Q19"),
        new(20, Agreeableness, Reverse: true, "AssessmentV2.Culture.Q20"),
        new(21, EmotionalStability, Reverse: false, "AssessmentV2.Culture.Q21"),
        new(22, EmotionalStability, Reverse: true, "AssessmentV2.Culture.Q22"),
        new(23, Curiosity, Reverse: false, "AssessmentV2.Culture.Q23"),
        new(24, Curiosity, Reverse: true, "AssessmentV2.Culture.Q24")
    ];

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
            return "Beantwoord alle 24 stellingen om de cultuurscan af te ronden.";
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
