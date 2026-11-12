using System.Text.Json;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Cross-block strong/weak checks for G78 result pages.</summary>
public static class PupilG78PageCoherence
{
    public static IReadOnlyList<string> FindStrongWeakOverlaps(
        PupilG78StoryPackage pkg,
        IReadOnlyDictionary<string, int> answers)
    {
        var overlaps = new List<string>();
        var avgs = PupilG78GrowthCompetencies.DimensionAverages(answers);
        var growthCats = pkg.GrowthCompetencies.Select(g => g.Category).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var storyStrong = PupilG78GrowthCompetencies.TopStrongCategories(avgs, 5)
            .Where(c => !growthCats.Contains(c))
            .Take(3)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var cat in storyStrong)
        {
            if (growthCats.Contains(cat))
            {
                overlaps.Add(PupilG78CompetencyNarrative.KidLabel(cat));
            }
        }

        foreach (var cat in CompetencyTestCatalog.QuickScanCategories)
        {
            var kid = PupilG78CompetencyNarrative.KidLabel(cat);
            var weakInTiles = pkg.Tiles.Any(t =>
                t.Explanation.Contains("minder", StringComparison.OrdinalIgnoreCase)
                && t.Explanation.Contains(kid, StringComparison.OrdinalIgnoreCase));
            var strongInStory = pkg.Body.Contains(kid, StringComparison.OrdinalIgnoreCase)
                                && pkg.Body.Contains("vooral", StringComparison.OrdinalIgnoreCase);
            if (weakInTiles && (storyStrong.Contains(cat) || strongInStory))
            {
                overlaps.Add(kid);
            }
        }

        foreach (var tile in pkg.Tiles)
        {
            overlaps.AddRange(FindTileInternalThemeClashes(tile.Explanation));
        }

        return overlaps.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static IReadOnlyList<string> FindStrongWeakOverlaps(PupilG78StoryPackage pkg, string? answersJson)
    {
        var answers = ParseAnswers(answersJson);
        return FindStrongWeakOverlaps(pkg, answers);
    }

    private static List<string> FindTileInternalThemeClashes(string tileText)
    {
        var clashes = new List<string>();
        var sentences = tileText.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
