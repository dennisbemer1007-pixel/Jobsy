using System.Text.Json;

namespace Jobsy.Core.Rules;

/// <summary>Shared Likert 1–5 JSON (question id → score) for both assessment engines.</summary>
public static class LikertAnswerJson
{
    public const int LikertMin = 1;
    public const int LikertMax = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool IsValidAnswer(int value) => value is >= LikertMin and <= LikertMax;

    public static Dictionary<int, int> Parse(string? json, int maxQuestionId)
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
                if (!int.TryParse(prop.Name, out var id) || id < 1 || id > maxQuestionId)
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

    public static string Serialize(IReadOnlyDictionary<int, int> answers, int maxQuestionId)
    {
        var ordered = answers
            .Where(kv => kv.Key is >= 1 && kv.Key <= maxQuestionId && IsValidAnswer(kv.Value))
            .OrderBy(kv => kv.Key)
            .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
        return JsonSerializer.Serialize(ordered, JsonOptions);
    }

    public static string SerializeTags(IEnumerable<string> tags)
        => JsonSerializer.Serialize(
            tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            JsonOptions);

    public static IReadOnlyList<string> ParseTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static int ToPercent(IEnumerable<int> scoredLikert)
    {
        var list = scoredLikert.ToList();
        if (list.Count == 0)
        {
            return 0;
        }

        var average = (decimal)list.Sum() / list.Count;
        var percent = (int)Math.Round(
            (average - LikertMin) / (LikertMax - LikertMin) * 100m,
            MidpointRounding.AwayFromZero);
        return Math.Clamp(percent, 0, 100);
    }
}
