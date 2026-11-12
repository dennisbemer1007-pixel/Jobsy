using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Four fact-based result tiles (competence, career, values, culture).</summary>
public static class PupilG78FactTileComposer
{
    private static readonly (string ModelKey, string LabelKey, AssessmentKind Kind)[] Slots =
    [
        ("competence", "LeerlingStory.Tile.ZoBenJij", AssessmentKind.Competence),
        ("riasec", "LeerlingStory.Tile.DitDoeJe", AssessmentKind.Career),
        ("values", "LeerlingStory.Tile.Belangrijk", AssessmentKind.Values),
        ("culture", "LeerlingStory.Tile.Thuis", AssessmentKind.Culture),
    ];

    private static readonly Dictionary<int, string> TileLowContrast = new()
    {
        [9012] = "Taken uitstellen tot het laatst is minder jouw ding.",
        [9014] = "Alles op de oude manier is minder jouw ding.",
        [9015] = "Stil afwachten is minder jouw ding.",
        [9016] = "Iets maken of repareren is minder jouw ding.",
        [9018] = "Iets moois maken, zoals een tekening of muziek, is minder vaak jouw ding.",
        [9019] = "Anderen uitleggen is minder vaak jouw ding.",
        [9003] = "Rustig blijven bij een slecht cijfer is minder jouw ding.",
        [9020] = "De leiding nemen is minder jouw ding.",
        [9024] = "Anderen steunen is minder vaak jouw ding.",
        [9055] = "Strakke regels zijn minder jouw ding.",
        [9057] = "Alles hetzelfde houden is minder jouw ding.",
        [9001] = "Vrienden in de gaten houden is minder vaak jouw ding.",
        [9002] = "Huiswerk afmaken zonder zin is minder jouw ding.",
        [9009] = "Iets nieuws proberen is minder vaak jouw ding.",
        [9032] = "Veel vrienden om je heen is minder vaak jouw ding.",
        [9033] = "Heel goed worden in iets is minder vaak jouw ding.",
        [9031] = "Later alles zelf kiezen is minder vaak jouw ding.",
        [9035] = "Later iets doen voor de wereld is minder vaak jouw ding.",
        [9050] = "Iedereen zich welkom laten voelen is minder vaak jouw ding.",
        [9053] = "Taken delen in een team is minder vaak jouw ding.",
        [9047] = "Samen in een groepje werken is minder vaak jouw ding.",
        [9058] = "Werken in een groepje is minder vaak jouw ding.",
        [9052] = "Een drukke, gezellige sfeer is minder vaak jouw ding.",
    };

    public static (IReadOnlyList<PupilStoryTileDto> Tiles, IReadOnlyList<int> UsedItemIds, IReadOnlyList<int> LowItemIds) Build(
        IReadOnlyList<G78AnswerClause> clauses,
        string language)
    {
        var usedIds = new HashSet<int>();
        var usedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lowIds = new List<int>();
        var tiles = new List<PupilStoryTileDto>();
        var builtTexts = new List<string>();

        foreach (var (modelKey, labelKey, kind) in Slots)
        {
            var highs = PickHighs(clauses, kind, usedCategories, usedIds);
            var low = PickLow(clauses, kind, usedIds, highs, builtTexts);
            if (low is not null)
            {
                lowIds.Add(low.ItemId);
            }

            var explanation = ComposeTileText(highs, low);
            builtTexts.Add(explanation);
            tiles.Add(new PupilStoryTileDto(
                modelKey,
                PupilVerhaalLocalized.Get(labelKey, language),
                explanation));
        }

        return (tiles, usedIds.ToList(), lowIds);
    }

    private static string ContrastForTile(G78AnswerClause low)
    {
        if (low.ItemId is >= 9001 and <= PupilG78CompetencyNarrative.CompetenceItemMax)
        {
            return PupilG78CompetencyNarrative.MinderNaarVoren(low.Category);
        }

        if (TileLowContrast.TryGetValue(low.ItemId, out var fixedLine))
        {
            return fixedLine;
        }

        return "";
    }

    private static string ComposeTileText(IReadOnlyList<G78AnswerClause> highs, G78AnswerClause? low)
    {
        var sentences = new List<string>();
        foreach (var h in highs)
        {
            var main = PupilG78QuestionJeForms.ForItem(h.ItemId)
                       ?? PupilG78JeNarrative.AsSentence(h.Text);
            sentences.Add(main);
        }

        if (low is not null && highs.Count > 0)
        {
            var contrast = ContrastForTile(low);
            var tileSoFar = string.Join(' ', sentences);
            if (!string.IsNullOrWhiteSpace(contrast)
                && !PupilG78JeNarrative.TooSimilar(highs[0].Text, low.Text)
                && !PupilG78AnswerConflicts.TileLowConflictsWithText(low.ItemId, tileSoFar)
                && !PupilG78AnswerConflicts.ContrastLineConflictsHighs(highs, contrast)
                && !highs.Any(h => h.Model == AssessmentKind.Competence
                                   && string.Equals(h.Category, low.Category, StringComparison.OrdinalIgnoreCase)))
            {
                sentences.Add(contrast);
            }
        }

        if (sentences.Count == 0)
        {
            return "Je gaf zelf antwoord op deze vragen.";
        }

        return string.Join(' ', sentences);
    }

    private static IReadOnlyList<G78AnswerClause> PickHighs(
        IReadOnlyList<G78AnswerClause> all,
        AssessmentKind kind,
        HashSet<string> usedCategories,
        HashSet<int> usedIds)
    {
        if (kind == AssessmentKind.Competence)
        {
            return PickCompetenceHighs(all, usedIds);
        }

        var picked = new List<G78AnswerClause>();
        foreach (var c in all
                     .Where(c => c.Model == kind
                                 && c.EffectiveScore >= 4
                                 && !c.Text.StartsWith("Dit zei je minder", StringComparison.Ordinal)
                                 && !usedIds.Contains(c.ItemId))
                     .OrderByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (usedCategories.Contains(c.Category) && picked.Count >= 1)
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
            if (picked.Count >= 3)
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
                                     && !picked.Contains(c))
                         .OrderBy(c => c.ItemId))
            {
                if (picked.Count > 0 && PupilG78JeNarrative.TooSimilar(picked[0].Text, c.Text))
                {
                    continue;
                }

                picked.Add(c);
                usedIds.Add(c.ItemId);
                if (picked.Count >= 2)
                {
                    break;
                }
            }
        }

        return picked;
    }

    private static IReadOnlyList<G78AnswerClause> PickCompetenceHighs(
        IReadOnlyList<G78AnswerClause> all,
        HashSet<int> usedIds)
    {
        var picked = new List<G78AnswerClause>();
        foreach (var cat in CompetencyTestCatalog.QuickScanCategories)
        {
            var c = all
                .Where(x => x.Model == AssessmentKind.Competence
                            && x.ItemId <= PupilG78CompetencyNarrative.CompetenceItemMax
                            && string.Equals(x.Category, cat, StringComparison.OrdinalIgnoreCase)
                            && x.EffectiveScore >= 4
                            && !x.Text.StartsWith("Dit zei je minder", StringComparison.Ordinal)
                            && !usedIds.Contains(x.ItemId))
                .OrderByDescending(x => x.EffectiveScore)
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
            if (picked.Count >= 3)
            {
                break;
            }
        }

        if (picked.Count < 2)
        {
            foreach (var c in all
                         .Where(x => x.Model == AssessmentKind.Competence
                                     && x.EffectiveScore >= 4
                                     && !usedIds.Contains(x.ItemId))
                         .OrderByDescending(x => x.EffectiveScore)
                         .ThenBy(x => x.ItemId))
            {
                if (picked.Any(p => PupilG78JeNarrative.TooSimilar(p.Text, c.Text)))
                {
                    continue;
                }

                picked.Add(c);
                usedIds.Add(c.ItemId);
                if (picked.Count >= 3)
                {
                    break;
                }
            }
        }

        return picked;
    }

    private static G78AnswerClause? PickLow(
        IReadOnlyList<G78AnswerClause> all,
        AssessmentKind kind,
        HashSet<int> usedIds,
        IReadOnlyList<G78AnswerClause> highs,
        IReadOnlyList<string> priorTileTexts)
    {
        foreach (var c in all
                     .Where(c => c.Model == kind
                                 && c.EffectiveScore <= 2
                                 && !usedIds.Contains(c.ItemId))
                     .OrderBy(c => TileLowContrast.ContainsKey(c.ItemId) ? 0 : 1)
                     .ThenBy(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (kind == AssessmentKind.Competence
                && highs.Any(h => string.Equals(h.Category, c.Category, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (highs.Count > 0
                && PupilG78JeNarrative.TooSimilar(highs[0].Text, c.Text)
                && !TileLowContrast.ContainsKey(c.ItemId))
            {
                continue;
            }

            var contrast = ContrastForTile(c);
            if (PupilG78AnswerConflicts.ContrastLineConflictsHighs(highs, contrast))
            {
                continue;
            }

            var conflictsPrior = priorTileTexts.Any(prior =>
                TileLowContrast.ContainsKey(c.ItemId) is false
                &&
                PupilG78AnswerConflicts.TileLowConflictsWithText(c.ItemId, prior));
            if (conflictsPrior)
            {
                continue;
            }

            usedIds.Add(c.ItemId);
            return c;
        }

        return null;
    }
}
