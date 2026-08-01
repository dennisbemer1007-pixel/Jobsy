namespace Jobsy.Core.Rules;

/// <summary>Pure rules for paid vacancy highlights on the banenkaart.</summary>
public static class VacancyHighlightRules
{
    /// <summary>
    /// A vacancy is featured when flagged and not past <paramref name="highlightedUntil"/>.
    /// Legacy rows without an until-date remain featured while <paramref name="isHighlighted"/> is true.
    /// </summary>
    public static bool IsActive(bool isHighlighted, DateTime? highlightedUntil, DateTime utcNow)
    {
        if (!isHighlighted)
        {
            return false;
        }

        if (highlightedUntil is null)
        {
            return true;
        }

        var until = highlightedUntil.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(highlightedUntil.Value, DateTimeKind.Utc)
            : highlightedUntil.Value.ToUniversalTime();

        return until > utcNow.ToUniversalTime();
    }

    public static DateTime ComputeUntil(DateTime utcNow, int days = VacancyProductRules.HighlightDays) =>
        utcNow.ToUniversalTime().AddDays(days);
}
