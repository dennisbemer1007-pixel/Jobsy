using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface IJobSearchActivityService
{
    Task<JobSearchActivityOverviewDto> GetOverviewAsync(
        Guid candidateUserId,
        bool afterMatchEnabled,
        string cultureName,
        CancellationToken cancellationToken = default);

    Task SyncAutomaticActivitiesAsync(
        Guid candidateUserId,
        bool afterMatchEnabled,
        CancellationToken cancellationToken = default);

    Task<JobSearchActivityItemDto> AddManualAsync(
        Guid candidateUserId,
        CreateManualJobSearchActivityRequest request,
        string cultureName,
        CancellationToken cancellationToken = default);

    Task UpdateAgreementAsync(
        Guid candidateUserId,
        int activitiesPerPeriod,
        CancellationToken cancellationToken = default);

    Task<byte[]> BuildPeriodPdfAsync(
        Guid candidateUserId,
        bool afterMatchEnabled,
        string cultureName,
        CancellationToken cancellationToken = default);
}
