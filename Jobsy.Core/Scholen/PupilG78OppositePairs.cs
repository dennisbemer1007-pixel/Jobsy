namespace Jobsy.Core.Scholen;

/// <summary>Keeps the higher-scored pole of trait opposites on the result page.</summary>
public static class PupilG78OppositePairs
{
    private static readonly (int[] Stability, int[] Change)[] NamedPairs =
    [
        (
            [9027, 9055, 9034, 9044],
            [9057, 9050, 9056, 9051, 9009]
        ),
        (
            [9046, 9058, 9015],
            [9047, 9053, 9059, 9030]
        ),
        (
            [9003, 9039, 9008],
            [9049, 9010]
        ),
        (
            [9014, 9027, 9055],
            [9050, 9051, 9004, 9009]
        ),
    ];

    public static HashSet<int> BlockedItems(
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlySet<int> alreadySelected)
    {
        var byId = clauses.ToDictionary(c => c.ItemId, c => c.EffectiveScore);
        var blocked = new HashSet<int>();

        foreach (var (poleA, poleB) in NamedPairs)
        {
            var scoreA = PoleScore(poleA, byId);
            var scoreB = PoleScore(poleB, byId);
            var losers = scoreA >= scoreB ? poleB : poleA;
            foreach (var id in losers)
            {
                if (!alreadySelected.Contains(id))
                {
                    blocked.Add(id);
                }
            }
        }

        return blocked;
    }

    public static bool IsAllowed(
        int itemId,
        IReadOnlyList<G78AnswerClause> clauses,
        IReadOnlySet<int> alreadySelected)
        => !BlockedItems(clauses, alreadySelected).Contains(itemId);

    public static IReadOnlyList<string> FindPageTensions(IEnumerable<int> pageItemIds)
    {
        var ids = pageItemIds.ToHashSet();
        var tensions = new List<string>();
        foreach (var (poleA, poleB) in NamedPairs)
        {
            var onA = poleA.Where(ids.Contains).ToList();
            var onB = poleB.Where(ids.Contains).ToList();
            if (onA.Count > 0 && onB.Count > 0)
            {
                tensions.Add($"opposite:{onA[0]}+{onB[0]}");
            }
        }

        return tensions;
    }

    private static int PoleScore(int[] items, Dictionary<int, int> byId)
    {
        var scores = items.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        return scores.Count == 0 ? 0 : (int)Math.Round(scores.Average(), MidpointRounding.AwayFromZero);
    }
}
