namespace Jobsy.Core.Rules;

/// <summary>
/// Shared RIASEC ranking for Beroepentest outcomes.
/// Primary sort: score descending. Tie-break: fixed Holland letter order R → I → A → S → E → C
/// (same order as <see cref="CareerTestCatalog.RiasecCodes"/>). When several codes share the
/// top score they are co-leaders and should be shown as equal (e.g. "A en S even sterk").
/// </summary>
public static class RiasecRanking
{
    public static IReadOnlyList<(string Code, int Value)> Rank(
        int? realistic,
        int? investigative,
        int? artistic,
        int? social,
        int? enterprising,
        int? conventional)
    {
        var order = CareerTestCatalog.RiasecCodes;
        var scores = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase)
        {
            [CareerTestCatalog.Realistic] = realistic,
            [CareerTestCatalog.Investigative] = investigative,
            [CareerTestCatalog.Artistic] = artistic,
            [CareerTestCatalog.Social] = social,
            [CareerTestCatalog.Enterprising] = enterprising,
            [CareerTestCatalog.Conventional] = conventional
        };

        return order
            .Select((code, index) => (Code: code, Value: scores[code], Index: index))
            .Where(x => x.Value is not null)
            .OrderByDescending(x => x.Value!.Value)
            .ThenBy(x => x.Index)
            .Select(x => (x.Code, x.Value!.Value))
            .ToList();
    }

    /// <summary>All codes tied at the highest score (empty when no scores).</summary>
    public static IReadOnlyList<string> TopCodes(
        int? realistic,
        int? investigative,
        int? artistic,
        int? social,
        int? enterprising,
        int? conventional)
    {
        var ranked = Rank(realistic, investigative, artistic, social, enterprising, conventional);
        if (ranked.Count == 0)
        {
            return [];
        }

        var top = ranked[0].Value;
        return ranked.Where(x => x.Value == top).Select(x => x.Code).ToList();
    }

    /// <summary>
    /// Headline fragment: single winner → type label; ties → Holland letters “A en S even sterk”.
    /// </summary>
    public static string FormatTopEqualOrLabel(IReadOnlyList<string> topCodes)
    {
        if (topCodes.Count == 0)
        {
            return CareerCompassBuilder.TypeLabel(CareerTestCatalog.Social);
        }

        if (topCodes.Count == 1)
        {
            return CareerCompassBuilder.TypeLabel(topCodes[0]);
        }

        var letters = topCodes
            .Select(c => CareerTestCatalog.HollandLetter.TryGetValue(c, out var letter) ? letter.ToString() : c)
            .ToList();
        if (letters.Count == 2)
        {
            return $"{letters[0]} en {letters[1]} even sterk";
        }

        return string.Join(", ", letters.Take(letters.Count - 1)) + " en " + letters[^1] + " even sterk";
    }

    public static string FormatCareerOutcomeLine(
        int? realistic,
        int? investigative,
        int? artistic,
        int? social,
        int? enterprising,
        int? conventional)
    {
        var top = TopCodes(realistic, investigative, artistic, social, enterprising, conventional);
        if (top.Count == 0)
        {
            return string.Empty;
        }

        var body = FormatTopEqualOrLabel(top);
        return top.Count == 1
            ? $"Richting: {body.ToLowerInvariant()}"
            : $"Richting: {body}";
    }
}
