using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Free culture &amp; personality Quick-Scan (18 Likert items).
/// Combines workplace culture preferences (autonomy↔hierarchy, informal↔formal, …)
/// with abbreviated Big Five / IPIP-style work facets for matching — without DISC jargon.
/// </summary>
public static class CulturePersonalityCatalog
{
    public const int QuestionCount = 18;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;

    // Culture dimensions (candidate preference / company objective culture).
    public const string Autonomy = "Autonomy";
    public const string Informal = "Informal";
    public const string Collaboration = "Collaboration";
    public const string Flexibility = "Flexibility";
    public const string Innovation = "Innovation";
    public const string PeopleFirst = "PeopleFirst";

    // Abbreviated Big Five work facets (IPIP-inspired workplace wording).
    public const string Openness = "Openness";
    public const string Conscientiousness = "Conscientiousness";
    public const string Extraversion = "Extraversion";
    public const string Agreeableness = "Agreeableness";
    public const string EmotionalStability = "EmotionalStability";

    public static readonly string[] CultureDimensionCodes =
    [
        Autonomy, Informal, Collaboration, Flexibility, Innovation, PeopleFirst
    ];

    public static readonly string[] PersonalityFacetCodes =
    [
        Openness, Conscientiousness, Extraversion, Agreeableness, EmotionalStability
    ];

    public static readonly string[] CategoryCodes =
    [
        Autonomy, Informal, Collaboration, Flexibility, Innovation, PeopleFirst,
        Openness, Conscientiousness, Extraversion, Agreeableness, EmotionalStability
    ];

    /// <summary>
    /// 18 items: 2 per culture dim (12) + ~1 per Big Five facet with one extra openness (6).
    /// </summary>
    public static readonly IReadOnlyList<CompetencyQuestion> Questions =
    [
        // Culture — Autonomy vs hierarchy
        new(1, Autonomy, Reverse: false, "CultureScan.Q01"),
        new(2, Autonomy, Reverse: true, "CultureScan.Q02"),
        // Informal vs formal
        new(3, Informal, Reverse: false, "CultureScan.Q03"),
        new(4, Informal, Reverse: true, "CultureScan.Q04"),
        // Collaboration vs independence
        new(5, Collaboration, Reverse: false, "CultureScan.Q05"),
        new(6, Collaboration, Reverse: true, "CultureScan.Q06"),
        // Flexibility vs structure
        new(7, Flexibility, Reverse: false, "CultureScan.Q07"),
        new(8, Flexibility, Reverse: true, "CultureScan.Q08"),
        // Innovation vs stability
        new(9, Innovation, Reverse: false, "CultureScan.Q09"),
        new(10, Innovation, Reverse: true, "CultureScan.Q10"),
        // People-first vs results-first
        new(11, PeopleFirst, Reverse: false, "CultureScan.Q11"),
        new(12, PeopleFirst, Reverse: true, "CultureScan.Q12"),
        // Big Five workplace facets (IPIP-style)
        new(13, Openness, Reverse: false, "CultureScan.Q13"),
        new(14, Conscientiousness, Reverse: false, "CultureScan.Q14"),
        new(15, Extraversion, Reverse: false, "CultureScan.Q15"),
        new(16, Agreeableness, Reverse: false, "CultureScan.Q16"),
        new(17, EmotionalStability, Reverse: false, "CultureScan.Q17"),
        new(18, Openness, Reverse: true, "CultureScan.Q18")
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
            return "Beantwoord alle 18 stellingen om de cultuurscan af te ronden.";
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
    {
        var scored = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in Questions)
        {
            if (!answers.TryGetValue(q.Id, out var raw) || !IsValidAnswer(raw))
            {
                continue;
            }

            var value = q.Reverse ? LikertMax + LikertMin - raw : raw;
            if (!scored.TryGetValue(q.Category, out var list))
            {
                list = [];
                scored[q.Category] = list;
            }

            list.Add(value);
        }

        if (scored.Count == 0)
        {
            return null;
        }

        int? Pct(string code)
            => scored.TryGetValue(code, out var list) && list.Count > 0
                ? LikertAnswerJson.ToPercent(list)
                : null;

        return new CulturePersonalityScores(
            Autonomy: Pct(Autonomy),
            Informal: Pct(Informal),
            Collaboration: Pct(Collaboration),
            Flexibility: Pct(Flexibility),
            Innovation: Pct(Innovation),
            PeopleFirst: Pct(PeopleFirst),
            Openness: Pct(Openness),
            Conscientiousness: Pct(Conscientiousness),
            Extraversion: Pct(Extraversion),
            Agreeableness: Pct(Agreeableness),
            EmotionalStability: Pct(EmotionalStability));
    }

    public static IReadOnlyList<string> DeriveMatchTags(CulturePersonalityScores scores)
    {
        var tags = new List<string>();
        void Add(string code, int? value, string high, string low)
        {
            if (value is null)
            {
                return;
            }

            tags.Add(value >= 60 ? high : low);
        }

        Add(Autonomy, scores.Autonomy, "zelfstandig", "duidelijke-kaders");
        Add(Informal, scores.Informal, "informeel", "formeel");
        Add(Collaboration, scores.Collaboration, "samenwerken", "zelfstandig-werken");
        Add(Flexibility, scores.Flexibility, "flexibel", "gestructureerd");
        Add(Innovation, scores.Innovation, "vernieuwend", "stabiel");
        Add(PeopleFirst, scores.PeopleFirst, "mensgericht", "resultaatgericht");
        return tags;
    }

    public static string SerializeTags(IEnumerable<string> tags)
        => LikertAnswerJson.SerializeTags(tags);

    public static IReadOnlyList<string> ParseTags(string? json)
        => LikertAnswerJson.ParseTags(json);

    public static IReadOnlyList<CompetencyQuestion> QuestionsFor(string category)
        => Questions.Where(q => string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Short Dutch label for UI / stories — no DISC or Big Five jargon.</summary>
    public static string EverydayLabel(string code) => code switch
    {
        Autonomy => "zelfstandig werken",
        Informal => "informele sfeer",
        Collaboration => "samenwerken",
        Flexibility => "flexibel meebewegen",
        Innovation => "nieuwe dingen proberen",
        PeopleFirst => "mensen voorop zetten",
        Openness => "openstaan voor nieuw",
        Conscientiousness => "netjes en betrouwbaar werken",
        Extraversion => "energie van mensen om je heen",
        Agreeableness => "prettig samen optrekken",
        EmotionalStability => "kalm blijven als het druk is",
        _ => "hoe jij graag werkt"
    };
}

/// <summary>0–100 scores per culture dimension and IPIP-style personality facet.</summary>
public sealed record CulturePersonalityScores(
    int? Autonomy = null,
    int? Informal = null,
    int? Collaboration = null,
    int? Flexibility = null,
    int? Innovation = null,
    int? PeopleFirst = null,
    int? Openness = null,
    int? Conscientiousness = null,
    int? Extraversion = null,
    int? Agreeableness = null,
    int? EmotionalStability = null)
{
    public bool IsComplete =>
        Autonomy is not null
        && Informal is not null
        && Collaboration is not null
        && Flexibility is not null
        && Innovation is not null
        && PeopleFirst is not null
        && Openness is not null
        && Conscientiousness is not null
        && Extraversion is not null
        && Agreeableness is not null
        && EmotionalStability is not null;

    public int Get(string code) => code switch
    {
        CulturePersonalityCatalog.Autonomy => Autonomy ?? 55,
        CulturePersonalityCatalog.Informal => Informal ?? 55,
        CulturePersonalityCatalog.Collaboration => Collaboration ?? 55,
        CulturePersonalityCatalog.Flexibility => Flexibility ?? 55,
        CulturePersonalityCatalog.Innovation => Innovation ?? 55,
        CulturePersonalityCatalog.PeopleFirst => PeopleFirst ?? 55,
        CulturePersonalityCatalog.Openness => Openness ?? 55,
        CulturePersonalityCatalog.Conscientiousness => Conscientiousness ?? 55,
        CulturePersonalityCatalog.Extraversion => Extraversion ?? 55,
        CulturePersonalityCatalog.Agreeableness => Agreeableness ?? 55,
        CulturePersonalityCatalog.EmotionalStability => EmotionalStability ?? 55,
        _ => 55
    };
}
