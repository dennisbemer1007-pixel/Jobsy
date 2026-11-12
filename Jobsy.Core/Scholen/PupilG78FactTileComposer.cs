using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Four fact-based result tiles — positive clauses only (2–4 per tile).</summary>
public static class PupilG78FactTileComposer
{
    private static readonly (string ModelKey, string LabelKey, AssessmentKind Kind)[] Slots =
    [
        ("competence", "LeerlingStory.Tile.ZoBenJij", AssessmentKind.Competence),
        ("riasec", "LeerlingStory.Tile.DitDoeJe", AssessmentKind.Career),
        ("values", "LeerlingStory.Tile.Belangrijk", AssessmentKind.Values),
        ("culture", "LeerlingStory.Tile.Thuis", AssessmentKind.Culture),
    ];

    public const int DefaultMaxSentencesPerTile = 3;

    public static (IReadOnlyList<PupilStoryTileDto> Tiles, IReadOnlyList<int> UsedItemIds) BuildFromPlan(
        IReadOnlyList<G78AnswerClause> clauses,
        string language,
        IReadOnlyDictionary<AssessmentKind, IReadOnlyList<int>> plan,
        IReadOnlySet<string> growthCategories,
        IReadOnlySet<int>? pageBlockedIds = null,
        IReadOnlySet<int>? storyItemIds = null)
    {
        var byId = clauses.ToDictionary(c => c.ItemId);
        var usedIds = new List<int>();
        var pageSelected = plan.Values.SelectMany(v => v).ToHashSet();
        if (pageBlockedIds is not null)
        {
            pageSelected.UnionWith(pageBlockedIds);
        }

        if (storyItemIds is not null)
        {
            pageSelected.UnionWith(storyItemIds);
        }

        var storyThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (storyItemIds is not null)
        {
            foreach (var sid in storyItemIds)
            {
                RememberTheme(sid, storyThemes);
            }
        }

        if (pageBlockedIds is not null)
        {
            foreach (var sid in pageBlockedIds)
            {
                RememberTheme(sid, storyThemes);
            }
        }

        var usedThemes = new HashSet<string>(storyThemes, StringComparer.OrdinalIgnoreCase);
        foreach (var id in pageSelected)
        {
            RememberTheme(id, usedThemes);
        }

        var tiles = new List<PupilStoryTileDto>();

        foreach (var (modelKey, labelKey, kind) in Slots)
        {
            if (!plan.TryGetValue(kind, out var ids) || ids.Count == 0)
            {
                tiles.Add(new PupilStoryTileDto(
                    modelKey,
                    PupilVerhaalLocalized.Get(labelKey, language),
                    "Je gaf zelf antwoord op deze vragen."));
                continue;
            }

            var highs = ids
                .Where(id => byId.ContainsKey(id)
                             && (pageBlockedIds is null || !pageBlockedIds.Contains(id))
                             && !StoryThemeConflicts(id, storyThemes))
                .Select(id => byId[id])
                .Where(c => c.EffectiveScore >= 4)
                .Take(4)
                .ToList();
            EnsureMinimumHighs(highs, clauses, kind, pageSelected, growthCategories, usedThemes, storyThemes, minimum: 2);
            foreach (var h in highs)
            {
                RememberTheme(h.ItemId, usedThemes);
            }

            usedIds.AddRange(highs.Select(h => h.ItemId));
            tiles.Add(new PupilStoryTileDto(
                modelKey,
                PupilVerhaalLocalized.Get(labelKey, language),
                ComposeTileText(highs)));
        }

        return (tiles, usedIds);
    }

    public static (IReadOnlyList<PupilStoryTileDto> Tiles, IReadOnlyList<int> UsedItemIds) Build(
        IReadOnlyList<G78AnswerClause> clauses,
        string language,
        IReadOnlySet<int> reservedIds,
        IReadOnlySet<string> growthCategories,
        int maxSentencesPerTile = DefaultMaxSentencesPerTile)
    {
        var blockedIds = new HashSet<int>(reservedIds);
        var usedIds = new HashSet<int>();
        var usedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in reservedIds)
        {
            var key = PupilG78ThemeKeys.DedupeKey(id);
            if (key is not null)
            {
                usedThemes.Add(key);
            }
        }

        var tiles = new List<PupilStoryTileDto>();
        var selected = new HashSet<int>(reservedIds);

        foreach (var (modelKey, labelKey, kind) in Slots)
        {
            var highs = PickHighs(
                clauses,
                kind,
                usedCategories,
                usedIds,
                blockedIds,
                usedThemes,
                selected,
                growthCategories,
                maxSentencesPerTile);
            foreach (var h in highs)
            {
                selected.Add(h.ItemId);
            }

            var explanation = ComposeTileText(highs);
            tiles.Add(new PupilStoryTileDto(
                modelKey,
                PupilVerhaalLocalized.Get(labelKey, language),
                explanation));
        }

        return (tiles, usedIds.ToList());
    }

    private static void EnsureMinimumHighs(
        List<G78AnswerClause> highs,
        IReadOnlyList<G78AnswerClause> clauses,
        AssessmentKind kind,
        IReadOnlySet<int> pageSelected,
        IReadOnlySet<string> growthCategories,
        HashSet<string> usedThemes,
        IReadOnlySet<string> storyThemes,
        int minimum)
    {
        if (highs.Count >= minimum)
        {
            return;
        }

        var blocked = PupilG78OppositePairs.BlockedItems(clauses, pageSelected);
        foreach (var c in clauses
                     .Where(c => c.Model == kind
                                 && c.EffectiveScore >= 4
                                 && !highs.Contains(c)
                                 && !pageSelected.Contains(c.ItemId)
                                 && !blocked.Contains(c.ItemId)
                                 && PupilG78OppositePairs.IsAllowed(c.ItemId, clauses, pageSelected)
                                 && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories))
                     .OrderByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (ThemeBlocked(c.ItemId, usedThemes) || StoryThemeConflicts(c.ItemId, storyThemes))
            {
                continue;
            }

            if (highs.Any(h => PupilG78JeNarrative.TooSimilar(h.Text, c.Text)))
            {
                continue;
            }

            highs.Add(c);
            if (highs.Count >= minimum)
            {
                break;
            }
        }

        if (highs.Count >= minimum)
        {
            return;
        }

        blocked = PupilG78OppositePairs.BlockedItems(clauses, pageSelected);
        foreach (var c in clauses
                     .Where(c => c.Model == kind
                                 && c.EffectiveScore >= 4
                                 && !highs.Any(h => h.ItemId == c.ItemId)
                                 && !pageSelected.Contains(c.ItemId)
                                 && !blocked.Contains(c.ItemId)
                                 && PupilG78OppositePairs.IsAllowed(c.ItemId, clauses, pageSelected)
                                 && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories))
                     .OrderByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (ThemeBlocked(c.ItemId, usedThemes) || StoryThemeConflicts(c.ItemId, storyThemes))
            {
                continue;
            }

            if (highs.Any(h => PupilG78JeNarrative.TooSimilar(h.Text, c.Text)))
            {
                continue;
            }

            highs.Add(c);
            if (highs.Count >= minimum)
            {
                break;
            }
        }
    }

    private static bool StoryThemeConflicts(int itemId, IReadOnlySet<string> storyThemes)
    {
        if (storyThemes.Count == 0)
        {
            return false;
        }

        var key = PupilG78ThemeKeys.DedupeKey(itemId);
        return key is not null && storyThemes.Contains(key);
    }

    private static string ComposeTileText(IReadOnlyList<G78AnswerClause> highs)
    {
        if (highs.Count == 0)
        {
            return "Je gaf zelf antwoord op deze vragen.";
        }

        var sentences = highs
            .Select(h => PupilG78QuestionJeForms.ForItem(h.ItemId) ?? PupilG78JeNarrative.AsSentence(h.Text))
            .ToList();
        return string.Join(' ', sentences);
    }

    private static IReadOnlyList<G78AnswerClause> PickHighs(
        IReadOnlyList<G78AnswerClause> all,
        AssessmentKind kind,
        HashSet<string> usedCategories,
        HashSet<int> usedIds,
        HashSet<int> blockedIds,
        HashSet<string> usedThemes,
        IReadOnlySet<int> selectedSoFar,
        IReadOnlySet<string> growthCategories,
        int maxSentences)
    {
        if (kind == AssessmentKind.Competence)
        {
            return PickCompetenceHighs(all, usedIds, blockedIds, usedThemes, selectedSoFar, growthCategories, maxSentences);
        }

        var blocked = PupilG78OppositePairs.BlockedItems(all, selectedSoFar);
        var picked = new List<G78AnswerClause>();

        foreach (var c in all
                     .Where(c => c.Model == kind
                                 && c.EffectiveScore >= 4
                                 && (c.Model != AssessmentKind.Competence || c.ItemId <= PupilG78CompetencyNarrative.PositiveTileCompetenceMax)
                                 && !usedIds.Contains(c.ItemId)
                                 && !blockedIds.Contains(c.ItemId)
                                 && !blocked.Contains(c.ItemId)
                                 && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories)
                                 && PupilG78OppositePairs.IsAllowed(c.ItemId, all, selectedSoFar.Union(picked.Select(p => p.ItemId)).ToHashSet()))
                     .OrderByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (usedCategories.Contains(c.Category) && picked.Count >= 1)
            {
                continue;
            }

            if (ThemeBlocked(c.ItemId, usedThemes))
            {
                continue;
            }

            if (picked.Count > 0 && PupilG78JeNarrative.TooSimilar(picked[0].Text, c.Text))
            {
                continue;
            }

            picked.Add(c);
            usedCategories.Add(c.Category);
            usedIds.Add(c.ItemId);
            RememberTheme(c.ItemId, usedThemes);
            if (picked.Count >= maxSentences)
            {
                break;
            }
        }

        if (picked.Count < 2)
        {
            foreach (var c in all
                         .Where(c => c.Model == kind
                                     && c.EffectiveScore == 4
                                     && !usedIds.Contains(c.ItemId)
                                     && !blockedIds.Contains(c.ItemId)
                                     && !blocked.Contains(c.ItemId)
                                     && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories))
                         .OrderBy(c => c.ItemId))
            {
                if (ThemeBlocked(c.ItemId, usedThemes))
                {
                    continue;
                }

                if (picked.Count > 0 && PupilG78JeNarrative.TooSimilar(picked[0].Text, c.Text))
                {
                    continue;
                }

                picked.Add(c);
                usedIds.Add(c.ItemId);
                RememberTheme(c.ItemId, usedThemes);
                if (picked.Count >= 2)
                {
                    break;
                }
            }
        }

        EnsureMinimumSentences(
            picked,
            all,
            kind,
            usedIds,
            blockedIds,
            usedThemes,
            selectedSoFar,
            growthCategories,
            minimum: 2,
            maxSentences);
        return picked;
    }

    private static IReadOnlyList<G78AnswerClause> PickCompetenceHighs(
        IReadOnlyList<G78AnswerClause> all,
        HashSet<int> usedIds,
        HashSet<int> blockedIds,
        HashSet<string> usedThemes,
        IReadOnlySet<int> selectedSoFar,
        IReadOnlySet<string> growthCategories,
        int maxSentences)
    {
        var blocked = PupilG78OppositePairs.BlockedItems(all, selectedSoFar);
        var picked = new List<G78AnswerClause>();

        foreach (var cat in CompetencyTestCatalog.QuickScanCategories)
        {
            var c = all
                .Where(x => x.Model == AssessmentKind.Competence
                            && x.ItemId <= PupilG78CompetencyNarrative.PositiveTileCompetenceMax
                            && string.Equals(x.Category, cat, StringComparison.OrdinalIgnoreCase)
                            && x.EffectiveScore >= 4
                            && !usedIds.Contains(x.ItemId)
                            && !blockedIds.Contains(x.ItemId)
                            && !blocked.Contains(x.ItemId)
                            && !PupilG78StrengthGuard.IsForbiddenStrength(x.ItemId, x.EffectiveScore, growthCategories))
                .Where(x => !ThemeBlocked(x.ItemId, usedThemes))
                .OrderByDescending(x => x.EffectiveScore)
                .ThenBy(x => x.ItemId == 9001 ? 1 : 0)
                .ThenBy(x => x.ItemId)
                .FirstOrDefault();

            if (c is null)
            {
                continue;
            }

            if (picked.Any(p => PupilG78JeNarrative.TooSimilar(p.Text, c.Text)))
            {
                continue;
            }

            picked.Add(c);
            usedIds.Add(c.ItemId);
            RememberTheme(c.ItemId, usedThemes);
            if (picked.Count >= maxSentences)
            {
                break;
            }
        }

        if (picked.Count < 2)
        {
            foreach (var c in all
                         .Where(x => x.Model == AssessmentKind.Competence
                                     && x.ItemId <= PupilG78CompetencyNarrative.PositiveTileCompetenceMax
                                     && x.EffectiveScore >= 4
                                     && !usedIds.Contains(x.ItemId)
                                     && !blockedIds.Contains(x.ItemId)
                                     && !blocked.Contains(x.ItemId)
                                     && !PupilG78StrengthGuard.IsForbiddenStrength(x.ItemId, x.EffectiveScore, growthCategories))
                         .OrderByDescending(x => x.EffectiveScore)
                         .ThenBy(x => x.ItemId))
            {
                if (ThemeBlocked(c.ItemId, usedThemes))
                {
                    continue;
                }

                if (picked.Any(p => PupilG78JeNarrative.TooSimilar(p.Text, c.Text)))
                {
                    continue;
                }

                picked.Add(c);
                usedIds.Add(c.ItemId);
                RememberTheme(c.ItemId, usedThemes);
                if (picked.Count >= maxSentences)
                {
                    break;
                }
            }
        }

        EnsureMinimumSentences(
            picked,
            all,
            AssessmentKind.Competence,
            usedIds,
            blockedIds,
            usedThemes,
            selectedSoFar,
            growthCategories,
            minimum: 2,
            maxSentences);
        return picked;
    }

    private static void EnsureMinimumSentences(
        List<G78AnswerClause> picked,
        IReadOnlyList<G78AnswerClause> all,
        AssessmentKind kind,
        HashSet<int> usedIds,
        HashSet<int> blockedIds,
        HashSet<string> usedThemes,
        IReadOnlySet<int> selectedSoFar,
        IReadOnlySet<string> growthCategories,
        int minimum,
        int maxSentences)
    {
        if (picked.Count >= minimum)
        {
            return;
        }

        var blocked = PupilG78OppositePairs.BlockedItems(all, selectedSoFar.Union(picked.Select(p => p.ItemId)).ToHashSet());
        foreach (var c in all
                     .Where(c => c.Model == kind
                                 && c.EffectiveScore >= 4
                                 && !usedIds.Contains(c.ItemId)
                                 && !blockedIds.Contains(c.ItemId)
                                 && !blocked.Contains(c.ItemId)
                                 && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories)
                                 && !picked.Contains(c))
                     .OrderByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (ThemeBlocked(c.ItemId, usedThemes))
            {
                continue;
            }

            if (picked.Any(p => PupilG78JeNarrative.TooSimilar(p.Text, c.Text)))
            {
                continue;
            }

            picked.Add(c);
            usedIds.Add(c.ItemId);
            RememberTheme(c.ItemId, usedThemes);
            if (picked.Count >= maxSentences)
            {
                break;
            }
        }
    }

    private static bool ThemeBlocked(int itemId, HashSet<string> usedThemes)
    {
        var key = PupilG78ThemeKeys.DedupeKey(itemId);
        return key is not null && usedThemes.Contains(key);
    }

    private static void RememberTheme(int itemId, HashSet<string> usedThemes)
    {
        var key = PupilG78ThemeKeys.DedupeKey(itemId);
        if (key is not null)
        {
            usedThemes.Add(key);
        }
    }
}
