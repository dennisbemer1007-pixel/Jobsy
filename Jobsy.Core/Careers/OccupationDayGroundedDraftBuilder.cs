namespace Jobsy.Core.Careers;

/// <summary>Fact-only fallback draft when OpenAI is unavailable (pilot gap-fill).</summary>
public static class OccupationDayGroundedDraftBuilder
{
    public static OccupationDayDraft? TryBuild(OccupationDayFacts facts)
    {
        var lines = facts.Tasks
            .Where(t => !t.Contains("Cleaning, organizing", StringComparison.Ordinal))
            .Where(t => !t.Contains("geneesmiddelen", StringComparison.OrdinalIgnoreCase))
            .Where(t => !LooksEnglishOnly(t))
            .Select(ShortSentence)
            .Where(t => t.Length > 0)
            .ToList();

        var intro = ShortSentence(facts.Description);
        if (intro.Length > 0)
        {
            lines.Insert(0, intro);
        }

        while (lines.Count < 5 && facts.Skills.Count > 0)
        {
            var skill = facts.Skills[lines.Count % facts.Skills.Count];
            lines.Add(ShortSentence("Je let op " + skill + "."));
        }

        if (lines.Count == 0)
        {
            return null;
        }

        var keys = new[] { "start", "morning", "pause", "afternoon", "close" };
        var blocks = new List<OccupationDayBlock>();
        for (var i = 0; i < keys.Length; i++)
        {
            blocks.Add(new OccupationDayBlock(
                keys[i],
                OccupationDayBlocks.DefaultLabel(keys[i]),
                lines[Math.Min(i, lines.Count - 1)]));
        }

        blocks = OccupationDayBlocks.Normalize(blocks).ToList();
        var highlights = PickHighlights(facts);
        var draft = OccupationDayBlocks.WithDerived(new OccupationDayDraft(
            facts.TitleNl,
            "",
            "",
            "",
            "",
            highlights,
            "De volgorde en taken kunnen per werkgever verschillen. Dat is normaal in dit beroep.",
            blocks,
            facts.Tasks.Where(t => !LooksEnglishOnly(t)).Take(6).ToList(),
            facts.Skills.Take(6).ToList()));

        draft = OccupationDayDraftRepair.Apply(draft, facts);
        return OccupationDayInLifeValidator.TryValidate(draft, facts, out _)
            ? draft
            : null;
    }

    private static bool LooksEnglishOnly(string text)
    {
        var t = text.Trim();
        if (t.Length == 0)
        {
            return true;
        }

        return t.Contains(" and ", StringComparison.OrdinalIgnoreCase)
            || t.Contains(" such as ", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> PickHighlights(OccupationDayFacts facts)
    {
        var list = new List<string>();
        foreach (var skill in facts.Skills)
        {
            if (list.Count >= 2)
            {
                break;
            }

            var line = OccupationDaySkillsForKids.Simplify(skill);
            if (line.Length >= 3)
            {
                list.Add(line.EndsWith('.') ? line : line + ".");
            }
        }

        while (list.Count < 2)
        {
            list.Add("Taken uit dit beroep.");
        }

        return list.Take(4).ToList();
    }

    private static string ShortSentence(string text)
    {
        var t = OccupationDayChildLanguage.SimplifyKnownJargon(text.Trim().TrimEnd('.'));
        if (t.Length == 0)
        {
            return "";
        }

        var words = t.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 18)
        {
            t = string.Join(' ', words.Take(18));
        }

        return t.EndsWith('.') ? t : t + ".";
    }
}
