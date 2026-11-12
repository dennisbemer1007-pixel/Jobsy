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

    /// <summary>Concrete pupil clause for competency bullet (no score language).</summary>
    public static string? PupilReflectionClause(
        string category,
        string? answersJson,
        ref bool explorePhraseUsed)
    {
        var band = CategoryBand(category, answersJson);
        var best = BestSupportingClause(category, answersJson);
        if (best is not null)
        {
            return $"je gaf aan dat {best}";
        }

        if (band is Band.Weak or Band.Mixed)
        {
            var text = "hierover zaten je antwoorden lager.";
            if (!explorePhraseUsed)
            {
                text += " Dat kun je verder ontdekken.";
                explorePhraseUsed = true;
            }

            return text;
        }

        return null;
    }

    private static string? BestSupportingClause(string category, string? answersJson)
    {
        var byId = ParseAnswers(answersJson);
        if (byId.Count == 0)
        {
            return null;
        }

        int? bestId = null;
        var bestScore = 0;
        foreach (var q in Bank.AsCompetencyItems())
        {
            if (!string.Equals(q.Category, category, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!byId.TryGetValue(q.Id, out var raw))
            {
                continue;
            }

            var adjusted = q.Reverse
                ? LikertAnswerJson.LikertMax + LikertAnswerJson.LikertMin - raw
                : raw;
            if (adjusted < 4 || adjusted <= bestScore)
            {
                continue;
            }

            bestScore = adjusted;
            bestId = q.Id;
        }

        if (bestId is null)
        {
            return null;
        }

        return PupilG78CompetenceAnswerParaphrases.TryGet(bestId.Value, out var clause) ? clause : null;
    }

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
