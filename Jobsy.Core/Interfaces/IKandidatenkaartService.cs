using System.Security.Claims;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface IKandidatenkaartService
{
    Task<bool> IsFeatureEnabledAsync(CancellationToken cancellationToken = default);

    Task<KandidatenkaartAccessDto> GetAccessAsync(
        ClaimsPrincipal employer,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KandidatenkaartVacancyOptionDto>> ListVacanciesAsync(
        ClaimsPrincipal employer,
        IReadOnlyList<Guid> companyIds,
        CancellationToken cancellationToken = default);

    Task<KandidatenkaartMapDto> GetMapAsync(
        ClaimsPrincipal employer,
        IReadOnlyList<Guid> companyIds,
        Guid? vacancyId,
        int minMatchPercent,
        int travelKm,
        string? availabilityFilter,
        string? hoursFilter,
        CancellationToken cancellationToken = default);

    Task<KandidatenkaartInvitePreviewDto> PreviewInviteAsync(
        ClaimsPrincipal employer,
        IReadOnlyList<Guid> companyIds,
        Guid vacancyId,
        string areaKey,
        int minMatchPercent,
        int travelKm,
        CancellationToken cancellationToken = default);

    Task<KandidatenkaartInviteResultDto> StartInviteCampaignAsync(
        ClaimsPrincipal employer,
        IReadOnlyList<Guid> companyIds,
        Guid vacancyId,
        string areaKey,
        int minMatchPercent,
        int travelKm,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KandidatenkaartCampaignStatusDto>> ListCampaignsAsync(
        ClaimsPrincipal employer,
        Guid vacancyId,
        CancellationToken cancellationToken = default);

    Task<CandidateKandidatenkaartSettingsDto> GetCandidateSettingsAsync(
        Guid candidateUserId,
        CancellationToken cancellationToken = default);

    Task<CandidateKandidatenkaartSettingsDto> UpdateCandidateSettingsAsync(
        Guid candidateUserId,
        CandidateKandidatenkaartSettingsUpdate update,
        CancellationToken cancellationToken = default);

    Task AckReAvailabilityAsync(
        Guid candidateUserId,
        KandidatenkaartReAvailabilityAck ack,
        CancellationToken cancellationToken = default);
}

public class KandidatenkaartException : Exception
{
    public KandidatenkaartException(string code, string message, int statusCode = 400)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public int StatusCode { get; }
}
