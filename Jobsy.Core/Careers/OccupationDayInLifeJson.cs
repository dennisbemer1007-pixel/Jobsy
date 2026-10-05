using System.Text.Json;

namespace Jobsy.Core.Careers;

public static class OccupationDayInLifeJson
{
    public static bool TryParse(string? json, OccupationDayFacts facts, out OccupationDayDraft draft, out string? error)
    {
        draft = new OccupationDayDraft(facts.TitleNl, "", "", "", [], "");
        error = null;
        var payload = Unwrap(json);
        if (payload.Length == 0)
        {
            error = "onleesbaar";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "onleesbaar";
                return false;
            }

            var root = doc.RootElement;
            var highlights = ReadHighlights(root);
            draft = new OccupationDayDraft(
                facts.TitleNl,
                ReadString(root, "morning"),
                ReadString(root, "midday"),
                ReadString(root, "afternoon"),
                highlights,
                ReadString(root, "varies"));
            return true;
        }
        catch (JsonException)
        {
            error = "onleesbaar";
            return false;
        }
    }

    private static string Unwrap(string? json)
    {
        var text = (json ?? "").Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var start = text.IndexOf('\n');
            var end = text.LastIndexOf("```", StringComparison.Ordinal);
            if (start >= 0 && end > start)
            {
                text = text[(start + 1)..end].Trim();
            }
        }

        var open = text.IndexOf('{');
        var close = text.LastIndexOf('}');
        if (open >= 0 && close > open)
        {
            text = text[open..(close + 1)];
        }

        return text.Trim();
    }

    private static string ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? (value.GetString() ?? "").Trim()
            : "";

    private static IReadOnlyList<string> ReadHighlights(JsonElement root)
    {
        if (!root.TryGetProperty("highlights", out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var lines = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var line = (item.GetString() ?? "").Trim();
            if (line.Length == 0 || lines.Contains(line, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            lines.Add(line);
            if (lines.Count == 4)
            {
                break;
            }
        }

        return lines;
    }
}
