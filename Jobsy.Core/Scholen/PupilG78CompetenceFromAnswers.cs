using System.Text.Json;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Competence bands from G78 likert answers 9001–9015 only (no invented scores).</summary>
internal static class PupilG78CompetenceFromAnswers
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static readonly PupilQuestionBank Bank = new();

    internal enum Band
    {
        Unknown,
        Strong,
        Weak,
        Mixed
    }

    internal static Band CategoryBand(string category, string? answersJson)
    {
        var values = AnswersForCategory(category, answersJson);
        if (values.Count == 0)
        {
            return Band.Unknown;
        }

        if (values.All(v => v >= 4))
        {
            return Band.Strong;
        }

        if (values.All(v => v <= 2))
        {
            return Band.Weak;
        }

        if (values.Any(v => v >= 4) && values.Any(v => v <= 2))
        {
            return Band.Mixed;
        }

        if (values.Average() >= 3.5)
        {
            return Band.Strong;
        }

        if (values.Average() <= 2.5)
        {
            return Band.Weak;
        }

        return Band.Mixed;
    }

    public static string NeutralReflection(string jobAsksLabel, string category, string? answersJson)
    {
        return CategoryBand(category, answersJson) switch
        {
            Band.Strong =>
                $"Je antwoorden laten zien dat je hier sterk in bent.",
            Band.Weak =>
                $"Dit werk vraagt ook {jobAsksLabel}; daar scoorde je lager. Dat kun je verder ontdekken.",
            Band.Mixed =>
                $"Je antwoorden over {CategoryTopic(category)} wisselen; sommige punten passen hier, andere kun je nog verkennen.",
            _ => ""
        };
    }

    private static string CategoryTopic(string category) => category switch
    {
        CompetencyTestCatalog.Samenwerken => "samenwerken",
        CompetencyTestCatalog.Resultaatgerichtheid => "netjes en precies werken",
        CompetencyTestCatalog.Stressbestendigheid => "kalm blijven",
        CompetencyTestCatalog.Innovatie => "nieuwe ideeën",
        CompetencyTestCatalog.Extraversie => "met mensen praten",
        _ => category.ToLowerInvariant()
    };

    private static List<int> AnswersForCategory(string category, string? answersJson)
    {
        var byId = ParseAnswers(answersJson);
        if (byId.Count == 0)
        {
            return [];
        }

        var values = new List<int>();
        foreach (var q in Bank.AsCompetencyItems())
        {
            if (!string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (byId.TryGetValue(q.Id, out var raw))
            {
                values.Add(q.Reverse
                    ? LikertAnswerJson.LikertMax + LikertAnswerJson.LikertMin - raw
                    : raw);
            }
        }

        return values;
    }

    private static Dictionary<int, int> ParseAnswers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var byString = JsonSerializer.Deserialize<Dictionary<string, int>>(json, JsonOpts) ?? [];
            var map = new Dictionary<int, int>();
            foreach (var item in Bank.AllItems)
            {
                if (byString.TryGetValue(item.Id, out var value) && LikertAnswerJson.IsValidAnswer(value))
                {
                    map[item.NumericId] = value;
                }
            }

            return map;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
