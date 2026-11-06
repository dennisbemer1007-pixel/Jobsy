namespace Jobsy.Core.Rules;

/// <summary>
/// Candidate-side only: maps private dislike codes to vacancy ordering.
/// Down-rank means "sort later", never hide, and never changes MatchPercent / employer scores.
/// </summary>
public static class DislikeMatchRules
{
    public const string NightShifts = "night-shifts";

    /// <summary>
    /// Codes stored today but not yet mapped to vacancy attributes.
    /// Do not invent vacancy fields for these.
    /// </summary>
    public static readonly IReadOnlyList<string> StoredOnlyCodes =
    [
        "heavy-lifting",
        "working-alone",
        "phone-customers",
        "noise",
        "cold-outdoor",
        "computer-work",
        "changing-hours",
        "crowded",
        "long-travel"
    ];

    public static bool HasNightShiftDislike(IEnumerable<string>? dislikeCodes)
        => dislikeCodes is not null
           && dislikeCodes.Any(c => string.Equals(c, NightShifts, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Stable partition: items that match the night-shift dislike move after the others,
    /// preserving relative order within each group. Match scores are untouched.
    /// </summary>
    public static IReadOnlyList<T> DownRankNightShifts<T>(
        IReadOnlyList<T> items,
        IEnumerable<string>? dislikeCodes,
        Func<T, bool> isNightShift)
    {
        if (items.Count == 0 || !HasNightShiftDislike(dislikeCodes))
        {
            return items;
        }

        List<T>? preferred = null;
        List<T>? demoted = null;
        foreach (var item in items)
        {
            if (isNightShift(item))
            {
                demoted ??= new List<T>();
                demoted.Add(item);
            }
            else
            {
                preferred ??= new List<T>();
                preferred.Add(item);
            }
        }

        if (demoted is null || demoted.Count == 0)
        {
            return items;
        }

        if (preferred is null || preferred.Count == 0)
        {
            return items;
        }

        var combined = new List<T>(items.Count);
        combined.AddRange(preferred);
        combined.AddRange(demoted);
        return combined;
    }
}
