using System.Text;
using System.Text.Json;

namespace Jobsy.Core.Careers;

/// <summary>One moment in a stored typical day. The label is short B1; the text stays inside the ESCO source.</summary>
public sealed record OccupationDayBlock(string Key, string Label, string Text);

/// <summary>
/// Timeline keys for a fuller day. Labels such as "Ochtendzorg" or "Overdracht" are only used
/// when the source occupation is actually about that work.
/// </summary>
public static class OccupationDayBlocks
{
    public static readonly string[] Keys =
    [
        "start", "morning", "talk", "plan", "pause", "afternoon", "handover", "close"
    ];

    /// <summary>
    /// Five moments always fit a normal source: start, morning, pause, afternoon, close.
    /// Talk, plan and handover are extra, only when the source names that work.
    /// </summary>
    public const int MinNormal = 5;
    public const int MinThin = 4;
    public const int Max = 8;

    public static string DefaultLabel(string? key) => (key ?? "").Trim().ToLowerInvariant() switch
    {
        "start" => "Start",
        "morning" => "Ochtend",
        "talk" => "Gesprek",
        "plan" => "Plannen",
        "pause" => "Pauze",
        "afternoon" => "Middag",
        "handover" => "Overdracht",
        "close" => "Afronden",
        _ => ""
    };

    public static bool IsKnown(string? key)
        => Keys.Contains((key ?? "").Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps a model key onto the stored key. Dutch names and a clock time in the key are accepted.
    /// A sentence in the label is not used as a key.
    /// </summary>
    public static string? CanonicalKey(string? raw, string? label = null)
    {
        var key = (raw ?? "").Trim();
        if (key.Length > 0)
        {
            var mapped = MapLoose(key);
            if (mapped is not null)
            {
                return mapped;
            }

            if (!ContainsClock(key))
            {
                return null;
            }
        }

        return MapLoose(label);
    }

    public static IReadOnlyList<OccupationDayBlock> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var rows = JsonSerializer.Deserialize<List<OccupationDayBlock>>(json, OccupationDayJson.Options) ?? [];
            return Normalize(rows);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string Serialize(IReadOnlyList<OccupationDayBlock>? blocks)
        => JsonSerializer.Serialize(Normalize(blocks ?? []), OccupationDayJson.Options);

    public static IReadOnlyList<OccupationDayBlock> Normalize(IEnumerable<OccupationDayBlock>? blocks)
    {
        var found = new List<OccupationDayBlock>();
        foreach (var block in blocks ?? [])
        {
            if (block is null || !IsKnown(block.Key))
            {
                continue;
            }

            var key = block.Key.Trim().ToLowerInvariant();
            if (found.Any(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var text = (block.Text ?? "").Trim();
            if (text.Length == 0)
            {
                continue;
            }

            var label = (block.Label ?? "").Trim();
            if (label.Length == 0)
            {
                label = DefaultLabel(key);
            }

            if (label.Length > 32)
            {
                label = label[..32].Trim();
            }

            found.Add(new OccupationDayBlock(key, label, text));
        }

        return Keys
            .Select(key => found.FirstOrDefault(item => item.Key == key))
            .Where(item => item is not null)
            .Cast<OccupationDayBlock>()
            .Take(Max)
            .ToList();
    }

    public static string? Text(IReadOnlyList<OccupationDayBlock>? blocks, string key)
        => blocks?.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))?.Text;

    /// <summary>Keeps the four stored prose fields in step with the timeline, for older readers.</summary>
    public static OccupationDayDraft WithDerived(OccupationDayDraft draft)
    {
        var blocks = draft.Blocks;
        if (blocks is not { Count: > 0 })
        {
            return draft;
        }

        var morning = Text(blocks, "morning") ?? Text(blocks, "start") ?? draft.Morning;
        var midday = Text(blocks, "plan") ?? Text(blocks, "talk") ?? Text(blocks, "pause") ?? draft.Midday;
        var afternoon = Text(blocks, "afternoon") ?? draft.Afternoon;
        var closing = Text(blocks, "close") ?? Text(blocks, "handover") ?? draft.Closing;
        return draft with
        {
            Morning = string.IsNullOrWhiteSpace(morning) ? draft.Morning : morning,
            Midday = string.IsNullOrWhiteSpace(midday) ? draft.Midday : midday,
            Afternoon = string.IsNullOrWhiteSpace(afternoon) ? draft.Afternoon : afternoon,
            Closing = string.IsNullOrWhiteSpace(closing) ? draft.Closing : closing
        };
    }

    public static IReadOnlyList<OccupationDayBlock> FromLegacy(string? morning, string? midday, string? afternoon, string? closing)
    {
        var rows = new List<OccupationDayBlock>();
        Add("morning", morning);
        Add("pause", midday);
        Add("afternoon", afternoon);
        Add("close", closing);
        return rows;

        void Add(string key, string? text)
        {
            var line = (text ?? "").Trim();
            if (line.Length == 0)
            {
                return;
            }

            rows.Add(new OccupationDayBlock(key, DefaultLabel(key), line));
        }
    }

    private static string? MapLoose(string? value)
    {
        string? found = null;
        var content = 0;
        foreach (var token in Tokens(value))
        {
            if (IsClock(token))
            {
                continue;
            }

            content++;
            var mapped = MapWord(token);
            if (mapped is null)
            {
                return null;
            }

            found = mapped;
        }

        return content == 0 ? null : found;
    }

    private static string? MapWord(string token) => token switch
    {
        "start" or "begin" or "opening" or "aanvang" => "start",
        "morning" or "ochtend" or "voormiddag" => "morning",
        "talk" or "gesprek" or "overleg" or "contact" => "talk",
        "plan" or "plannen" or "planning" => "plan",
        "pause" or "pauze" or "break" or "rust" => "pause",
        "afternoon" or "middag" or "namiddag" => "afternoon",
        "handover" or "overdracht" or "overdragen" or "overgave" => "handover",
        "close" or "closing" or "afronden" or "afsluiten" or "einde" or "eind" or "slot" => "close",
        _ => null
    };

    private static bool ContainsClock(string text)
    {
        foreach (var token in Tokens(text))
        {
            if (IsClock(token))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsClock(string token)
    {
        var split = token.IndexOf(':');
        if (split <= 0)
        {
            split = token.IndexOf('.');
        }

        if (split <= 0 || split >= token.Length - 1)
        {
            return false;
        }

        for (var i = 0; i < token.Length; i++)
        {
            if (i == split)
            {
                continue;
            }

            if (!char.IsDigit(token[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static List<string> Tokens(string? value)
    {
        var list = new List<string>();
        var current = new StringBuilder();
        foreach (var ch in (value ?? "").Trim().ToLowerInvariant())
        {
            if (ch is ' ' or '\t' or '-' or '–' or '—' or '|' or '/' or ',')
            {
                Flush();
                continue;
            }

            current.Append(ch);
        }

        Flush();
        return list;

        void Flush()
        {
            if (current.Length == 0)
            {
                return;
            }

            var token = current.ToString().Trim('.', ';');
            current.Clear();
            if (token.Length > 0)
            {
                list.Add(token);
            }
        }
    }
}
