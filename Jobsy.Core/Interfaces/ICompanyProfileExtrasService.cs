using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICompanyProfileExtrasService
{
    Task<CompanyProfileExtrasDto?> GetAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<CompanyProfileExtrasDto> SaveAsync(
        Guid companyId,
        CompanyProfileExtrasUpdate update,
        CancellationToken cancellationToken = default);

    /// <summary>Root organisation id for a company (self when no parent).</summary>
    Task<Guid> ResolveRootCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public sealed record CompanyProfileExtrasDto(
    Guid CompanyId,
    Guid RootCompanyId,
    IReadOnlyList<string> WorkTypeLabels,
    IReadOnlyList<string> SuggestedWorkTypeLabels,
    IReadOnlyDictionary<string, int>? CultureSliders,
    string? CultureSource,
    CulturePersonalityScores? CultureScores,
    IReadOnlyList<string> ValueCardIds,
    SchwartzValuesScores? ValuesScores,
    bool WorkTypesFromKvk);

public sealed record CompanyProfileExtrasUpdate(
    IReadOnlyList<string>? WorkTypeLabels = null,
    IReadOnlyDictionary<string, int>? CultureSliders = null,
    IReadOnlyList<string>? ValueCardIds = null);
