using System.Text.RegularExpressions;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

public sealed record PupilG78PageLintIssue(string Code, string Detail);

/// <summary>Quality gate on full rendered G78 result pages (story + tiles + growth copy).</summary>
public static class PupilG78PageLint
{
    private static readonly string[] BadPatterns =
    [
        "merkt het bijvoorbeeld dat",
        "Ook dat je",
        "Daarbij Je",
        "  ",
    ];

    public static IReadOnlyList<PupilG78PageLintIssue> Run(
        PupilG78StoryPackage pkg,
        IReadOnlyDictionary<string, int> answers,
        string language = "nl")
    {
        _ = language;
        var issues = new List<PupilG78PageLintIssue>();
        var growth = pkg.GrowthCompetencies.Select(g => g.Category).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pageText = RenderedPageText(pkg);
        CheckBadPatterns(pageText, issues);
        CheckTiles(pkg, issues);
        CheckDuplicates(pkg, issues);
        CheckPageWideRepeats(pkg, issues);
        CheckStoryTileParaphrase(pkg, issues);
        CheckGrowthClashes(pkg, answers, growth, issues);
        CheckCompetencyAnchors(pkg, issues);
        CheckReflexiveVerbs(pageText, issues);
        CheckStoryCultureTileTheme(pkg, issues);
        return issues;
    }

    private static string RenderedPageText(PupilG78StoryPackage pkg)
    {
        var parts = new List<string> { pkg.Body };
        foreach (var t in pkg.Tiles)
        {
            parts.Add(t.Explanation);
        }

        foreach (var g in pkg.GrowthCompetencies)
        {
            parts.Add(g.WhyExplanation);
        }

        return string.Join('\n', parts);
    }

    private static void CheckBadPatterns(string pageText, List<PupilG78PageLintIssue> issues)
    {
        var lower = pageText.ToLowerInvariant();
        foreach (var bad in BadPatterns)
        {
            if (lower.Contains(bad.ToLowerInvariant(), StringComparison.Ordinal))
            {
                issues.Add(new PupilG78PageLintIssue("bad-pattern", bad));
            }
        }

        if (Regex.IsMatch(pageText, @"\bik\b[^.]{0,120}\bhebt\b", RegexOptions.IgnoreCase))
        {
            issues.Add(new PupilG78PageLintIssue("bad-pattern", "ik … hebt"));
        }

    }

    private static void CheckTiles(PupilG78StoryPackage pkg, List<PupilG78PageLintIssue> issues)
    {
        foreach (var tile in pkg.Tiles)
        {
            if (tile.Explanation.Contains("Je gaf zelf antwoord op deze vragen", StringComparison.Ordinal))
            {
                issues.Add(new PupilG78PageLintIssue("placeholder-tile", tile.ModelKey));
            }

            var count = SplitSentences(tile.Explanation).Count(s => !IsPlaceholderSentence(s));
            if (count < 2)
            {
                issues.Add(new PupilG78PageLintIssue("tile-min-sentences", $"{tile.ModelKey}:{count}"));
            }
        }
    }

    private static void CheckStoryTileParaphrase(PupilG78StoryPackage pkg, List<PupilG78PageLintIssue> issues)
    {
        var storyIds = pkg.StoryItemIds.ToHashSet();
        foreach (var storySentence in SplitSentences(pkg.Body))
        {
            foreach (var tile in pkg.Tiles)
            {
                foreach (var tileSentence in SplitSentences(tile.Explanation))
                {
                    if (IsPlaceholderSentence(tileSentence))
                    {
                        continue;
                    }

                    var tileItemIds = ExtractItemIds(tileSentence).ToList();
                    if (tileItemIds.Count == 0)
                    {
                        continue;
                    }

                    if (!tileItemIds.Any(storyIds.Contains))
                    {
                        continue;
                    }

                    if (PupilG78JeNarrative.TooSimilar(storySentence, tileSentence))
                    {
                        issues.Add(new PupilG78PageLintIssue(
                            "story-tile-paraphrase",
                            $"{tile.ModelKey}: {Truncate(NormalizeSentence(tileSentence))}"));
                    }
                }
            }
        }
    }

    private static void CheckDuplicates(PupilG78StoryPackage pkg, List<PupilG78PageLintIssue> issues)
    {
        var storyNorms = new HashSet<string>(StringComparer.Ordinal);
        var storyThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var storyItemIds = pkg.StoryItemIds.ToHashSet();

        foreach (var s in SplitSentences(pkg.Body))
        {
            var norm = NormalizeSentence(s);
            if (norm.Length >= 20)
            {
                storyNorms.Add(norm);
            }
        }

        foreach (var id in storyItemIds)
        {
            var key = PupilG78ThemeKeys.DedupeKey(id);
            if (key is not null)
            {
                storyThemes.Add(key);
            }
        }

        foreach (var tile in pkg.Tiles)
        {
            foreach (var s in SplitSentences(tile.Explanation))
            {
                if (IsPlaceholderSentence(s))
                {
                    continue;
                }

                var norm = NormalizeSentence(s);
                if (norm.Length >= 20 && storyNorms.Contains(norm))
                {
                    issues.Add(new PupilG78PageLintIssue("dup-sentence", $"story↔{tile.ModelKey}: {Truncate(norm)}"));
                }
            }

            foreach (var id in ExtractItemIds(tile.Explanation))
            {
                if (storyItemIds.Contains(id))
                {
                    issues.Add(new PupilG78PageLintIssue("dup-item", $"story↔{tile.ModelKey}: {id}"));
                }

            }
        }
    }

    private static bool IsPlaceholderSentence(string sentence)
        => sentence.Contains("Je gaf zelf antwoord op deze vragen", StringComparison.Ordinal);

    private static void CheckPageWideRepeats(PupilG78StoryPackage pkg, List<PupilG78PageLintIssue> issues)
    {
        foreach (var tile in pkg.Tiles)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in ExtractItemIds(tile.Explanation))
            {
                var key = PupilG78ThemeKeys.DedupeKey(id);
                if (key is null)
                {
                    continue;
                }

                if (!seen.Add(key))
                {
                    issues.Add(new PupilG78PageLintIssue("dup-theme-tile", $"{tile.ModelKey}:{key}"));
                }
            }
        }

        var sentenceCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var s in SplitSentences(pkg.Body).Concat(pkg.Tiles.SelectMany(t => SplitSentences(t.Explanation))))
        {
            if (IsPlaceholderSentence(s))
            {
                continue;
            }

            var norm = NormalizeSentence(s);
            if (norm.Length < 20)
            {
                continue;
            }

            sentenceCounts.TryGetValue(norm, out var n);
            sentenceCounts[norm] = n + 1;
        }

        foreach (var (norm, count) in sentenceCounts.Where(kv => kv.Value > 1))
        {
            issues.Add(new PupilG78PageLintIssue("dup-sentence-page", Truncate(norm)));
        }
    }

    private static void CheckGrowthClashes(
        PupilG78StoryPackage pkg,
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string> growth,
        List<PupilG78PageLintIssue> issues)
    {
        foreach (var cat in growth)
        {
            if (PupilG78StrengthGuard.PagePublishesCategoryAsStrength(pkg.UsedItemIds, cat, answers))
            {
                issues.Add(new PupilG78PageLintIssue("growth-strength", cat));
            }
        }

        if (growth.Contains(CompetencyTestCatalog.Resultaatgerichtheid)
            && pkg.Body.Contains("overzicht en duidelijke stappen", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new PupilG78PageLintIssue("growth-strength", "riasec:C/plannen"));
        }
    }

    private static void CheckCompetencyAnchors(PupilG78StoryPackage pkg, List<PupilG78PageLintIssue> issues)
    {
        foreach (var anchor in pkg.CompetencyExamples)
        {
            if (!PupilG78CompetencyExampleItems.ItemMatchesCategory(anchor.ItemId, anchor.Category))
            {
                issues.Add(new PupilG78PageLintIssue(
                    "example-mismatch",
                    $"{anchor.Category} vs item {anchor.ItemId}"));
            }
        }

        var expectedLabels = CountCompetencyIntroLabels(pkg.Body);
        if (expectedLabels >= 2 && pkg.CompetencyExamples.Count < expectedLabels)
        {
            issues.Add(new PupilG78PageLintIssue(
                "competency-examples-missing",
                $"{pkg.CompetencyExamples.Count}/{expectedLabels}"));
        }
    }

    private static void CheckReflexiveVerbs(string pageText, List<PupilG78PageLintIssue> issues)
    {
        if (Regex.IsMatch(pageText, @"\bvoel je op je gemak\b", RegexOptions.IgnoreCase)
            && !pageText.Contains("voel je je op je gemak", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new PupilG78PageLintIssue("reflexive-verb", "voel je op je gemak"));
        }
    }

    private static void CheckStoryCultureTileTheme(PupilG78StoryPackage pkg, List<PupilG78PageLintIssue> issues)
    {
        var cultureTile = pkg.Tiles.FirstOrDefault(t => t.ModelKey == "culture");
        if (cultureTile is null)
        {
            return;
        }

        var storyMentionsFlexibility = pkg.Body.Contains(
            "ruimte is om dingen anders",
            StringComparison.OrdinalIgnoreCase);
        if (!storyMentionsFlexibility)
        {
            return;
        }

        foreach (var id in ExtractItemIds(cultureTile.Explanation))
        {
            if (string.Equals(PupilG78ThemeKeys.DedupeKey(id), "change_variety", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new PupilG78PageLintIssue("story-culture-theme", "change_variety"));
            }
        }
    }

    private static int CountCompetencyIntroLabels(string body)
    {
        var match = Regex.Match(
            body,
            @"Wat in je antwoorden vooral naar voren komt, is ([^.]+)\.",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return 0;
        }

        var fragment = match.Groups[1].Value;
        var parts = Regex.Split(fragment, @"\s*,\s*|\s+en\s+");
        return parts.Count(p => !string.IsNullOrWhiteSpace(p));
    }

    public static string NormalizeSentence(string sentence)
    {
        var s = sentence.Trim().TrimEnd('.').ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", " ");
        return s;
    }

    private static IEnumerable<string> SplitSentences(string text)
    {
        foreach (var part in text.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var s = part.Trim();
            if (s.Length > 0)
            {
                yield return s.EndsWith('.') ? s : s + ".";
            }
        }
    }

    private static IEnumerable<int> ExtractItemIds(string tileExplanation)
        => PupilG78PageCoherence.ExtractJeFormItemIds(tileExplanation);

    private static string Truncate(string s) => s.Length <= 72 ? s : s[..72] + "…";
}
