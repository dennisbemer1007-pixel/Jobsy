using System.Text.Json;

namespace Jobsy.Core.Careers;

public static class OccupationDayInLifeJson
{
    public static bool TryParse(string? json, OccupationDayFacts facts, out OccupationDayDraft draft, out string? error)
    {
        draft = new OccupationDayDraft(facts.TitleNl, "", "", "", "", [], "");
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
            var blocks = ReadBlocks(root);
            draft = OccupationDayBlocks.WithDerived(new OccupationDayDraft(
                facts.TitleNl,
                ReadString(root, "morning"),
                ReadString(root, "midday"),
                ReadString(root, "afternoon"),
                ReadString(root, "closing"),
                highlights,
                ReadString(root, "varies"),
                blocks,
                ReadLines(root, "tasks"),
                ReadLines(root, "skills")));
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
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return "";
        }

        foreach (var property in root.EnumerateObject())
        {
            if (!property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                || property.Value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            return (property.Value.GetString() ?? "").Trim();
        }

        return "";
    }

    private static string ReadAlias(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            var value = ReadString(item, name);
            if (value.Length > 0)
            {
                return value;
            }
        }

        return "";
    }

    private static IReadOnlyList<OccupationDayBlock> ReadBlocks(JsonElement root)
    {
        if (!root.TryGetProperty("blocks", out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var rows = new List<OccupationDayBlock>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var clean = true;
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                clean = false;
                continue;
            }

            var keyRaw = ReadAlias(item, "key", "sleutel", "moment");
            var label = ReadAlias(item, "label", "titel", "title", "naam", "name");
            var text = ReadAlias(item, "text", "tekst", "omschrijving", "description", "body", "inhoud");
            var canonical = OccupationDayBlocks.CanonicalKey(keyRaw, label);
            if (canonical is null)
            {
                clean = false;
            }
            else if (label.Length > 32 || text.Length == 0 || !seen.Add(canonical))
            {
                clean = false;
            }

            var key = canonical ?? (keyRaw.Length == 0 ? "?" : keyRaw.Trim());
            rows.Add(new OccupationDayBlock(key, label, text));
        }

        return clean ? OccupationDayBlocks.Normalize(rows) : rows;
    }

    private static IReadOnlyList<string> ReadLines(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
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
            if (lines.Count == 8)
            {
                break;
            }
        }

        return lines;
    }

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
