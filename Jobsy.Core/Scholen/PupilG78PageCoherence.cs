using Jobsy.Core.Contracts.Scholen;

namespace Jobsy.Core.Scholen;

/// <summary>Cross-block strong/weak checks for G78 result pages.</summary>
public static class PupilG78PageCoherence
{
    public static IReadOnlyList<int> FindDuplicateItemIds(PupilG78StoryPackage pkg)
    {
        var tileIds = pkg.Tiles
            .SelectMany(t => ExtractJeFormItemIds(t.Explanation))
            .ToList();

        var dupes = pkg.StoryItemIds
            .Intersect(tileIds)
            .Distinct()
            .ToList();

        if (dupes.Count > 0)
        {
            return dupes;
        }

        foreach (var tile in pkg.Tiles)
        {
            foreach (var sentence in SplitSentences(tile.Explanation))
            {
                if (sentence.Length > 35
                    && pkg.Body.Contains(sentence.TrimEnd('.'), StringComparison.Ordinal))
                {
                    dupes.Add(-1);
                }
            }
        }

        return dupes.Distinct().ToList();
    }

    public static IReadOnlyList<string> FindStrongWeakOverlaps(PupilG78StoryPackage pkg)
    {
        var overlaps = new List<string>();
        var pageIds = pkg.UsedItemIds.ToHashSet();

        foreach (var tile in pkg.Tiles)
        {
            if (tile.Explanation.Contains("minder", StringComparison.OrdinalIgnoreCase))
            {
                overlaps.Add("tile-minder-line");
            }

            overlaps.AddRange(FindDuplicateThemesAcrossTiles(pkg.Tiles));
        }

        overlaps.AddRange(
            PupilG78OppositePairs.FindPageTensions(pageIds)
                .Select(t => "page-" + t));

        return overlaps.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static IReadOnlyList<string> FindStrongWeakOverlaps(
        PupilG78StoryPackage pkg,
        IReadOnlyDictionary<string, int> answers)
    {
        var overlaps = new List<string>(FindStrongWeakOverlaps(pkg));
        var pageIds = pkg.UsedItemIds.ToHashSet();
        foreach (var g in pkg.GrowthCompetencies)
        {
            if (PupilG78StrengthGuard.GrowthCategoryPublishedStrong(pageIds, g.Category, answers))
            {
                overlaps.Add("growth-strong-on-page:" + g.Category);
            }
        }

        return overlaps.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static IReadOnlyList<string> FindStrongWeakOverlaps(PupilG78StoryPackage pkg, string? answersJson)
    {
        if (string.IsNullOrWhiteSpace(answersJson))
        {
            return FindStrongWeakOverlaps(pkg);
        }

        var answers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(answersJson)
                      ?? new Dictionary<string, int>(StringComparer.Ordinal);
        return FindStrongWeakOverlaps(pkg, answers);
    }

    public static IReadOnlyList<string> FindDuplicateThemesAcrossTiles(IReadOnlyList<PupilStoryTileDto> tiles)
    {
        var clashes = new List<string>();
        foreach (var tile in tiles)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in ExtractJeFormItemIds(tile.Explanation))
            {
                var key = PupilG78ThemeKeys.DedupeKey(id);
                if (key is null)
                {
                    continue;
                }

                if (!seen.Add(key))
                {
                    clashes.Add("theme-dup:" + key);
                }
            }
        }

        return clashes;
    }

    public static IEnumerable<int> ExtractJeFormItemIds(string tileExplanation)
    {
        for (var id = 9001; id <= 9060; id++)
        {
            var je = PupilG78QuestionJeForms.ForItem(id);
            if (je is not null
                && tileExplanation.Contains(je.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
            {
                yield return id;
            }
        }
    }

    /// <summary>Match g78-2 hand-written je-zinnen inside tile copy.</summary>
    public static IEnumerable<int> ExtractContrastV2ItemIds(string tileExplanation)
    {
        if (string.IsNullOrWhiteSpace(tileExplanation))
        {
            yield break;
        }

        var tileNorm = PupilG78PageLint.NormalizeSentence(tileExplanation);
        for (var id = PupilQuestionBank.MinId; id <= PupilQuestionBank.MaxId; id++)
        {
            var matched = false;
            if (PupilQuestionBankJeFormsG78V2.TryGet(id, towardB: false, out var jeA))
            {
                matched |= tileNorm.Contains(PupilG78PageLint.NormalizeSentence(jeA), StringComparison.Ordinal);
            }

            if (PupilQuestionBankJeFormsG78V2.TryGet(id, towardB: true, out var jeB))
            {
                matched |= tileNorm.Contains(PupilG78PageLint.NormalizeSentence(jeB), StringComparison.Ordinal);
            }

            if (matched)
            {
                yield return id;
            }
        }
    }

    private static IReadOnlyList<string> SplitSentences(string text)
        => text.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.TrimEnd('.'))
            .ToList();
}
