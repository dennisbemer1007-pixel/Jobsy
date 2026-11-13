using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Links job competency slots to likert answers (items chosen by meaning, not whole category).</summary>
internal static class PupilG78CompetenceFromAnswers
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private static readonly PupilQuestionBank Bank = new();

    public static (string? Reflection, int? EvidenceItemId) PupilReflectionForEvidence(
        string slotKey,
        string jobKey,
        IReadOnlyList<int> evidenceItemIds,
        string? answersJson,
        PupilProgress? progress = null)
    {
        var contrastV2 = PupilG78ContrastAnswerPhrases.IsContrastSession(progress);
        var byId = ParseAnswers(answersJson, contrastV2);
        if (byId.Count == 0 || evidenceItemIds.Count == 0)
        {
            return ("dit kwam in je antwoorden niet naar voren.", null);
        }

        var scored = new List<(int Id, int Adjusted)>();
        var answered = new List<(int Id, int Adjusted)>();
        foreach (var id in evidenceItemIds)
        {
            if (!byId.TryGetValue(id, out var raw))
            {
                continue;
            }

            var adjusted = AdjustedScore(id, raw, contrastV2);
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
            var clause = ClauseForItem(pick.Id, byId, contrastV2);
            return clause is null
                ? (null, pick.Id)
                : ($"je gaf aan dat {clause}", pick.Id);
        }

        if (answered.Count == 0)
        {
            return (contrastV2 ? "dit kwam minder naar voren in je antwoorden." : "dit kwam in je antwoorden niet naar voren.", null);
        }

        if (contrastV2)
        {
            return ("dit kwam minder naar voren in je antwoorden.", null);
        }

        if (answered.All(x => x.Adjusted <= 2))
        {
            return ("hierover zaten je antwoorden lager.", null);
        }

        if (answered.All(x => x.Adjusted == 3) || answered.Average(x => x.Adjusted) is >= 2.6 and <= 3.4)
        {
            return ("hierover zaten je antwoorden in het midden.", null);
        }

        return ("hierover zaten je antwoorden in het midden.", null);
    }

    private static string? ClauseForItem(int itemId, IReadOnlyDictionary<int, int> answers, bool contrastV2)
    {
        if (contrastV2)
        {
            var bank = new PupilQuestionBankG78V2();
            var q = bank.Questions.FirstOrDefault(x => x.Id == itemId);
            if (q is null || !answers.TryGetValue(itemId, out var raw))
            {
                return null;
            }

            return PupilG78ContrastAnswerPhrases.TryGetSubordinate(q, raw, out var sub) ? sub : null;
        }

        if (itemId is >= 9001 and <= 9015)
        {
            return PupilG78CompetenceAnswerParaphrases.TryGet(itemId, out var c) ? c : null;
        }

        return PupilG78HighAnswerParaphrases.TryGetSubordinateClause(itemId, out var subordinate) ? subordinate : null;
    }

    private static int RotateIndex(string key, int count)
        => count <= 1 ? 0 : Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(key)) % count;

    private static int AdjustedScore(int itemId, int raw, bool contrastV2)
    {
        if (contrastV2)
        {
            var q = new PupilQuestionBankG78V2().Questions.FirstOrDefault(x => x.Id == itemId);
            return q is null ? raw : PupilG78ContrastAnswerPhrases.ContrastEffective(q, raw);
        }

        var item = Bank.AllItems.FirstOrDefault(i => i.NumericId == itemId);
        if (item is null)
        {
            return raw;
        }

        return item.Reverse ? LikertAnswerJson.LikertMax + LikertAnswerJson.LikertMin - raw : raw;
    }

    private static Dictionary<int, int> ParseAnswers(string? json, bool contrastV2)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var byString = JsonSerializer.Deserialize<Dictionary<string, int>>(json, JsonOpts) ?? [];
            var map = new Dictionary<int, int>();
            var bankItems = contrastV2
                ? new PupilQuestionBankG78V2().AllItems
                : Bank.AllItems;
            foreach (var item in bankItems)
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
