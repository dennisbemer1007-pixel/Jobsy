using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;

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
        [9019] = "Anderen uitleggen is minder vaak jouw ding.",
        [9020] = "De leiding nemen is minder jouw ding.",
        [9055] = "Strakke regels zijn minder jouw ding.",
        [9057] = "Alles hetzelfde houden is minder jouw ding.",
    };

    public static (IReadOnlyList<PupilStoryTileDto> Tiles, IReadOnlyList<int> UsedItemIds) Build(
        IReadOnlyList<G78AnswerClause> clauses,
        string language)
    {
        var usedIds = new HashSet<int>();
        var usedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tiles = new List<PupilStoryTileDto>();

        foreach (var (modelKey, labelKey, kind) in Slots)
        {
            var highs = PickHighs(clauses, kind, usedCategories, usedIds);
            var low = PickLow(clauses, kind, usedIds, highs);
            var explanation = ComposeTileText(highs, low);
            tiles.Add(new PupilStoryTileDto(
                modelKey,
                PupilVerhaalLocalized.Get(labelKey, language),
                explanation));
        }

        return (tiles, usedIds.ToList());
    }

    private static string ContrastForTile(G78AnswerClause low)
    {
        if (TileLowContrast.TryGetValue(low.ItemId, out var fixedLine))
        {
            return fixedLine;
        }

        var canonical = PupilG78QuestionJeForms.ForItem(low.ItemId);
        var inner = canonical is not null
            ? canonical.Trim().TrimEnd('.')
            : ExtractLowInner(low.Text);
        if (inner.StartsWith("Je ", StringComparison.Ordinal))
        {
            inner = "je " + inner[3..];
        }

        return $"Je zei minder vaak dat {inner}.";
    }

    private static string ComposeTileText(IReadOnlyList<G78AnswerClause> highs, G78AnswerClause? low)
    {
        var parts = new List<string>();
        if (highs.Count >= 2)
        {
            parts.Add(PupilG78JeNarrative.MergeTwo(highs[0].Text, highs[1].Text));
        }
        else if (highs.Count == 1)
        {
            parts.Add(PupilG78JeNarrative.AsSentence(highs[0].Text));
        }

        if (low is not null && highs.Count > 0
            && !PupilG78JeNarrative.TooSimilar(highs[0].Text, low.Text))
        {
            parts.Add(ContrastForTile(low));
        }

        if (parts.Count == 0)
        {
            return "Je gaf zelf antwoord op deze vragen.";
        }

        return string.Join(' ', parts);
    }

    private static string ExtractLowInner(string lowText)
    {
        var s = lowText.Trim().TrimEnd('.');
        if (s.StartsWith("Dit zei je minder vaak over jezelf: ", StringComparison.Ordinal))
        {
            return s["Dit zei je minder vaak over jezelf: ".Length..].Trim().TrimEnd('.');
        }

        if (s.StartsWith("Juist hier zie je het anders: ", StringComparison.Ordinal))
        {
            return s["Juist hier zie je het anders: ".Length..].Trim().TrimEnd('.');
        }

        return s.StartsWith("Je ", StringComparison.Ordinal) ? "je " + s[3..] : s;
    }

    private static IReadOnlyList<G78AnswerClause> PickHighs(
        IReadOnlyList<G78AnswerClause> all,
        AssessmentKind kind,
        HashSet<string> usedCategories,
        HashSet<int> usedIds)
    {
        var picked = new List<G78AnswerClause>();
        foreach (var c in all
                     .Where(c => c.Model == kind
                                 && c.EffectiveScore >= 4
                                 && !c.Text.StartsWith("Dit zei je minder", StringComparison.Ordinal)
                                 && !usedIds.Contains(c.ItemId)
                                 && !usedCategories.Contains(c.Category))
                     .OrderByDescending(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (picked.Count > 0 && PupilG78JeNarrative.TooSimilar(picked[0].Text, c.Text))
            {
                continue;
            }

            picked.Add(c);
            usedCategories.Add(c.Category);
            usedIds.Add(c.ItemId);
            if (picked.Count >= 2)
            {
                break;
            }
        }

        return picked;
    }

    private static G78AnswerClause? PickLow(
        IReadOnlyList<G78AnswerClause> all,
        AssessmentKind kind,
        HashSet<int> usedIds,
        IReadOnlyList<G78AnswerClause> highs)
    {
        foreach (var c in all
                     .Where(c => c.Model == kind
                                 && c.EffectiveScore <= 2
                                 && !usedIds.Contains(c.ItemId))
                     .OrderBy(c => c.EffectiveScore)
                     .ThenBy(c => c.ItemId))
        {
            if (highs.Count > 0 && PupilG78JeNarrative.TooSimilar(highs[0].Text, c.Text))
            {
                continue;
            }

            usedIds.Add(c.ItemId);
            return c;
        }

        return null;
    }
}
