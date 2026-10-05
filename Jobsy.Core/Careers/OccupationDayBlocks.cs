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

    public const int MinNormal = 6;
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
}
