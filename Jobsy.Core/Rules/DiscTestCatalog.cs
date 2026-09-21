using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Free DISC Quick-Scan: 25 Likert items on workplace behaviour (D, I, S, C).
/// Candidate-facing Functie-Fit copy uses everyday Dutch, not the letters.
/// </summary>
public static class DiscTestCatalog
{
    public const int QuestionCount = 25;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;
    public const int CategoryQuestionCount = 6;

    public const string Dominant = "Dominant";
    public const string Invloed = "Invloed";
    public const string Stabiel = "Stabiel";
    public const string Nauwkeurig = "Nauwkeurig";

    public static readonly string[] CategoryCodes =
    [
        Dominant,
        Invloed,
        Stabiel,
        Nauwkeurig
    ];

    public static readonly IReadOnlyList<CompetencyQuestion> Questions =
    [
        new(1, Dominant, Reverse: false, "Disc.Q01"),
        new(2, Dominant, Reverse: false, "Disc.Q02"),
        new(3, Dominant, Reverse: false, "Disc.Q03"),
        new(4, Dominant, Reverse: false, "Disc.Q04"),
        new(5, Dominant, Reverse: false, "Disc.Q05"),
        new(6, Dominant, Reverse: true, "Disc.Q06"),
        new(7, Dominant, Reverse: true, "Disc.Q07"),
        new(8, Invloed, Reverse: false, "Disc.Q08"),
        new(9, Invloed, Reverse: false, "Disc.Q09"),
        new(10, Invloed, Reverse: false, "Disc.Q10"),
        new(11, Invloed, Reverse: false, "Disc.Q11"),
        new(12, Invloed, Reverse: true, "Disc.Q12"),
        new(13, Invloed, Reverse: true, "Disc.Q13"),
        new(14, Stabiel, Reverse: false, "Disc.Q14"),
        new(15, Stabiel, Reverse: false, "Disc.Q15"),
        new(16, Stabiel, Reverse: false, "Disc.Q16"),
        new(17, Stabiel, Reverse: false, "Disc.Q17"),
        new(18, Stabiel, Reverse: true, "Disc.Q18"),
        new(19, Stabiel, Reverse: true, "Disc.Q19"),
        new(20, Nauwkeurig, Reverse: false, "Disc.Q20"),
        new(21, Nauwkeurig, Reverse: false, "Disc.Q21"),
        new(22, Nauwkeurig, Reverse: false, "Disc.Q22"),
        new(23, Nauwkeurig, Reverse: false, "Disc.Q23"),
        new(24, Nauwkeurig, Reverse: true, "Disc.Q24"),
        new(25, Nauwkeurig, Reverse: true, "Disc.Q25")
    ];

    public static bool IsValidAnswer(int value) => LikertAnswerJson.IsValidAnswer(value);

    public static string? ValidateAnswers(IReadOnlyDictionary<int, int> answers, bool requireComplete)
    {
        foreach (var (id, value) in answers)
        {
            if (id is < 1 or > QuestionCount)
            {
                return "Onbekend vraagnummer in de gedragsanalyse.";
            }

            if (!IsValidAnswer(value))
            {
                return "Elk antwoord moet tussen 1 (helemaal oneens) en 5 (helemaal eens) liggen.";
            }
        }

        if (requireComplete && !IsComplete(answers))
        {
            return "Beantwoord alle 25 vragen om de Quick-Scan af te ronden.";
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

    public static DiscScores? Score(IReadOnlyDictionary<int, int> answers)
    {
        int? ScoreCategory(string category)
        {
            var items = Questions.Where(q => q.Category == category).ToList();
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

        var dominant = ScoreCategory(Dominant);
        var invloed = ScoreCategory(Invloed);
        var stabiel = ScoreCategory(Stabiel);
        var nauwkeurig = ScoreCategory(Nauwkeurig);
        if (dominant is null && invloed is null && stabiel is null && nauwkeurig is null)
        {
            return null;
        }

        return new DiscScores(dominant, invloed, stabiel, nauwkeurig);
    }

    public static IReadOnlyList<string> DeriveMatchTags(DiscScores? scores)
    {
        var tags = new List<string>();
        if (scores is null)
        {
            return tags;
        }

        foreach (var category in CategoryCodes)
        {
            if (scores.TryGet(category) is >= 70)
            {
                tags.Add(category);
            }
        }

        return tags;
    }

    public static string SerializeTags(IEnumerable<string> tags)
        => LikertAnswerJson.SerializeTags(tags);

    public static IReadOnlyList<string> ParseTagsJson(string? json)
        => LikertAnswerJson.ParseTags(json);

    public static DiscScores? CompletedScoresOrNull(
        string? status,
        int? dominant,
        int? invloed,
        int? stabiel,
        int? nauwkeurig)
    {
        if (!CandidateCompetencyStatuses.IsCompleted(status)
            || dominant is null
            || invloed is null
            || stabiel is null
            || nauwkeurig is null)
        {
            return null;
        }

        return new DiscScores(dominant, invloed, stabiel, nauwkeurig);
    }

    public static string EverydayLabel(string category) => category switch
    {
        Dominant => "het voortouw nemen",
        Invloed => "mensen meenemen",
        Stabiel => "rust en ritme",
        Nauwkeurig => "nauwkeurig werken",
        _ => "werkstijl"
    };
}

/// <summary>Workplace DISC-style scores as 0–100 percentages (null = not enough answers).</summary>
public sealed record DiscScores(
    int? Dominant,
    int? Invloed,
    int? Stabiel,
    int? Nauwkeurig)
{
    public bool IsComplete =>
        Dominant is not null
        && Invloed is not null
        && Stabiel is not null
        && Nauwkeurig is not null;

    public int Get(string category) => TryGet(category) ?? 0;

    public int? TryGet(string category) => category switch
    {
        DiscTestCatalog.Dominant => Dominant,
        DiscTestCatalog.Invloed => Invloed,
        DiscTestCatalog.Stabiel => Stabiel,
        DiscTestCatalog.Nauwkeurig => Nauwkeurig,
        _ => null
    };
}
