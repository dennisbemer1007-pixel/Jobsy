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
        string? attachmentFileName,
        string? attachmentContentType,
        ReadOnlyMemory<byte>? attachmentContent,
        CancellationToken cancellationToken = default);

    Task<(string FileName, string ContentType, byte[] Content)?> GetAttachmentAsync(
        Guid candidateUserId,
        Guid activityId,
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
