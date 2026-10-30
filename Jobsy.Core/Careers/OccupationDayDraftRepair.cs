namespace Jobsy.Core.Careers;

/// <summary>
/// Fills gaps in model output from ESCO facts so validation can pass without inventing employers or places.
/// </summary>
public static class OccupationDayDraftRepair
{
    public static OccupationDayDraft Apply(OccupationDayDraft draft, OccupationDayFacts facts)
    {
        var highlights = RepairHighlights(draft.Highlights, facts);
        var varies = RepairVaries(draft.VariesNote, facts);
        return draft with { Highlights = highlights, VariesNote = varies };
    }

    private static List<string> RepairHighlights(IReadOnlyList<string> incoming, OccupationDayFacts facts)
    {
        var min = facts.IsThin ? 1 : 2;
        var max = 4;
        var list = incoming
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(NormalizeHighlight)
            .Where(line => WordCount(line) >= 2 && line.Length <= 140)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();

        foreach (var skill in facts.Skills)
        {
            if (list.Count >= min)
            {
                break;
            }

            var line = NormalizeHighlight(skill);
            if (WordCount(line) < 2)
            {
                line = "Je let op " + skill.Trim().TrimEnd('.');
            }

            line = NormalizeHighlight(line);
            if (WordCount(line) >= 2 && line.Length <= 140 && !list.Contains(line, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(line);
            }
        }

        foreach (var task in facts.Tasks)
        {
            if (list.Count >= min)
            {
                break;
            }

            var line = ShortClause(task);
            if (WordCount(line) >= 2 && line.Length <= 140 && !list.Contains(line, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(line);
            }
        }

        while (list.Count < min)
        {
            list.Add("Je werkt met taken uit dit beroep.");
        }

        return list.Take(max).ToList();
    }

    private static string RepairVaries(string? varies, OccupationDayFacts facts)
    {
        var text = (varies ?? "").Trim();
        if (text.Length >= 20
            && text.Length <= 400
            && VariesLooksOk(text))
        {
            return text;
        }

        _ = facts;
        return "De volgorde en taken kunnen per werkgever verschillen. Dat is normaal in dit beroep.";
    }

    private static bool VariesLooksOk(string text)
        => text.Contains("werkgever", StringComparison.OrdinalIgnoreCase)
           || text.Contains("verschil", StringComparison.OrdinalIgnoreCase)
           || text.Contains("niet overal", StringComparison.OrdinalIgnoreCase)
           || text.Contains("per plek", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeHighlight(string line)
    {
        var t = line.Trim().TrimEnd('.');
        if (t.Length == 0)
        {
            return "";
        }

        return char.ToUpper(t[0]) + t[1..] + ".";
    }

    private static string ShortClause(string text)
    {
        var t = text.Trim().TrimEnd('.');
        if (t.Length == 0)
        {
            return "";
        }

        var words = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 14)
        {
            t = string.Join(' ', words.Take(14));
        }

        return char.ToUpper(t[0]) + t[1..] + ".";
    }

    private static int WordCount(string text)
        => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}
