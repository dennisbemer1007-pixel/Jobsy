namespace Jobsy.Core.Careers;

/// <summary>Short B1 skill chips for groep 7-8 (mockup "Wat je vaak doet"), not raw ESCO wording.</summary>
public static class OccupationDaySkillsForKids
{
    private static readonly Dictionary<string, string> Replacements = new(StringComparer.OrdinalIgnoreCase)
    {
        ["specimens"] = "monsters",
        ["organismen"] = "planten en dieren",
        ["organisme"] = "plant of dier",
        ["biologisch"] = "levende",
        ["wetenschappelijke"] = "",
        ["gemeenschap"] = "groep",
        ["implementeren"] = "uitvoeren",
        ["toepassen"] = "gebruiken",
        ["analyseren"] = "bekijken",
        ["evalueren"] = "beoordelen",
        ["coördineren"] = "regelen",
        ["faciliteren"] = "helpen",
    };

    public static List<string> ToDisplayChips(IEnumerable<string>? raw, int max = 6)
    {
        var list = new List<string>();
        foreach (var line in raw ?? [])
        {
            if (list.Count >= max)
            {
                break;
            }

            var chip = Simplify(line);
            if (chip.Length < 3)
            {
                continue;
            }

            if (list.Contains(chip, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            list.Add(chip);
        }

        return list;
    }

    public static string Simplify(string? raw)
    {
        var t = (raw ?? "").Trim().TrimEnd('.');
        if (t.Length == 0)
        {
            return "";
        }

        foreach (var pair in Replacements)
        {
            if (pair.Value.Length == 0)
            {
                t = t.Replace(pair.Key, "", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                t = t.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase);
            }
        }

        t = t.Trim();
        while (t.Contains("  ", StringComparison.Ordinal))
        {
            t = t.Replace("  ", " ", StringComparison.Ordinal);
        }

        var words = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 5)
        {
            t = string.Join(' ', words.Take(5));
        }

        if (t.Length > 48)
        {
            t = t[..48].Trim();
        }

        if (t.Length == 0)
        {
            return "";
        }

        return char.ToUpper(t[0]) + t[1..];
    }
}
