namespace Jobsy.Core.Scholen;

/// <summary>Tile copy when two chosen facts feel opposite to a child (g78-2).</summary>
internal static class PupilG78TileSentenceGlue
{
    private static readonly (int First, int Second)[] ContrastPairs =
    [
        (9003, 9008),
        (9008, 9003),
        (9003, 9049),
        (9049, 9003),
    ];

    public static string Join(IReadOnlyList<G78AnswerClause> highs, IReadOnlyList<string> sentences)
    {
        if (sentences.Count == 0)
        {
            return "";
        }

        if (sentences.Count == 1)
        {
            return sentences[0];
        }

        var parts = new List<string> { sentences[0] };
        for (var i = 1; i < sentences.Count; i++)
        {
            var id = highs[i].ItemId;
            var sentence = sentences[i];
            var needsMaar = false;
            for (var j = 0; j < i; j++)
            {
                if (NeedsContrastConnector(highs[j].ItemId, id))
                {
                    needsMaar = true;
                    break;
                }
            }

            if (needsMaar
                && !sentence.StartsWith("Maar ", StringComparison.Ordinal))
            {
                var tail = PupilG78StoryFlow.LowerFirst(sentence.TrimEnd('.'));
                sentence = "Maar " + tail + ".";
            }

            parts.Add(sentence);
        }

        return string.Join(' ', parts);
    }

    private static bool NeedsContrastConnector(int firstId, int secondId)
    {
        foreach (var (a, b) in ContrastPairs)
        {
            if (a == firstId && b == secondId)
            {
                return true;
            }
        }

        return false;
    }
}
