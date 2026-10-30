using System.Globalization;
using System.Text.RegularExpressions;

namespace Jobsy.Core.Careers;

/// <summary>
/// Typical school-day clock labels in order (no backwards times or big gaps).
/// Times follow block key order from <see cref="OccupationDayBlocks.Normalize"/>.
/// </summary>
public static partial class OccupationDayTimeline
{
    /// <summary>Minutes from midnight for 4–8 timeline blocks (≈90 min steps; 12:30→13:30 is 60 min).</summary>
    private static readonly int[][] Schedules =
    [
        [480, 660, 750, 990],
        [480, 570, 660, 750, 870],
        [480, 570, 660, 750, 870, 990],
        [480, 570, 660, 750, 810, 900, 990],
        [480, 570, 660, 750, 810, 900, 990, 1020],
    ];

    public static IReadOnlyList<OccupationDayBlock> AssignChronologicalLabels(IReadOnlyList<OccupationDayBlock> blocks)
    {
        if (blocks is not { Count: > 0 })
        {
            return blocks ?? [];
        }

        var schedule = ScheduleFor(blocks.Count);
        var list = new List<OccupationDayBlock>(blocks.Count);
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            var word = LabelWord(block);
            var time = FormatMinutes(schedule[Math.Min(i, schedule.Length - 1)]);
            list.Add(block with { Label = time + " " + word });
        }

        return list;
    }

    public static bool TryValidate(IReadOnlyList<OccupationDayBlock> blocks, out string? reason)
    {
        reason = null;
        if (blocks is not { Count: >= 4 })
        {
            reason = "tijdlijn-te-kort";
            return false;
        }

        var minutes = new List<int>();
        foreach (var block in blocks)
        {
            if (!TryParseLabelMinutes(block.Label, out var m))
            {
                reason = "tijd-onleesbaar";
                return false;
            }

            minutes.Add(m);
        }

        for (var i = 1; i < minutes.Count; i++)
        {
            var delta = minutes[i] - minutes[i - 1];
            if (delta <= 0)
            {
                reason = "tijd-niet-stijgend";
                return false;
            }

            if (delta > 120)
            {
                reason = "tijd-gat-te-groot";
                return false;
            }

            if (delta < 30)
            {
                reason = "tijd-gat-te-klein";
                return false;
            }
        }

        return true;
    }

    private static int[] ScheduleFor(int count)
    {
        var idx = Math.Clamp(count, 4, 8) - 4;
        return Schedules[idx];
    }

    private static string LabelWord(OccupationDayBlock block)
    {
        var fromKey = OccupationDayBlocks.DefaultLabel(block.Key);
        if (fromKey.Length > 0)
        {
            return fromKey;
        }

        var label = (block.Label ?? "").Trim();
        var space = label.IndexOf(' ');
        if (space > 0 && space < label.Length - 1)
        {
            return label[(space + 1)..].Trim();
        }

        return label.Length > 0 ? label : "Moment";
    }

    private static string FormatMinutes(int totalMinutes)
    {
        var h = totalMinutes / 60;
        var m = totalMinutes % 60;
        return h.ToString("00", CultureInfo.InvariantCulture)
            + ":"
            + m.ToString("00", CultureInfo.InvariantCulture);
    }

    public static bool TryParseLabelMinutes(string? label, out int minutes)
    {
        minutes = 0;
        var text = (label ?? "").Trim();
        var match = ClockPattern().Match(text);
        if (!match.Success)
        {
            return false;
        }

        if (!int.TryParse(match.Groups["h"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var h)
            || !int.TryParse(match.Groups["m"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var m))
        {
            return false;
        }

        minutes = h * 60 + m;
        return h is >= 0 and < 24 && m is >= 0 and < 60;
    }

    [GeneratedRegex(@"^(?<h>\d{1,2})[:.](?<m>\d{2})\b", RegexOptions.CultureInvariant)]
    private static partial Regex ClockPattern();
}
