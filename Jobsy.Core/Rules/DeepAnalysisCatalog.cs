using System.Text.Json;

namespace Jobsy.Core.Rules;

/// <summary>
/// Paid deep psychometric analysis: 150 Likert items covering Big Five facets,
/// RIASEC interests, and practical work capacity / skills.
/// </summary>
public static class DeepAnalysisCatalog
{
    public const int QuestionCount = 150;
    public const int LikertMin = 1;
    public const int LikertMax = 5;

    private static readonly Lazy<IReadOnlyList<DeepAnalysisQuestion>> LazyQuestions = new(BuildQuestions);

    public static IReadOnlyList<DeepAnalysisQuestion> Questions => LazyQuestions.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static IReadOnlyList<DeepAnalysisQuestion> BuildQuestions()
    {
        var list = new List<DeepAnalysisQuestion>(QuestionCount);
        var id = 1;

        // Big Five facets: 5 traits × 10 = 50
        string[] bigFive =
        [
            "Openheid", "Consciëntieusheid", "Extraversie", "Inschikkelijkheid", "EmotioneleStabiliteit"
        ];
        string[] bigFivePrompts =
        [
            "Ik zoek graag nieuwe ideeën en werkwijzen.",
            "Ik werk systematisch en houd afspraken bij.",
            "Ik krijg energie van contact met collega's.",
            "Ik houd rekening met de gevoelens van anderen.",
            "Ik blijf rustig onder tijdsdruk."
        ];
        for (var t = 0; t < bigFive.Length; t++)
        {
            for (var i = 0; i < 10; i++)
            {
                var reverse = i % 5 == 4;
                list.Add(new DeepAnalysisQuestion(
                    id++,
                    "BigFive",
                    bigFive[t],
                    reverse,
                    $"{bigFivePrompts[t]} (variant {i + 1})"));
            }
        }

        // RIASEC: 6 × 10 = 60
        string[] riasec = CompetencyTestCatalog.RiasecCodes.ToArray();
        string[] riasecPrompts =
        [
            "Ik werk graag met machines, gereedschap of buiten.",
            "Ik onderzoek graag hoe iets werkt of waarom iets zo is.",
            "Ik bedenk graag creatieve of visuele oplossingen.",
            "Ik help graag mensen of werk in een team.",
            "Ik neem graag initiatief en overtuig anderen.",
            "Ik houd van orde, administratie en vaste procedures."
        ];
        for (var t = 0; t < riasec.Length; t++)
        {
            for (var i = 0; i < 10; i++)
            {
                list.Add(new DeepAnalysisQuestion(
                    id++,
                    "RIASEC",
                    riasec[t],
                    Reverse: false,
                    $"{riasecPrompts[t]} (variant {i + 1})"));
            }
        }

        // Practical capacity / skills: 40
        string[] practicalThemes =
        [
            "FysiekeBelasting", "MentaleBelasting", "Klantcontact", "Zelfstandigheid",
            "Ploegendienst", "Leervermogen", "Nauwkeurigheid", "Tempo"
        ];
        string[] practicalPrompts =
        [
            "Ik kan fysiek zwaar of staand werk lang volhouden.",
            "Ik kan complexe informatie combineren zonder overprikkeld te raken.",
            "Ik voel me op mijn gemak bij klant- of gastcontact.",
            "Ik werk goed zelfstandig zonder voortdurende sturing.",
            "Ik kan wisselende of onregelmatige diensten aan.",
            "Ik leer nieuwe systemen of taken snel.",
            "Ik controleer mijn werk zorgvuldig op fouten.",
            "Ik houd een hoog werktempo vol zonder kwaliteit in te leveren."
        ];
        for (var t = 0; t < practicalThemes.Length; t++)
        {
            for (var i = 0; i < 5; i++)
            {
                list.Add(new DeepAnalysisQuestion(
                    id++,
                    "Practical",
                    practicalThemes[t],
                    Reverse: i == 4,
                    $"{practicalPrompts[t]} (variant {i + 1})"));
            }
        }

        if (list.Count != QuestionCount)
        {
            throw new InvalidOperationException(
                $"DeepAnalysisCatalog must contain {QuestionCount} questions, got {list.Count}.");
        }

        return list;
    }

    public static bool IsValidAnswer(int value) => value is >= LikertMin and <= LikertMax;

    public static string? ValidateAnswers(IReadOnlyDictionary<int, int> answers, bool requireComplete)
    {
        foreach (var (qid, value) in answers)
        {
            if (qid is < 1 or > QuestionCount)
            {
                return "Onbekend vraagnummer in de diepte-analyse.";
            }

            if (!IsValidAnswer(value))
            {
                return "Elk antwoord moet tussen 1 en 5 liggen.";
            }
        }

        if (requireComplete && !IsComplete(answers))
        {
            return "Beantwoord alle 150 vragen om de diepte-analyse af te ronden.";
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
                if (!int.TryParse(prop.Name, out var qid) || qid is < 1 or > QuestionCount)
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
                    result[qid] = value;
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

    public static IReadOnlyList<string> DeriveEnrichedTags(IReadOnlyDictionary<int, int> answers)
    {
        var domainAverages = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var question in Questions)
        {
            if (!answers.TryGetValue(question.Id, out var raw) || !IsValidAnswer(raw))
            {
                continue;
            }

            var value = question.Reverse ? LikertMax + LikertMin - raw : raw;
            if (!domainAverages.TryGetValue(question.Domain, out var bucket))
            {
                bucket = [];
                domainAverages[question.Domain] = bucket;
            }

            bucket.Add(value);
        }

        var tags = new List<string>();
        foreach (var (domain, values) in domainAverages)
        {
            if (values.Count == 0)
            {
                continue;
            }

            var avg = values.Average();
            if (avg >= 4.0)
            {
                tags.Add(domain);
            }
        }

        return tags.OrderBy(t => t, StringComparer.Ordinal).ToList();
    }
}

public sealed record DeepAnalysisQuestion(
    int Id,
    string Family,
    string Domain,
    bool Reverse,
    string PromptNl);
