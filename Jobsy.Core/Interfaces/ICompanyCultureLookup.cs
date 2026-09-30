using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Batch lookup of completed employer culture profiles for match scoring.
/// Per company: own completed profile, else parent organisation, else nothing.
/// </summary>
public interface ICompanyCultureLookup
{
    Task<IReadOnlyDictionary<Guid, CulturePersonalityScores>> GetForCompaniesAsync(
        IReadOnlyCollection<Guid> companyIds,
        CancellationToken cancellationToken = default);
}
