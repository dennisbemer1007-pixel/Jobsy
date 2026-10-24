using Jobsy.Core.Contracts;

namespace Jobsy.Core.Media;

/// <summary>
/// Loads vacancy photo bytes using the same chain as detail / card (inline → photo → logo → work-type WebP).
/// Falls back to HTTP against <c>PublicWebBaseUrl</c> when static files are not on the API disk (Render acc/prod).
/// </summary>
public interface IVacancyPublicPhotoBytesLoader
{
    Task<byte[]?> LoadForMapPopupAsync(
        VacancyDiscoveryRecord record,
        Guid vacancyId,
        string? workType,
        CancellationToken cancellationToken = default);
}
