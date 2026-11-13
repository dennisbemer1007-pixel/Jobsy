using System.Text.RegularExpressions;
using Jobsy.Core.Contracts.Scholen;

namespace Jobsy.Core.Scholen;

/// <summary>Each fact sentence at most once on the result page (story + tiles).</summary>
public static class PupilG78PageTextDedupe
{
    /// <summary>Final pass: strip story sentences that also appear on tiles (after polish).</summary>
    public static string ApplyFullStoryTileDedupe(
        string body,
        IReadOnlyList<PupilStoryTileDto> tiles,
        IReadOnlySet<int> tileItemIds,
        IReadOnlyList<G78AnswerClause> clauses)
    {
        var tileNorms = CollectTileSentenceNorms(tiles, tileItemIds, clauses);
        if (tileNorms.Count == 0)
        {
            return body;
        }

        return ScrubStoryNorms(body, tileNorms);
    }

    /// <summary>Removes from the story any sentence that already appears on a tile; tiles stay intact.</summary>
    public static string ScrubStoryAgainstTileExplanations(
        string body,
        IReadOnlyList<PupilStoryTileDto> tiles)
        => ApplyFullStoryTileDedupe(body, tiles, new HashSet<int>(), []);

    internal static HashSet<string> CollectTileSentenceNorms(
        IReadOnlyList<PupilStoryTileDto> tiles,
        IReadOnlySet<int> tileItemIds,
        IReadOnlyList<G78AnswerClause> clauses)
    {
        var tileNorms = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tile in tiles)
        {
            var rendered = FinalizeRenderedText(tile.Explanation);
            foreach (var sentence in SplitRenderedSentences(rendered))
            {
                AddNorm(tileNorms, sentence);
            }
        }

        var byId = clauses.ToDictionary(c => c.ItemId);
        foreach (var id in tileItemIds)
        {
            if (byId.TryGetValue(id, out var clause))
            {
                AddNorm(tileNorms, FinalizeRenderedText(clause.Text));
            }

            if (PupilQuestionBankJeFormsG78V2.TryGet(id, towardB: false, out var jeA))
            {
                AddNorm(tileNorms, jeA);
            }

            if (PupilQuestionBankJeFormsG78V2.TryGet(id, towardB: true, out var jeB))
            {
                AddNorm(tileNorms, jeB);
            }
        }

        foreach (var tile in tiles)
        {
            foreach (var id in PupilG78PageCoherence.ExtractContrastV2ItemIds(tile.Explanation))
            {
                if (byId.TryGetValue(id, out var clause))
                {
                    AddNorm(tileNorms, FinalizeRenderedText(clause.Text));
                }
            }
        }

        return tileNorms;
    }

    private static void AddNorm(HashSet<string> tileNorms, string sentence)
    {
        var norm = PupilG78PageLint.NormalizeSentence(sentence);
        if (norm.Length >= 12)
        {
            tileNorms.Add(norm);
        }
    }

    internal static string ScrubStoryNorms(string body, IReadOnlySet<string> forbiddenNorms)
    {
        var paragraphs = body.Split(PupilG78FactStory.ParagraphBreak, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var scrubbed = new List<string>();
        foreach (var para in paragraphs)
        {
            var renderedPara = FinalizeRenderedText(para);
            var kept = new List<string>();
            foreach (var sentence in SplitRenderedSentences(renderedPara))
            {
                var norm = PupilG78PageLint.NormalizeSentence(sentence);
                if (norm.Length >= 12 && forbiddenNorms.Contains(norm))
                {
                    continue;
                }

                kept.Add(sentence);
            }

            if (kept.Count > 0)
            {
                scrubbed.Add(PupilG78StoryFlow.EnsureSentenceStartsCapitalized(PupilG78StoryFlow.FlowJoin(kept)));
            }
        }

        return string.Join(PupilG78FactStory.ParagraphBreak, scrubbed);
    }

    internal static string FinalizeRenderedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        return PupilG78StoryFlow.EnsureSentenceStartsCapitalized(
            PupilQuestionStoryParaphrase.PolishJeForm(text.Trim()));
    }

    internal static IEnumerable<string> SplitRenderedSentences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        foreach (var part in Regex.Split(text, @"(?<=\.)\s+"))
        {
            var s = part.Trim();
            if (s.Length == 0)
            {
                continue;
            }

            yield return s.EndsWith('.') ? s : s + ".";
        }
    }
}
