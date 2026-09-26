namespace Jobsy.Core.Rules;

/// <summary>
/// Free values Quick-Scan: 25 Likert items mapped to Schwartz workplace drivers
/// (Self-Direction/Stimulation, Benevolence, Achievement, Security/Tradition, Universalism).
/// </summary>
public static class SchwartzValuesCatalog
{
    public const int QuestionCount = 25;
    public const int LikertMin = LikertAnswerJson.LikertMin;
    public const int LikertMax = LikertAnswerJson.LikertMax;
    public const int CategoryQuestionCount = 5;

    public const string Autonomy = "Autonomy";
    public const string Connection = "Connection";
    public const string Achievement = "Achievement";
    public const string Stability = "Stability";
    public const string Impact = "Impact";

    public static readonly string[] CategoryCodes =
    [
        Autonomy,
        Connection,
        Achievement,
        Stability,
        Impact
    ];

    public static readonly IReadOnlyList<CompetencyQuestion> Questions =
    [
        new(1, Autonomy, Reverse: false, "ValuesScan.Q01"),
        new(2, Autonomy, Reverse: false, "ValuesScan.Q02"),
        new(3, Autonomy, Reverse: true, "ValuesScan.Q03"),
        new(4, Autonomy, Reverse: false, "ValuesScan.Q04"),
        new(5, Autonomy, Reverse: true, "ValuesScan.Q05"),
        new(6, Connection, Reverse: false, "ValuesScan.Q06"),
        new(7, Connection, Reverse: false, "ValuesScan.Q07"),
        new(8, Connection, Reverse: true, "ValuesScan.Q08"),
        new(9, Connection, Reverse: false, "ValuesScan.Q09"),
        new(10, Connection, Reverse: true, "ValuesScan.Q10"),
        new(11, Achievement, Reverse: false, "ValuesScan.Q11"),
        new(12, Achievement, Reverse: false, "ValuesScan.Q12"),
        new(13, Achievement, Reverse: true, "ValuesScan.Q13"),
        new(14, Achievement, Reverse: false, "ValuesScan.Q14"),
        new(15, Achievement, Reverse: true, "ValuesScan.Q15"),
        new(16, Stability, Reverse: false, "ValuesScan.Q16"),
        new(17, Stability, Reverse: false, "ValuesScan.Q17"),
        new(18, Stability, Reverse: true, "ValuesScan.Q18"),
        new(19, Stability, Reverse: false, "ValuesScan.Q19"),
        new(20, Stability, Reverse: true, "ValuesScan.Q20"),
        new(21, Impact, Reverse: false, "ValuesScan.Q21"),
        new(22, Impact, Reverse: false, "ValuesScan.Q22"),
        new(23, Impact, Reverse: true, "ValuesScan.Q23"),
        new(24, Impact, Reverse: false, "ValuesScan.Q24"),
        new(25, Impact, Reverse: true, "ValuesScan.Q25")
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
            return "Beantwoord alle 25 stellingen om de waardenscan af te ronden.";
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
    {
        int? Pct(string code)
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

            return scored.Count == CategoryQuestionCount ? LikertAnswerJson.ToPercent(scored) : null;
        }

        var autonomy = Pct(Autonomy);
        var connection = Pct(Connection);
        var achievement = Pct(Achievement);
        var stability = Pct(Stability);
        var impact = Pct(Impact);

        if (autonomy is null && connection is null && achievement is null && stability is null && impact is null)
        {
            return null;
        }

        return new SchwartzValuesScores(autonomy, connection, achievement, stability, impact);
    }

    public static IReadOnlyList<string> DeriveMatchTags(SchwartzValuesScores scores)
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

        Add(Autonomy, scores.Autonomy, "waarden-eigen-regie", "waarden-kaders");
        Add(Connection, scores.Connection, "waarden-verbinding", "waarden-zelfstandig");
        Add(Achievement, scores.Achievement, "waarden-prestatie", "waarden-balans");
        Add(Stability, scores.Stability, "waarden-zekerheid", "waarden-verandering");
        Add(Impact, scores.Impact, "waarden-impact", "waarden-praktisch");
        return tags;
    }

    public static string SerializeTags(IEnumerable<string> tags)
        => LikertAnswerJson.SerializeTags(tags);

    public static IReadOnlyList<string> ParseTags(string? json)
        => LikertAnswerJson.ParseTags(json);

    public static IReadOnlyList<string> ParseTagsJson(string? json)
        => LikertAnswerJson.ParseTags(json);

    public static IReadOnlyList<CompetencyQuestion> QuestionsFor(string category)
        => Questions.Where(q => string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Shared dimension label (title case). Legacy lowercase phrases map via <see cref="DimensionLabels.MapStored"/>.</summary>
    public static string EverydayLabel(string code)
        => code switch
        {
            Autonomy or Connection or Achievement or Stability or Impact => DimensionLabels.For(code),
            _ => "wat jij belangrijk vindt op werk"
        };
}

/// <summary>0–100 scores per Schwartz workplace value driver.</summary>
public sealed record SchwartzValuesScores(
    int? Autonomy = null,
    int? Connection = null,
    int? Achievement = null,
    int? Stability = null,
    int? Impact = null)
{
    public bool IsComplete =>
        Autonomy is not null
        && Connection is not null
        && Achievement is not null
        && Stability is not null
        && Impact is not null;

    public int Get(string code) => code switch
    {
        SchwartzValuesCatalog.Autonomy => Autonomy ?? 55,
        SchwartzValuesCatalog.Connection => Connection ?? 55,
        SchwartzValuesCatalog.Achievement => Achievement ?? 55,
        SchwartzValuesCatalog.Stability => Stability ?? 55,
        SchwartzValuesCatalog.Impact => Impact ?? 55,
        _ => 55
    };
}
