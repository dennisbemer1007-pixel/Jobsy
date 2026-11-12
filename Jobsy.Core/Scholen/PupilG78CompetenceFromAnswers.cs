using System.Text.Json;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Competence bands from G78 likert answers 9001–9015 only (no invented scores).</summary>
internal static class PupilG78CompetenceFromAnswers
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static readonly PupilQuestionBank Bank = new();

    /// <summary>Item ids preferred per route need key (dimension must still match).</summary>
    private static readonly IReadOnlyDictionary<string, int[]> PreferredItemsByNeedKey =
        new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Precies"] = [9007, 9012],
            ["Nauwkeurig"] = [9007],
            ["Netjes"] = [9007],
            ["Maken"] = [9002],
            ["Verantwoordelijk"] = [9002],
            ["Concentratie"] = [9002],
            ["Geduld"] = [9003, 9008],
            ["Kalm"] = [9003, 9008],
            ["RustigBlijven"] = [9003],
            ["Stress"] = [9003, 9008],
            ["Nieuwsgierig"] = [9004, 9009],
            ["Ideeën"] = [9004, 9009],
            ["Nieuw"] = [9004],
            ["Probleem"] = [9004],
            ["Mensen"] = [9006, 9010],
            ["Overtuigen"] = [9010],
        };

    internal enum Band
    {
        Unknown,
        Strong,
        Weak,
        Mixed,
        Middle
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

        var avg = values.Average();
        if (avg is >= 2.6 and <= 3.4)
        {
            return Band.Middle;
        }

        if (values.Any(v => v >= 4) && values.Any(v => v <= 2))
        {
            return Band.Mixed;
        }

        if (avg >= 3.5)
        {
            return Band.Strong;
        }

        if (avg <= 2.5)
        {
            return Band.Weak;
        }

        return Band.Middle;
    }

  public static string? PupilReflectionClause(
        string category,
        string needKey,
        string jobKey,
        string? answersJson,
        ref bool explorePhraseUsed)
    {
        var clause = BestSupportingClause(category, needKey, jobKey, answersJson);
        if (clause is not null)
        {
            return $"je gaf aan dat {clause}";
        }

        var band = CategoryBand(category, answersJson);
        return band switch
        {
            Band.Weak or Band.Mixed => WeakReflection(ref explorePhraseUsed),
            Band.Middle or Band.Unknown => "hierover zaten je antwoorden in het midden.",
            _ => null
        };
    }

    private static string WeakReflection(ref bool explorePhraseUsed)
    {
        if (!explorePhraseUsed)
        {
            explorePhraseUsed = true;
            return "hierover zaten je antwoorden lager. Dat kun je verder ontdekken.";
        }

        return "hierover zaten je antwoorden lager.";
    }

    private static string? BestSupportingClause(
        string category,
        string needKey,
        string jobKey,
        string? answersJson)
    {
        var byId = ParseAnswers(answersJson);
        if (byId.Count == 0)
        {
            return null;
        }

        var preferred = PreferredItemsByNeedKey.TryGetValue(needKey, out var ids) ? ids : [];
        var candidates = new List<(int Id, int Score)>();

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

            var adjusted = Adjust(q.Reverse, raw);
            if (adjusted < 4)
            {
                continue;
            }

            candidates.Add((q.Id, adjusted));
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        IEnumerable<(int Id, int Score)> ordered = candidates.OrderByDescending(c => c.Score);
        if (preferred.Length > 0)
        {
            ordered = candidates
                .OrderByDescending(c => preferred.Contains(c.Id) ? 1 : 0)
                .ThenByDescending(c => c.Score);
        }

        var list = ordered.ToList();
        var pick = list[RotateIndex(jobKey, list.Count)];
        return PupilG78CompetenceAnswerParaphrases.TryGet(pick.Id, out var phrase) ? phrase : null;
    }

    private static int RotateIndex(string jobKey, int count)
        => count <= 1 ? 0 : Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(jobKey)) % count;

    private static int Adjust(bool reverse, int raw)
        => reverse ? LikertAnswerJson.LikertMax + LikertAnswerJson.LikertMin - raw : raw;

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
                values.Add(Adjust(q.Reverse, raw));
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
