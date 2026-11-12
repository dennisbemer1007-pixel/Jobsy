using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>Story-first reservation of competency examples and shared pick filters.</summary>
public static class PupilG78ContentAllocation
{
    public static IReadOnlyList<int> ReserveCompetencyExamples(
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlyList<string> topDimensions)
    {
        var reserved = new List<int>();
        var usedThemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var selected = new HashSet<int>();

        foreach (var dim in topDimensions)
        {
            var blocked = PupilG78OppositePairs.BlockedItems(clauses, selected);
            var pick = clauses
                .Where(c => c.Model == AssessmentKind.Competence
                            && c.ItemId <= PupilG78CompetencyNarrative.CompetenceItemMax
                            && string.Equals(c.Category, dim, StringComparison.OrdinalIgnoreCase)
                            && c.EffectiveScore >= 4
                            && !blocked.Contains(c.ItemId))
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
}
