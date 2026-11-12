using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Story-first reservation of competency examples and shared pick filters.</summary>
public static class PupilG78ContentAllocation
{
    public static IReadOnlyList<string> StoryStrongCategories(
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string> growthCategories,
        int count = 3)
        => PupilG78GrowthCompetencies.TopStrongCategories(
                PupilG78GrowthCompetencies.DimensionAverages(answers), 10)
            .Where(c => !growthCategories.Contains(c))
            .Take(count)
            .ToList();

    public static IReadOnlyList<int> ReserveCompetencyExamples(
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlyList<string> topDimensions,
        IReadOnlySet<string> growthCategories,
        IReadOnlySet<int>? excludeIds = null)
    {
        var reserved = new List<int>();
        var usedThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var selected = excludeIds is not null ? new HashSet<int>(excludeIds) : new HashSet<int>();
        foreach (var id in selected)
        {
            var key = PupilG78ThemeKeys.DedupeKey(id);
            if (key is not null)
            {
                usedThemes.Add(key);
            }
        }

        foreach (var dim in topDimensions)
        {
            var blocked = PupilG78OppositePairs.BlockedItems(clauses, selected);
            G78AnswerClause? pick = null;

            foreach (var preferredId in PupilG78CompetencyExampleItems.ForCategory(dim))
            {
                var candidate = clauses.FirstOrDefault(c =>
                    c.ItemId == preferredId
                    && c.EffectiveScore >= 4
                    && !selected.Contains(c.ItemId)
                    && !blocked.Contains(c.ItemId)
                    && PupilG78OppositePairs.IsAllowed(c.ItemId, clauses, selected)
                    && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories));
                if (candidate is not null)
                {
                    pick = candidate;
                    break;
                }
            }

            pick ??= clauses
                .Where(c => c.Model == AssessmentKind.Competence
                            && c.ItemId <= PupilG78CompetencyNarrative.PositiveTileCompetenceMax
                            && PupilG78CompetencyExampleItems.ItemMatchesCategory(c.ItemId, dim)
                            && c.EffectiveScore >= 4
                            && !selected.Contains(c.ItemId)
                            && !blocked.Contains(c.ItemId)
                            && PupilG78OppositePairs.IsAllowed(c.ItemId, clauses, selected)
                            && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories))
                .Where(c =>
                {
                    var key = PupilG78ThemeKeys.DedupeKey(c.ItemId);
                    return key is null || !usedThemes.Contains(key);
                })
                .OrderByDescending(c => c.EffectiveScore)
                .ThenBy(c => c.ItemId)
                .FirstOrDefault();

            if (pick is null)
            {
                continue;
            }

            reserved.Add(pick.ItemId);
            selected.Add(pick.ItemId);
            var theme = PupilG78ThemeKeys.DedupeKey(pick.ItemId);
            if (theme is not null)
            {
                usedThemes.Add(theme);
            }
        }

        return reserved;
    }

    /// <summary>Reserve 2–3 positive items per tile before the story consumes the pool.</summary>
    public static IReadOnlyDictionary<AssessmentKind, IReadOnlyList<int>> PlanTileItems(
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlySet<string> growthCategories,
        IReadOnlySet<int> blockedIds,
        int maxPerTile = 3)
    {
        var kinds = new[]
        {
            AssessmentKind.Competence,
            AssessmentKind.Career,
            AssessmentKind.Values,
            AssessmentKind.Culture,
        };
        var result = new Dictionary<AssessmentKind, IReadOnlyList<int>>();
        var globalUsed = new HashSet<int>(blockedIds);
        var globalThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var selected = new HashSet<int>(blockedIds);
        foreach (var kind in kinds)
        {
            var picked = PickTileItems(
                clauses,
                kind,
                growthCategories,
                globalUsed,
                globalThemes,
                selected,
                maxPerTile,
                minimum: 2);
            result[kind] = picked;
            foreach (var id in picked)
            {
                globalUsed.Add(id);
                selected.Add(id);
                var key = PupilG78ThemeKeys.DedupeKey(id);
                if (key is not null)
                {
                    globalThemes.Add(key);
                }
            }
        }

        return result;
    }

    private static List<int> PickTileItems(
        IReadOnlyList<G78AnswerClause> clauses,
        AssessmentKind kind,
        IReadOnlySet<string> growthCategories,
        HashSet<int> usedIds,
        HashSet<string> usedThemes,
        HashSet<int> selected,
        int maxPerTile,
        int minimum)
    {
        var blocked = PupilG78OppositePairs.BlockedItems(clauses, selected);
        var picked = new List<int>();
        var usedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in clauses
                     .Where(c => c.Model == kind
                                 && c.EffectiveScore >= 4
                                 && (kind != AssessmentKind.Competence || c.ItemId <= PupilG78CompetencyNarrative.PositiveTileCompetenceMax)
                                 && !usedIds.Contains(c.ItemId)
                                 && !blocked.Contains(c.ItemId)
                                 && PupilG78OppositePairs.IsAllowed(c.ItemId, clauses, selected)
                                 && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories))
                     .OrderBy(c => kind == AssessmentKind.Competence && c.Beat == G78StoryBeat.Samen ? 1 : 0)
                     .ThenByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (kind != AssessmentKind.Competence && usedCategories.Contains(c.Category) && picked.Count >= 2)
            {
                continue;
            }

            var theme = PupilG78ThemeKeys.DedupeKey(c.ItemId);
            if (theme is not null && usedThemes.Contains(theme))
            {
                continue;
            }

            picked.Add(c.ItemId);
            usedIds.Add(c.ItemId);
            selected.Add(c.ItemId);
            usedCategories.Add(c.Category);

            if (picked.Count >= maxPerTile)
            {
                break;
            }
        }

        if (picked.Count < minimum)
        {
            blocked = PupilG78OppositePairs.BlockedItems(clauses, selected);
            foreach (var c in clauses
                         .Where(c => c.Model == kind
                                     && c.EffectiveScore >= 4
                                     && !usedIds.Contains(c.ItemId)
                                     && !blocked.Contains(c.ItemId)
                                     && PupilG78OppositePairs.IsAllowed(c.ItemId, clauses, selected)
                                     && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories))
                         .OrderByDescending(c => c.EffectiveScore)
                         .ThenBy(c => c.ItemId))
            {
                if (picked.Contains(c.ItemId))
                {
                    continue;
                }

                var theme = PupilG78ThemeKeys.DedupeKey(c.ItemId);
                if (theme is not null && usedThemes.Contains(theme))
                {
                    continue;
                }

                if (picked.Count >= 1 && PupilG78JeNarrative.TooSimilar(
                        clauses.First(x => x.ItemId == picked[0]).Text,
                        c.Text))
                {
                    continue;
                }

                picked.Add(c.ItemId);
                usedIds.Add(c.ItemId);
                selected.Add(c.ItemId);

                if (picked.Count >= minimum)
                {
                    break;
                }
            }
        }

        return picked;
    }
}
