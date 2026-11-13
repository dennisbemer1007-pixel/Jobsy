using Jobsy.Core.Contracts.Scholen;

namespace Jobsy.Core.Scholen;

/// <summary>Each fact sentence at most once on the result page (story + tiles).</summary>
internal static class PupilG78PageTextDedupe
{
    /// <summary>Removes from the story any sentence that already appears on a tile; tiles stay intact.</summary>
    public static string ScrubStoryAgainstTileExplanations(
        string body,
        IReadOnlyList<PupilStoryTileDto> tiles)
    {
        var tileNorms = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tile in tiles)
        {
            foreach (var sentence in SplitSentences(tile.Explanation))
            {
                var norm = PupilG78PageLint.NormalizeSentence(sentence);
                if (norm.Length >= 12)
                {
                    tileNorms.Add(norm);
                }
            }
        }

        if (tileNorms.Count == 0)
        {
            return body;
        }

        var paragraphs = body.Split(PupilG78FactStory.ParagraphBreak, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var scrubbed = new List<string>();
        foreach (var para in paragraphs)
        {
            var kept = new List<string>();
            foreach (var sentence in SplitSentences(para))
            {
                var norm = PupilG78PageLint.NormalizeSentence(sentence);
                if (norm.Length >= 12 && tileNorms.Contains(norm))
                {
                    continue;
                }

                kept.Add(sentence);
            }

            scrubbed.Add(kept.Count > 0 ? PupilG78StoryFlow.FlowJoin(kept) : para);
        }

        return string.Join(
            PupilG78FactStory.ParagraphBreak,
            scrubbed.Select(PupilG78StoryFlow.EnsureSentenceStartsCapitalized));
    }

    private static IEnumerable<string> SplitSentences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        foreach (var part in text.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var s = part.Trim();
            if (s.Length > 0)
            {
                yield return s.EndsWith('.') ? s : s + ".";
            }
        }
    }
}
