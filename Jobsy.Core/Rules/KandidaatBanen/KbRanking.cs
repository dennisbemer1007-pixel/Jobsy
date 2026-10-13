namespace Jobsy.Core.Rules.KandidaatBanen;

/// <summary>Candidate-own list ranking helpers (never hide; down-rank only).</summary>
public static class KbRanking
{
    /// <summary>Sort-key penalty when a vacancy matches a private dislike (D8).</summary>
    public const int DislikePenalty = 15;

    /// <summary>
    /// Sort key for "Past het best": calibrated fit percent minus dislike penalty.
    /// Higher is better. Vacancies without a shown percent sort last.
    /// </summary>
    public static int SortKey(int? fitPercent, bool ranksLower)
    {
        if (fitPercent is not int pct)
        {
            return int.MinValue;
        }

        return ranksLower ? pct - DislikePenalty : pct;
    }

    /// <summary>
    /// Stable candidate list order: sort key desc, then title, then id.
    /// </summary>
    public static IOrderedEnumerable<T> OrderByFitThenTitle<T>(
        IEnumerable<T> items,
        Func<T, int?> fitPercent,
        Func<T, bool> ranksLower,
        Func<T, string> title,
        Func<T, Guid>? id = null)
    {
        var ordered = items
            .OrderByDescending(i => SortKey(fitPercent(i), ranksLower(i)))
            .ThenBy(i => title(i), StringComparer.OrdinalIgnoreCase);
        return id is null
            ? ordered
            : ordered.ThenBy(i => id(i));
    }
}
