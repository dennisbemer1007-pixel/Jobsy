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

    public static (IReadOnlyList<PupilStoryTileDto> Tiles, IReadOnlyList<int> UsedItemIds) Build(
        IReadOnlyList<G78AnswerClause> clauses,
        string language,
        IReadOnlySet<int> reservedIds)
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
            var highs = PickHighs(clauses, kind, usedCategories, usedIds, blockedIds, usedThemes, selected);
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
        IReadOnlySet<int> selectedSoFar)
    {
        if (kind == AssessmentKind.Competence)
        {
            return PickCompetenceHighs(all, usedIds, blockedIds, usedThemes, selectedSoFar);
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
            if (picked.Count >= 4)
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
                                     && !blocked.Contains(c.ItemId))
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

        EnsureMinimumSentences(picked, all, kind, usedIds, blockedIds, usedThemes, selectedSoFar, minimum: 2);
        return picked;
    }

    private static IReadOnlyList<G78AnswerClause> PickCompetenceHighs(
        IReadOnlyList<G78AnswerClause> all,
        HashSet<int> usedIds,
        HashSet<int> blockedIds,
        HashSet<string> usedThemes,
        IReadOnlySet<int> selectedSoFar)
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
                            && !blocked.Contains(x.ItemId))
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
            if (picked.Count >= 4)
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
                                     && !blocked.Contains(x.ItemId))
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
                if (picked.Count >= 4)
                {
                    break;
                }
            }
        }

        EnsureMinimumSentences(picked, all, AssessmentKind.Competence, usedIds, blockedIds, usedThemes, selectedSoFar, minimum: 2);
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
        int minimum)
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
            if (picked.Count >= minimum)
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
