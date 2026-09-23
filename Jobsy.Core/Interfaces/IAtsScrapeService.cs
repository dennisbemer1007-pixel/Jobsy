using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface IAtsScrapeService
{
    Task<AtsScrapeRunReport> ScrapeSourceAsync(Guid sourceId, CancellationToken cancellationToken = default);
    Task<AtsScrapeRunReport> ScrapeAllEnabledAsync(CancellationToken cancellationToken = default);
}

public interface IAtsVacancyModerationService
{
    Task<IReadOnlyList<AtsScrapedListing>> ListAsync(
        AtsListingStatus? status = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<AtsScrapedListing?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AtsScrapedListing?> UpdateFieldsAsync(
        Guid id,
        string title,
        string companyName,
        string? locationLabel,
        string description,
        string? salaryText,
        decimal? hourlyWage,
        string? hoursText,
        CancellationToken cancellationToken = default);

    Task<Vacancy?> ApproveAsync(Guid id, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid id, string? reason, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAtsVacancyHealthService
{
    Task<int> RunHealthPassAsync(CancellationToken cancellationToken = default);
}
