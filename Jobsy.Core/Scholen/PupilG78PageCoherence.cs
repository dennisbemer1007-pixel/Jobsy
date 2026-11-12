using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Cross-block strong/weak checks for G78 result pages.</summary>
public static class PupilG78PageCoherence
{
    public static IReadOnlyList<int> FindDuplicateItemIds(PupilG78StoryPackage pkg)
    {
        var storyIds = Regex.Matches(
                pkg.Body,
                @"\b90[0-5][0-9]\b",
                RegexOptions.CultureInvariant)
            .Select(m => int.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        // Story ids tracked via package UsedItemIds split: story = used - tiles
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

        // Text-level: same canonical sentence in story and tile
        foreach (var tile in pkg.Tiles)
        {
            foreach (var sentence in SplitSentences(tile.Explanation))
            {
                if (sentence.Length > 35
                    && !sentence.Contains("minder", StringComparison.OrdinalIgnoreCase)
                    && pkg.Body.Contains(sentence.TrimEnd('.'), StringComparison.Ordinal))
                {
                    dupes.Add(-1);
                }
            }
        }

        return dupes.Distinct().ToList();
    }

    public static IReadOnlyList<string> FindStrongWeakOverlaps(
        PupilG78StoryPackage pkg,
        IReadOnlyDictionary<string, int> answers)
    {
        var overlaps = new List<string>();
        var clauses = BuildClauses(answers);
        var publishedStrong = PublishedStrongIds(pkg, clauses);
        var growthCats = pkg.GrowthCompetencies.Select(g => g.Category).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var g in pkg.GrowthCompetencies)
        {
            if (!PupilG78ItemThemes.GrowthCategoryAllowed(g.Category, publishedStrong, clauses))
            {
                overlaps.Add("growth:" + g.Category);
            }

            var minder = PupilG78CompetencyNarrative.MinderNaarVoren(g.Category);
            if (pkg.Tiles.Any(t => t.Explanation.Contains(minder, StringComparison.OrdinalIgnoreCase)))
            {
                overlaps.Add("growth-tile:" + g.Category);
            }
        }

        foreach (var tile in pkg.Tiles)
        {
            overlaps.AddRange(FindTileInternalThemeClashes(tile.Explanation));
            var tileStrongIds = ExtractJeFormItemIds(tile.Explanation)
                .Where(id =>
                {
                    var je = PupilG78QuestionJeForms.ForItem(id);
                    return je is not null
                           && tile.Explanation.Contains(je.TrimEnd('.'), StringComparison.Ordinal)
                           && !SplitSentences(tile.Explanation)
                               .Any(s => s.Contains("minder", StringComparison.OrdinalIgnoreCase)
                                         && s.Contains(je.TrimEnd('.'), StringComparison.Ordinal));
                })
                .ToHashSet();
            var lows = SplitSentences(tile.Explanation)
                .Where(s => s.Contains("minder", StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var low in lows)
            {
                if (PupilG78ItemThemes.ContrastConflictsStrength(low, tileStrongIds, clauses))
                {
                    overlaps.Add("tile-weak:" + low);
                }
            }
        }

        foreach (var cat in CompetencyTestCatalog.QuickScanCategories)
        {
            var kid = PupilG78CompetencyNarrative.KidLabel(cat);
            var weakInTiles = pkg.Tiles.Any(t =>
                t.Explanation.Contains("minder", StringComparison.OrdinalIgnoreCase)
                && t.Explanation.Contains(kid, StringComparison.OrdinalIgnoreCase));
            var strongPublished = clauses.Any(c =>
                publishedStrong.Contains(c.ItemId)
                && c.EffectiveScore >= 4
                && string.Equals(c.Category, cat, StringComparison.OrdinalIgnoreCase));
            if (weakInTiles && strongPublished)
            {
                overlaps.Add(kid);
            }
        }

        return overlaps.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static IReadOnlyList<string> FindStrongWeakOverlaps(PupilG78StoryPackage pkg, string? answersJson)
    {
        var answers = ParseAnswers(answersJson);
        return FindStrongWeakOverlaps(pkg, answers);
    }

    private static List<G78AnswerClause> BuildClauses(IReadOnlyDictionary<string, int> answers)
    {
        var bank = new PupilQuestionBank();
        var list = new List<G78AnswerClause>();
        foreach (var item in bank.AllItems)
        {
            if (!answers.TryGetValue(item.Id, out var raw))
            {
                continue;
            }

            var q = bank.Questions.FirstOrDefault(x => x.Id == item.NumericId);
            if (q is null)
            {
                continue;
            }

            var eff = PupilQuestionStoryParaphrase.EffectiveScore(raw, q.Reverse);
            list.Add(new G78AnswerClause(q.Id, q.Category, q.Model, G78StoryBeat.Doen, "", eff));
        }

        return list;
    }

    private static IEnumerable<int> ExtractJeFormItemIds(string tileExplanation)
    {
        for (var id = 9001; id <= 9060; id++)
        {
            var je = PupilG78QuestionJeForms.ForItem(id);
            if (je is not null
                && tileExplanation.Contains(je.TrimEnd('.'), StringComparison.Ordinal))
            {
                yield return id;
            }
        }
    }

    private static List<string> FindTileInternalThemeClashes(string tileText)
    {
        var clashes = new List<string>();
        var sentences = SplitSentences(tileText);
        var highs = sentences.Where(s => !s.Contains("minder", StringComparison.OrdinalIgnoreCase)).ToList();
        var lows = sentences.Where(s => s.Contains("minder", StringComparison.OrdinalIgnoreCase)).ToList();
        if (lows.Count > 1)
        {
            clashes.Add("tile-multiple-low");
        }

        foreach (var low in lows)
        {
            var lowText = low.ToLowerInvariant();
            foreach (var high in highs)
            {
                var highText = high.ToLowerInvariant();
                foreach (var (weak, strong) in PupilG78AnswerConflicts.ThemeClashTokens)
                {
                    if (lowText.Contains(weak, StringComparison.Ordinal) && highText.Contains(strong, StringComparison.Ordinal))
                    {
                        clashes.Add(weak);
                    }
                }
            }
        }

        return clashes;
    }

    private static IReadOnlyList<string> SplitSentences(string text)
        => text.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.TrimEnd('.'))
            .ToList();

    private static HashSet<int> PublishedStrongIds(PupilG78StoryPackage pkg, List<G78AnswerClause> clauses)
    {
        var tileIds = pkg.Tiles.SelectMany(t => ExtractJeFormItemIds(t.Explanation)).ToHashSet();
        return clauses
            .Where(c => c.EffectiveScore >= 4
                        && (pkg.StoryItemIds.Contains(c.ItemId) || tileIds.Contains(c.ItemId)))
            .Select(c => c.ItemId)
            .ToHashSet();
    }

    private static Dictionary<string, int> ParseAnswers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
               ?? new Dictionary<string, int>(StringComparer.Ordinal);
    }
}
