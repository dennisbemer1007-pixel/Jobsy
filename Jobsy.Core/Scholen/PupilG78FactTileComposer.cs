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
        IReadOnlySet<int>? storyItemIds = null,
        IReadOnlyDictionary<string, int>? answers = null,
        bool contrastV2 = false)
    {
        var byId = clauses.ToDictionary(c => c.ItemId);
        var usedIds = new List<int>();
        var reservedIds = pageBlockedIds?.ToHashSet() ?? [];
        var pageSelected = plan.Values.SelectMany(v => v).ToHashSet();
        pageSelected.UnionWith(reservedIds);
        var oppositeContext = new HashSet<int>(pageSelected);
        if (storyItemIds is not null)
        {
            oppositeContext.UnionWith(storyItemIds);
        }

        var storyThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (storyItemIds is not null)
        {
            foreach (var sid in storyItemIds)
            {
                RememberTheme(sid, storyThemes);
            }
        }

        var tileThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var tiles = new List<PupilStoryTileDto>();

        foreach (var (modelKey, labelKey, kind) in Slots)
        {
            plan.TryGetValue(kind, out var ids);
            ids ??= [];

            var tileLocalThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var highs = new List<G78AnswerClause>();
            foreach (var id in ids)
            {
                if (!byId.TryGetValue(id, out var clause)
                    || clause.EffectiveScore < 4
                    || (pageBlockedIds is not null && pageBlockedIds.Contains(id))
                    || StoryThemeConflicts(id, storyThemes)
                    || ThemeBlocked(id, tileLocalThemes))
                {
                    continue;
                }

                highs.Add(clause);
                RememberTheme(id, tileLocalThemes);
                if (highs.Count >= 4)
                {
                    break;
                }
            }
            FillTileToMinimum(highs, clauses, kind, oppositeContext, reservedIds, growthCategories, tileLocalThemes, storyThemes, storyItemIds, answers, minimum: 2, contrastV2);
            foreach (var h in highs)
            {
                RememberTheme(h.ItemId, tileThemes);
                RememberTheme(h.ItemId, tileLocalThemes);
            }

            if (highs.Count < 2 && answers is not null)
            {
                TryFillFromAnswerBank(kind, answers, growthCategories, reservedIds, storyItemIds, tileLocalThemes, highs, 2, contrastV2);
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

    private static void FillTileToMinimum(
        List<G78AnswerClause> highs,
        IReadOnlyList<G78AnswerClause> clauses,
        AssessmentKind kind,
        IReadOnlySet<int> oppositeContext,
        IReadOnlySet<int> reservedIds,
        IReadOnlySet<string> growthCategories,
        HashSet<string> tileThemes,
        IReadOnlySet<string> storyThemes,
        IReadOnlySet<int>? storyItemIds,
        IReadOnlyDictionary<string, int>? answers,
        int minimum,
        bool contrastV2 = false)
    {
        TryFillHighs(highs, clauses, kind, oppositeContext, reservedIds, growthCategories, tileThemes, storyThemes, storyItemIds, minimum, TilePickMode.Strict);
        if (highs.Count < minimum)
        {
            TryFillHighs(highs, clauses, kind, oppositeContext, reservedIds, growthCategories, tileThemes, storyThemes, storyItemIds, minimum, TilePickMode.RelaxStoryThemes);
        }

        if (highs.Count < minimum)
        {
            TryFillHighs(highs, clauses, kind, oppositeContext, reservedIds, growthCategories, tileThemes, storyThemes, storyItemIds, minimum, TilePickMode.RelaxTileThemes);
        }

        if (highs.Count < minimum)
        {
            TryFillHighsFromAnyHigh(clauses, kind, growthCategories, reservedIds, storyItemIds, highs, minimum);
        }

        if (contrastV2 && highs.Count < minimum)
        {
            ContrastFillTileFromClauses(kind, clauses, reservedIds, storyItemIds, growthCategories, highs, minimum);
        }

        if (highs.Count < minimum && answers is not null)
        {
            TryFillFromAnswerBank(kind, answers, growthCategories, reservedIds, storyItemIds, tileThemes, highs, minimum, contrastV2);
        }

        if (highs.Count < minimum)
        {
            TryFillDesperation(highs, clauses, kind, oppositeContext, reservedIds, growthCategories, storyItemIds, minimum);
        }

        if (kind == AssessmentKind.Culture && highs.Count < minimum)
        {
            TryFillCultureFallback(highs, clauses, growthCategories, reservedIds, storyItemIds, oppositeContext, minimum);
        }

    }

    private static readonly int[] CultureFallbackIds =
        [9049, 9054, 9048, 9058, 9052, 9060, 9055, 9059, 9047, 9053, 9046];

    private static void TryFillCultureFallback(
        List<G78AnswerClause> highs,
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlySet<string> growthCategories,
        IReadOnlySet<int> reservedIds,
        IReadOnlySet<int>? storyItemIds,
        IReadOnlySet<int> oppositeContext,
        int minimum)
    {
        var byId = clauses.ToDictionary(c => c.ItemId);
        foreach (var id in CultureFallbackIds)
        {
            if (highs.Count >= minimum)
            {
                break;
            }

            if (!byId.TryGetValue(id, out var clause)
                || clause.EffectiveScore < 4
                || highs.Any(h => h.ItemId == id)
                || reservedIds.Contains(id)
                || (storyItemIds?.Contains(id) ?? false)
                || PupilG78StrengthGuard.IsForbiddenStrength(id, clause.EffectiveScore, growthCategories)
                || PupilG78OppositePairs.WouldCreatePageTension(id, oppositeContext))
            {
                continue;
            }

            if (highs.Any(h => PupilG78JeNarrative.TooSimilar(h.Text, clause.Text)))
            {
                continue;
            }

            highs.Add(clause);
        }
    }

    private static void TryFillDesperation(
        List<G78AnswerClause> highs,
        IReadOnlyList<G78AnswerClause> clauses,
        AssessmentKind kind,
        IReadOnlySet<int> oppositeContext,
        IReadOnlySet<int> reservedIds,
        IReadOnlySet<string> growthCategories,
        IReadOnlySet<int>? storyItemIds,
        int minimum)
    {
        foreach (var c in clauses
                     .Where(c => c.Model == kind
                                 && c.EffectiveScore >= 4
                                 && !highs.Any(h => h.ItemId == c.ItemId)
                                 && !(storyItemIds?.Contains(c.ItemId) ?? false)
                                 && !reservedIds.Contains(c.ItemId)
                                 && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories)
                                 && !PupilG78OppositePairs.WouldCreatePageTension(c.ItemId, oppositeContext))
                     .OrderByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
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

    private static void ContrastFillTileFromClauses(
        AssessmentKind kind,
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlySet<int> reservedIds,
        IReadOnlySet<int>? storyItemIds,
        IReadOnlySet<string> growthCategories,
        List<G78AnswerClause> highs,
        int minimum)
    {
        void Pick(bool allowStoryOverlap)
        {
            foreach (var c in clauses
                         .Where(c => c.Model == kind && c.EffectiveScore >= 4)
                         .OrderByDescending(c => c.EffectiveScore)
                         .ThenBy(c => c.ItemId))
            {
                if (highs.Count >= minimum)
                {
                    return;
                }

                if (highs.Any(h => h.ItemId == c.ItemId)
                    || reservedIds.Contains(c.ItemId)
                    || (!allowStoryOverlap && (storyItemIds?.Contains(c.ItemId) ?? false))
                    || PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories))
                {
                    continue;
                }

                highs.Add(c);
            }
        }

        Pick(allowStoryOverlap: false);
        if (highs.Count < minimum)
        {
            Pick(allowStoryOverlap: true);
        }
    }

    private static void TryFillFromAnswerBank(
        AssessmentKind kind,
        IReadOnlyDictionary<string, int> answers,
        IReadOnlySet<string> growthCategories,
        IReadOnlySet<int> reservedIds,
        IReadOnlySet<int>? storyItemIds,
        HashSet<string>? tileThemes,
        List<G78AnswerClause> highs,
        int minimum,
        bool contrastV2 = false)
    {
        var bank = contrastV2 ? (IPupilQuestionBank)new PupilQuestionBankG78V2() : new PupilQuestionBank();
        foreach (var q in bank.Questions.Where(q => q.Model == kind).OrderByDescending(q =>
                 {
                     if (!answers.TryGetValue(q.Id.ToString(), out var raw))
                     {
                         return 0;
                     }

                     if (contrastV2 && q.IsContrast)
                     {
                         var fact = PupilQuestionStoryParaphrase.ContrastChosenFact(q, raw);
                         return fact?.EffectiveScore ?? 0;
                     }

                     return PupilQuestionStoryParaphrase.EffectiveScore(raw, q.Reverse);
                 }).ThenBy(q => q.Id))
        {
            if (highs.Count >= minimum)
            {
                break;
            }

            if (!answers.TryGetValue(q.Id.ToString(), out var raw))
            {
                continue;
            }

            string text;
            int eff;
            string category;
            if (contrastV2 && q.IsContrast)
            {
                var fact = PupilQuestionStoryParaphrase.ContrastChosenFact(q, raw);
                if (fact is null)
                {
                    continue;
                }

                (text, eff, category) = fact.Value;
            }
            else
            {
                eff = PupilQuestionStoryParaphrase.EffectiveScore(raw, q.Reverse);
                category = q.Category;
                text = PupilG78QuestionJeForms.ForItem(q.Id) ?? "";
            }

            if (eff < 4
                || string.IsNullOrWhiteSpace(text)
                || highs.Any(h => h.ItemId == q.Id)
                || reservedIds.Contains(q.Id)
                || (storyItemIds?.Contains(q.Id) ?? false)
                || PupilG78StrengthGuard.IsForbiddenStrength(q.Id, eff, growthCategories)
                || (tileThemes is not null && ThemeBlocked(q.Id, tileThemes)))
            {
                continue;
            }

            highs.Add(new G78AnswerClause(
                q.Id,
                category,
                kind,
                G78StoryBeat.Doen,
                text,
                eff));
        }
    }

    private static void TryFillHighsFromAnyHigh(
        IReadOnlyList<G78AnswerClause> clauses,
        AssessmentKind kind,
        IReadOnlySet<string> growthCategories,
        IReadOnlySet<int> reservedIds,
        IReadOnlySet<int>? storyItemIds,
        List<G78AnswerClause> highs,
        int minimum)
    {
        foreach (var c in clauses
                     .Where(c => c.Model == kind
                                 && c.EffectiveScore >= 4
                                 && !highs.Any(h => h.ItemId == c.ItemId)
                                 && !(storyItemIds?.Contains(c.ItemId) ?? false)
                                 && !reservedIds.Contains(c.ItemId)
                                 && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories))
                     .OrderByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            highs.Add(c);
            if (highs.Count >= minimum)
            {
                break;
            }
        }
    }

    private enum TilePickMode
    {
        Strict,
        RelaxStoryThemes,
        RelaxTileThemes,
    }

    private static void TryFillHighs(
        List<G78AnswerClause> highs,
        IReadOnlyList<G78AnswerClause> clauses,
        AssessmentKind kind,
        IReadOnlySet<int> oppositeContext,
        IReadOnlySet<int> reservedIds,
        IReadOnlySet<string> growthCategories,
        HashSet<string> tileThemes,
        IReadOnlySet<string> storyThemes,
        IReadOnlySet<int>? storyItemIds,
        int minimum,
        TilePickMode mode)
    {
        if (highs.Count >= minimum)
        {
            return;
        }

        var blocked = PupilG78OppositePairs.BlockedItems(clauses, oppositeContext);
        foreach (var c in clauses
                     .Where(c => c.Model == kind
                                 && c.EffectiveScore >= 4
                                 && !highs.Any(h => h.ItemId == c.ItemId)
                                 && !(storyItemIds?.Contains(c.ItemId) ?? false)
                                 && !reservedIds.Contains(c.ItemId)
                                 && !blocked.Contains(c.ItemId)
                                 && PupilG78OppositePairs.IsAllowed(c.ItemId, clauses, oppositeContext)
                                 && !PupilG78StrengthGuard.IsForbiddenStrength(c.ItemId, c.EffectiveScore, growthCategories))
                     .OrderByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (mode == TilePickMode.Strict)
            {
                if (ThemeBlocked(c.ItemId, tileThemes) || StoryThemeConflicts(c.ItemId, storyThemes))
                {
                    continue;
                }
            }
            else if (mode == TilePickMode.RelaxStoryThemes)
            {
                if (ThemeBlocked(c.ItemId, tileThemes))
                {
                    continue;
                }
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
        var sentences = highs
            .Select(h =>
            {
                if (!string.IsNullOrWhiteSpace(h.Text))
                {
                    return PupilG78JeNarrative.AsSentence(h.Text);
                }

                var fallback = PupilG78QuestionJeForms.ForItem(h.ItemId);
                return string.IsNullOrWhiteSpace(fallback)
                    ? ""
                    : PupilG78JeNarrative.AsSentence(fallback);
            })
            .Where(s => s.Length > 0)
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
