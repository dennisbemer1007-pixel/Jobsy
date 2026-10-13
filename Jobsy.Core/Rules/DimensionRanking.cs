namespace Jobsy.Core.Rules;

/// <summary>
/// Shared score ranking for culture and values. Highest score wins.
/// Ties use a fixed priority so Autonomy does not win every tie just because
/// its code sorts first alphabetically. RIASEC uses <see cref="RiasecRanking"/>.
/// </summary>
public static class DimensionRanking
{
    /// <summary>Earlier codes win a tie. Autonomy is last.</summary>
    public static readonly string[] ValueTieBreak =
    [
        SchwartzValuesCatalog.Connection,
        SchwartzValuesCatalog.Stability,
        SchwartzValuesCatalog.Achievement,
        SchwartzValuesCatalog.Impact,
        SchwartzValuesCatalog.Autonomy
    ];

    /// <summary>Earlier codes win a tie. Autonomy is last.</summary>
    public static readonly string[] CultureTieBreak =
    [
        CulturePersonalityCatalog.Collaboration,
        CulturePersonalityCatalog.PeopleFirst,
        CulturePersonalityCatalog.Informal,
        CulturePersonalityCatalog.Innovation,
        CulturePersonalityCatalog.Flexibility,
        CulturePersonalityCatalog.Autonomy
    ];

    public static IReadOnlyList<(string Code, int Score)> Rank(
        IEnumerable<(string Code, int? Score)> items,
        IReadOnlyList<string> tieBreak)
    {
        var order = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < tieBreak.Count; i++)
        {
            order[tieBreak[i]] = i;
        }

        return items
            .Where(x => x.Score is not null)
            .Select(x => (x.Code, Score: x.Score!.Value))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => order.TryGetValue(x.Code, out var index) ? index : int.MaxValue)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .ToList();
    }
}
