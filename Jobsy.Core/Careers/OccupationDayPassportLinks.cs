namespace Jobsy.Core.Careers;

/// <summary>Which occupations on a passport may link to a stored day-in-life.</summary>
public static class OccupationDayPassportLinks
{
    public static async Task<IReadOnlySet<string>> FilterStoredAsync(
        IOccupationDayInLifeReader reader,
        IEnumerable<string?> escoIds,
        CancellationToken cancellationToken)
    {
        var candidates = escoIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return await reader.StoredEscoIdsAsync(candidates, cancellationToken);
    }
}
