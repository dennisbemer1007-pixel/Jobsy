namespace Jobsy.Core.Scholen;

/// <summary>Warm, factual paragraph layout for "Dit ben jij" (template sentences only).</summary>
public static class PupilStoryNarrative
{
    public const string ParagraphBreak = "\n\n";

    public static string ComposeBody(IReadOnlyList<string> sentences, PupilStoryKeySet keys, string language)
    {
        if (sentences.Count == 0)
        {
            return string.Empty;
        }

        var lines = sentences
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        CoalesceAutonomyValueCulture(lines, keys, language);

        var personality = new List<string>();
        var values = new List<string>();
        var likes = new List<string>();

        if (lines.Count > 0)
        {
            personality.Add(lines[0]);
        }

        if (lines.Count > 1)
        {
            personality.Add(lines[1]);
        }

        for (var i = 2; i < lines.Count; i++)
        {
            if (lines[i].StartsWith("Je houdt van", StringComparison.Ordinal)
                || lines[i].StartsWith("You enjoy", StringComparison.OrdinalIgnoreCase)
                || lines[i].StartsWith("In je vrije tijd", StringComparison.Ordinal)
                || lines[i].StartsWith("In your free time", StringComparison.OrdinalIgnoreCase))
            {
                likes.Add(lines[i]);
            }
            else
            {
                values.Add(lines[i]);
            }
        }

        var bfExplain = TryExplain(keys.TileCompetenceKey, language);
        if (!string.IsNullOrWhiteSpace(bfExplain) && personality.Count > 0)
        {
            personality.Add(bfExplain);
        }

        var riasecExplain = TryExplain(PupilStoryTemplates.NormalizeTileRiasecKey(keys.TileRiasecKey), language);
        if (!string.IsNullOrWhiteSpace(riasecExplain) && personality.Count > 1)
        {
            personality.Add(riasecExplain);
        }

        var valExplain = TryExplain(keys.TileValueKey, language);
        if (!string.IsNullOrWhiteSpace(valExplain) && values.Count > 0)
        {
            values.Add(valExplain);
        }

        var paragraphs = new List<string>();
        var p1 = JoinSentences(personality);
        if (p1.Length > 0)
        {
            paragraphs.Add(p1);
        }

        var p2 = JoinSentences(values);
        if (p2.Length > 0)
        {
            paragraphs.Add(p2);
        }

        var p3 = JoinSentences(likes);
        if (p3.Length > 0)
        {
            paragraphs.Add(p3);
        }

        if (paragraphs.Count == 0)
        {
            return JoinSentences(lines);
        }

        return string.Join(ParagraphBreak, paragraphs);
    }

    public static IReadOnlyList<string> SplitParagraphs(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return [];
        }

        return body
            .Split(ParagraphBreak, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => p.Length > 0)
            .ToList();
    }

    public static string ForSpeakAloud(string body)
        => string.Join(' ', SplitParagraphs(body));

    private static string JoinSentences(IReadOnlyList<string> parts)
        => string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));

    private static string? TryExplain(string tileKey, string language)
    {
        var explainKey = tileKey + ".Explain";
        if (!PupilVerhaalCopy.TryGet(explainKey, out _))
        {
            return null;
        }

        return PupilVerhaalLocalized.Get(explainKey, language);
    }

    private static void CoalesceAutonomyValueCulture(
        List<string> lines,
        PupilStoryKeySet keys,
        string language)
    {
        if (lines.Count < 4)
        {
            return;
        }

        var valKey = keys.SchwartzKey ?? "";
        var cultKey = keys.CultureKey ?? "";
        if (!valKey.EndsWith(".Autonomy", StringComparison.Ordinal)
            || !cultKey.EndsWith(".Autonomy", StringComparison.Ordinal))
        {
            return;
        }

        var mergedKey = "LeerlingStory.ValCult.Autonomy";
        if (!PupilVerhaalCopy.TryGet(mergedKey, out _))
        {
            return;
        }

        lines[2] = PupilVerhaalLocalized.Get(mergedKey, language);
        lines.RemoveAt(3);
    }
}
