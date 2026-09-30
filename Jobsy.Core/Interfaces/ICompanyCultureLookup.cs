using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Batch lookup of completed employer culture + values profiles for match scoring.
/// Per company: own completed profile, else parent organisation, else nothing.
/// </summary>
public interface ICompanyCultureLookup
{
    Task<IReadOnlyDictionary<Guid, CompanyCultureLookupResult>> GetForCompaniesAsync(
        IReadOnlyCollection<Guid> companyIds,
        CancellationToken cancellationToken = default);
}

/// <summary>Resolved employer culture, kernwaarden and engagement for one company (vestiging → org fallback).</summary>
public sealed record CompanyCultureLookupResult(
    CulturePersonalityScores? Culture,
    SchwartzValuesScores? Values,
    IReadOnlyList<CompanyEngagementMatchItem>? Engagement = null);
