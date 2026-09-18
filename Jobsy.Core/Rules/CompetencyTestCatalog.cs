using System.Text.Json;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

/// <summary>
/// Workplace Big Five / OCEAN short form (20 Likert items), mapped to four Lobsy competencies.
/// Inspired by Mini-IPIP (Donnellan et al., 2006) and IPIP-NEO work-style wording:
/// Agreeableness (+ social Extraversion) → Samenwerken,
/// Conscientiousness → Resultaatgerichtheid,
/// Emotional Stability (reverse Neuroticism) → Stressbestendigheid,
/// Openness / Intellect → Innovatie / Probleemoplossend.
/// </summary>
public static class CompetencyTestCatalog
{
    public const int QuestionCount = 20;
    public const int LikertMin = 1;
    public const int LikertMax = 5;
    public const int CategoryQuestionCount = 5;

    public const string Samenwerken = "Samenwerken";
    public const string Resultaatgerichtheid = "Resultaatgerichtheid";
    public const string Stressbestendigheid = "Stressbestendigheid";
    public const string Innovatie = "Innovatie";

    public static readonly string[] CategoryCodes =
    [
        Samenwerken,
        Resultaatgerichtheid,
        Stressbestendigheid,
        Innovatie
    ];

    public static readonly IReadOnlyList<CompetencyQuestion> Questions =
    [
        new(1, Samenwerken, Reverse: false, "Competency.Q01"),
        new(2, Samenwerken, Reverse: false, "Competency.Q02"),
        new(3, Samenwerken, Reverse: false, "Competency.Q03"),
        new(4, Samenwerken, Reverse: true, "Competency.Q04"),
        new(5, Samenwerken, Reverse: true, "Competency.Q05"),
        new(6, Resultaatgerichtheid, Reverse: false, "Competency.Q06"),
        new(7, Resultaatgerichtheid, Reverse: false, "Competency.Q07"),
        new(8, Resultaatgerichtheid, Reverse: false, "Competency.Q08"),
        new(9, Resultaatgerichtheid, Reverse: true, "Competency.Q09"),
        new(10, Resultaatgerichtheid, Reverse: true, "Competency.Q10"),
        new(11, Stressbestendigheid, Reverse: false, "Competency.Q11"),
        new(12, Stressbestendigheid, Reverse: false, "Competency.Q12"),
        new(13, Stressbestendigheid, Reverse: false, "Competency.Q13"),
        new(14, Stressbestendigheid, Reverse: true, "Competency.Q14"),
        new(15, Stressbestendigheid, Reverse: true, "Competency.Q15"),
        new(16, Innovatie, Reverse: false, "Competency.Q16"),
        new(17, Innovatie, Reverse: false, "Competency.Q17"),
        new(18, Innovatie, Reverse: false, "Competency.Q18"),
        new(19, Innovatie, Reverse: true, "Competency.Q19"),
        new(20, Innovatie, Reverse: true, "Competency.Q20")
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool IsValidAnswer(int value) => value is >= LikertMin and <= LikertMax;

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
            return "Beantwoord alle 20 vragen om de test af te ronden.";
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
    {
        var result = new Dictionary<int, int>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!int.TryParse(prop.Name, out var id) || id is < 1 or > QuestionCount)
                {
                    continue;
                }

                var value = prop.Value.ValueKind switch
                {
                    JsonValueKind.Number when prop.Value.TryGetInt32(out var n) => n,
                    JsonValueKind.String when int.TryParse(prop.Value.GetString(), out var parsed) => parsed,
                    _ => 0
                };
                if (IsValidAnswer(value))
                {
                    result[id] = value;
                }
            }
        }
        catch (JsonException)
        {
            return result;
        }

        return result;
    }

    public static string SerializeAnswers(IReadOnlyDictionary<int, int> answers)
    {
        var ordered = answers
            .Where(kv => kv.Key is >= 1 and <= QuestionCount && IsValidAnswer(kv.Value))
            .OrderBy(kv => kv.Key)
            .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
        return JsonSerializer.Serialize(ordered, JsonOptions);
    }

    /// <summary>
    /// Converts Likert answers to 0–100% per category. Missing items are skipped;
    /// a category without answers is omitted (null).
    /// </summary>
    public static CompetencyScores? Score(IReadOnlyDictionary<int, int> answers)
    {
        int? ScoreCategory(string category)
        {
            var items = Questions.Where(q => q.Category == category).ToList();
            var scored = 0;
            var sum = 0;
            foreach (var question in items)
            {
                if (!answers.TryGetValue(question.Id, out var raw) || !IsValidAnswer(raw))
                {
                    continue;
                }

                var value = question.Reverse ? LikertMax + LikertMin - raw : raw;
                sum += value;
                scored++;
            }

            if (scored == 0)
            {
                return null;
            }

            var average = (decimal)sum / scored;
            var percent = (int)Math.Round((average - LikertMin) / (LikertMax - LikertMin) * 100m, MidpointRounding.AwayFromZero);
            return Math.Clamp(percent, 0, 100);
        }

        var samenwerken = ScoreCategory(Samenwerken);
        var resultaat = ScoreCategory(Resultaatgerichtheid);
        var stress = ScoreCategory(Stressbestendigheid);
        var innovatie = ScoreCategory(Innovatie);
        if (samenwerken is null && resultaat is null && stress is null && innovatie is null)
        {
            return null;
        }

        return new CompetencyScores(samenwerken, resultaat, stress, innovatie);
    }

    public static CompetencyScores? CompletedScoresOrNull(
        string? status,
        int? samenwerken,
        int? resultaat,
        int? stress,
        int? innovatie)
    {
        if (!CandidateCompetencyStatuses.IsCompleted(status)
            || samenwerken is null
            || resultaat is null
            || stress is null
            || innovatie is null)
        {
            return null;
        }

        return new CompetencyScores(samenwerken, resultaat, stress, innovatie);
    }
}

public sealed record CompetencyQuestion(int Id, string Category, bool Reverse, string TextKey);

/// <summary>Four workplace competencies as 0–100 percentages (null = not enough answers).</summary>
public sealed record CompetencyScores(
    int? Samenwerken,
    int? Resultaatgerichtheid,
    int? Stressbestendigheid,
    int? Innovatie)
{
    public bool IsComplete =>
        Samenwerken is not null
        && Resultaatgerichtheid is not null
        && Stressbestendigheid is not null
        && Innovatie is not null;

    public int Get(string category) => category switch
    {
        CompetencyTestCatalog.Samenwerken => Samenwerken ?? 0,
        CompetencyTestCatalog.Resultaatgerichtheid => Resultaatgerichtheid ?? 0,
        CompetencyTestCatalog.Stressbestendigheid => Stressbestendigheid ?? 0,
        CompetencyTestCatalog.Innovatie => Innovatie ?? 0,
        _ => 0
    };

    public int? TryGet(string category) => category switch
    {
        CompetencyTestCatalog.Samenwerken => Samenwerken,
        CompetencyTestCatalog.Resultaatgerichtheid => Resultaatgerichtheid,
        CompetencyTestCatalog.Stressbestendigheid => Stressbestendigheid,
        CompetencyTestCatalog.Innovatie => Innovatie,
        _ => null
    };
}
