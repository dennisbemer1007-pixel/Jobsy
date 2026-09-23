using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Free career Quick-Scan: 25 Likert items based on Holland RIASEC occupational interests.
/// Output: 0–100% per type, Holland-code (top 3), tags for vacancy matching.
/// </summary>
public static class CareerTestCatalog
{
    public const int QuestionCount = 25;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;

    public const string Realistic = CompetencyTestCatalog.RiasecRealistic;
    public const string Investigative = CompetencyTestCatalog.RiasecInvestigative;
    public const string Artistic = CompetencyTestCatalog.RiasecArtistic;
    public const string Social = CompetencyTestCatalog.RiasecSocial;
    public const string Enterprising = CompetencyTestCatalog.RiasecEnterprising;
    public const string Conventional = CompetencyTestCatalog.RiasecConventional;

    public static readonly string[] RiasecCodes = CompetencyTestCatalog.RiasecCodes.ToArray();

    public static readonly IReadOnlyDictionary<string, char> HollandLetter = new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase)
    {
        [Realistic] = 'R',
        [Investigative] = 'I',
        [Artistic] = 'A',
        [Social] = 'S',
        [Enterprising] = 'E',
        [Conventional] = 'C'
    };

    /// <summary>
    /// 25 compact items: Realistic 5 (Westland practical work) + 4 each for I/A/S/E/C.
    /// </summary>
    public static readonly IReadOnlyList<CareerQuestion> Questions =
    [
        new(1, Realistic, Reverse: false, "Career.Q01"),
        new(2, Realistic, Reverse: false, "Career.Q02"),
        new(3, Realistic, Reverse: false, "Career.Q03"),
        new(4, Realistic, Reverse: false, "Career.Q04"),
        new(5, Realistic, Reverse: true, "Career.Q05"),
        new(6, Investigative, Reverse: false, "Career.Q06"),
        new(7, Investigative, Reverse: false, "Career.Q07"),
        new(8, Investigative, Reverse: false, "Career.Q08"),
        new(9, Investigative, Reverse: true, "Career.Q09"),
        new(10, Artistic, Reverse: false, "Career.Q10"),
        new(11, Artistic, Reverse: false, "Career.Q11"),
        new(12, Artistic, Reverse: false, "Career.Q12"),
        new(13, Artistic, Reverse: true, "Career.Q13"),
        new(14, Social, Reverse: false, "Career.Q14"),
        new(15, Social, Reverse: false, "Career.Q15"),
        new(16, Social, Reverse: false, "Career.Q16"),
        new(17, Social, Reverse: true, "Career.Q17"),
        new(18, Enterprising, Reverse: false, "Career.Q18"),
        new(19, Enterprising, Reverse: false, "Career.Q19"),
        new(20, Enterprising, Reverse: false, "Career.Q20"),
        new(21, Enterprising, Reverse: true, "Career.Q21"),
        new(22, Conventional, Reverse: false, "Career.Q22"),
        new(23, Conventional, Reverse: false, "Career.Q23"),
        new(24, Conventional, Reverse: false, "Career.Q24"),
        new(25, Conventional, Reverse: true, "Career.Q25")
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
            return "Beantwoord alle 25 vragen om de beroepentest af te ronden.";
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
        => LikertAnswerJson.SerializeTags(tags);

    public static IReadOnlyList<string> ParseTagsJson(string? json)
        => LikertAnswerJson.ParseTags(json);

    public static RiasecScores? Score(IReadOnlyDictionary<int, int> answers)
    {
        int? ScoreType(string code)
        {
            var items = Questions.Where(q => q.Category == code).ToList();
            var scored = new List<int>();
            foreach (var question in items)
            {
                if (!answers.TryGetValue(question.Id, out var raw) || !IsValidAnswer(raw))
                {
                    continue;
                }

                scored.Add(question.Reverse ? LikertMax + LikertMin - raw : raw);
            }

            return scored.Count == 0 ? null : LikertAnswerJson.ToPercent(scored);
        }

        var r = ScoreType(Realistic);
        var i = ScoreType(Investigative);
        var a = ScoreType(Artistic);
        var s = ScoreType(Social);
        var e = ScoreType(Enterprising);
        var c = ScoreType(Conventional);
        if (r is null && i is null && a is null && s is null && e is null && c is null)
        {
            return null;
        }

        return new RiasecScores(r, i, a, s, e, c);
    }

    public static IReadOnlyList<string> DeriveRiasecTags(RiasecScores? scores)
    {
        if (scores is null)
        {
            return [];
        }

        var ranked = RiasecCodes
            .Select(code => (Code: code, Value: scores.TryGet(code) ?? 0))
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .ToList();

        var tags = ranked.Where(x => x.Value >= 70).Select(x => x.Code).Take(3).ToList();
        if (tags.Count == 0)
        {
            tags = ranked.Where(x => x.Value > 0).Take(2).Select(x => x.Code).ToList();
        }

        return tags;
    }

    public static string HollandCode(RiasecScores? scores)
    {
        if (scores is null)
        {
            return "";
        }

        return string.Concat(
            RiasecCodes
                .Select(code => (Letter: HollandLetter[code], Value: scores.TryGet(code) ?? 0))
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Letter)
                .Take(3)
                .Where(x => x.Value > 0)
                .Select(x => x.Letter));
    }

    public static RiasecScores? CompletedScoresOrNull(
        string? status,
        int? realistic,
        int? investigative,
        int? artistic,
        int? social,
        int? enterprising,
        int? conventional)
    {
        if (!CandidateCompetencyStatuses.IsCompleted(status)
            || realistic is null
            || investigative is null
            || artistic is null
            || social is null
            || enterprising is null
            || conventional is null)
        {
            return null;
        }

        return new RiasecScores(realistic, investigative, artistic, social, enterprising, conventional);
    }

    /// <summary>
    /// Reconstructs RIASEC tags from the legacy combined Quick-Scan (Q21–Q25 = R/I/A/S/E).
    /// </summary>
    public static IReadOnlyList<string> DeriveLegacyCompactRiasecTags(IReadOnlyDictionary<int, int> answers)
    {
        var map = new Dictionary<int, string>
        {
            [21] = Realistic,
            [22] = Investigative,
            [23] = Artistic,
            [24] = Social,
            [25] = Enterprising
        };

        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, code) in map)
        {
            if (answers.TryGetValue(id, out var value) && IsValidAnswer(value))
            {
                scores[code] = value;
            }
        }

        var tags = scores
            .Where(kv => kv.Value >= 4)
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => kv.Key)
            .Take(3)
            .ToList();

        if (tags.Count == 0 && scores.Count > 0)
        {
            tags = scores
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                .Take(2)
                .Select(kv => kv.Key)
                .ToList();
        }

        return tags;
    }
}

public sealed record CareerQuestion(
    int Id,
    string Category,
    bool Reverse,
    string TextKey);

/// <summary>Holland RIASEC interests as 0–100 percentages.</summary>
public sealed record RiasecScores(
    int? Realistic,
    int? Investigative,
    int? Artistic,
    int? Social,
    int? Enterprising,
    int? Conventional)
{
    public bool IsComplete =>
        Realistic is not null
        && Investigative is not null
        && Artistic is not null
        && Social is not null
        && Enterprising is not null
        && Conventional is not null;

    public int Get(string category) => TryGet(category) ?? 0;

    public int? TryGet(string category) => category switch
    {
        CareerTestCatalog.Realistic => Realistic,
        CareerTestCatalog.Investigative => Investigative,
        CareerTestCatalog.Artistic => Artistic,
        CareerTestCatalog.Social => Social,
        CareerTestCatalog.Enterprising => Enterprising,
        CareerTestCatalog.Conventional => Conventional,
        _ => null
    };
}
