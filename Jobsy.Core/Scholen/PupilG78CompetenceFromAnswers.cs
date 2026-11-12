using System.Text.Json;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Links job competency slots to likert answers (items chosen by meaning, not whole category).</summary>
internal static class PupilG78CompetenceFromAnswers
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static readonly PupilQuestionBank Bank = new();

    public static string? PupilReflectionForEvidence(
        string slotKey,
        string jobKey,
        IReadOnlyList<int> evidenceItemIds,
        string? answersJson)
    {
        var byId = ParseAnswers(answersJson);
        if (byId.Count == 0 || evidenceItemIds.Count == 0)
        {
            return "dit kwam in je antwoorden niet naar voren.";
        }

        var scored = new List<(int Id, int Adjusted)>();
        var answered = new List<(int Id, int Adjusted)>();
        foreach (var id in evidenceItemIds)
        {
            if (!byId.TryGetValue(id, out var raw))
            {
                continue;
            }

            var adjusted = AdjustedScore(id, raw);
            answered.Add((id, adjusted));
            if (adjusted >= 4)
            {
                scored.Add((id, adjusted));
            }
        }

        if (scored.Count > 0)
        {
            var ordered = scored
                .OrderByDescending(x => x.Adjusted)
                .ThenBy(x => x.Id)
                .ToList();
            var pick = ordered[RotateIndex($"{slotKey}:{jobKey}", ordered.Count)];
            var clause = ClauseForItem(pick.Id);
            return clause is null ? null : $"je gaf aan dat {clause}";
        }

        if (answered.Count == 0)
        {
            return "dit kwam in je antwoorden niet naar voren.";
        }

        if (answered.All(x => x.Adjusted <= 2))
        {
            return "hierover zaten je antwoorden lager.";
        }

        if (answered.All(x => x.Adjusted == 3) || answered.Average(x => x.Adjusted) is >= 2.6 and <= 3.4)
        {
            return "hierover zaten je antwoorden in het midden.";
        }

        return "hierover zaten je antwoorden in het midden.";
    }

    private static string? ClauseForItem(int itemId)
    {
        if (itemId is >= 9001 and <= 9015)
        {
            return PupilG78CompetenceAnswerParaphrases.TryGet(itemId, out var c) ? c : null;
        }

        return PupilG78HighAnswerParaphrases.TryGetSubordinateClause(itemId, out var sub) ? sub : null;
    }

    private static int RotateIndex(string key, int count)
        => count <= 1 ? 0 : Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(key)) % count;

    private static int AdjustedScore(int itemId, int raw)
    {
        var q = Bank.AllItems.FirstOrDefault(i => i.NumericId == itemId);
        if (q is null)
        {
            return raw;
        }

        return q.Reverse ? LikertAnswerJson.LikertMax + LikertAnswerJson.LikertMin - raw : raw;
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
