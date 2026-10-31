namespace Jobsy.Core.Careers;

/// <summary>Guards groep 7-8 copy (B1, no academic jargon).</summary>
public static class OccupationDayChildLanguage
{
    private static readonly string[] Forbidden =
    [
        "specimens",
        "specimen",
        "organismen",
        "organisme",
        "wetenschappelijke gemeenschap",
        "wetenschappelijk onderzoek",
        "hypothese",
        "methodologie",
        "paradigma",
        "implementeren",
        "faciliteren",
        "stakeholder",
        "compliance",
        "audit trail",
    ];

    public static bool ContainsForbiddenJargon(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (var word in Forbidden)
        {
            if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string SimplifyKnownJargon(string? text)
    {
        var t = text ?? "";
        if (t.Length == 0)
        {
            return t;
        }

        t = t.Replace("specimens", "monsters", StringComparison.OrdinalIgnoreCase);
        t = t.Replace("specimen", "monster", StringComparison.OrdinalIgnoreCase);
        t = t.Replace("organismen", "planten en dieren", StringComparison.OrdinalIgnoreCase);
        t = t.Replace("organisme", "plant of dier", StringComparison.OrdinalIgnoreCase);
        t = t.Replace("wetenschappelijke gemeenschap", "andere onderzoekers", StringComparison.OrdinalIgnoreCase);
        t = t.Replace("wetenschappelijke papers", "verslagen over je onderzoek", StringComparison.OrdinalIgnoreCase);
        t = t.Replace("wetenschappelijk onderzoek", "onderzoek in de praktijk", StringComparison.OrdinalIgnoreCase);
        return t;
    }

    public static IReadOnlyList<string> FindForbiddenInDraft(OccupationDayDraft draft)
    {
        var found = new List<string>();
        void Scan(string? part)
        {
            if (ContainsForbiddenJargon(part))
            {
                found.Add("taal-te-moeilijk");
            }
        }

        Scan(draft.Morning);
        Scan(draft.Midday);
        Scan(draft.Afternoon);
        Scan(draft.Closing);
        Scan(draft.VariesNote);
        foreach (var h in draft.Highlights)
        {
            Scan(h);
        }

        foreach (var block in draft.Blocks ?? [])
        {
            Scan(block.Text);
            Scan(block.Label);
        }

        return found.Distinct(StringComparer.Ordinal).ToList();
    }
}
