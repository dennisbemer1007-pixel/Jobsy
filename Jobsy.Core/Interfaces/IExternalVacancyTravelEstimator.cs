namespace Jobsy.Core.Interfaces;

public interface IExternalVacancyTravelEstimator
{
    /// <summary>
    /// Estimates one-way travel minutes when the candidate home location and vacancy place are known.
    /// </summary>
    Task<int?> TryEstimateTravelMinutesAsync(
        Guid candidateUserId,
        string vacancyPlaceText,
        CancellationToken cancellationToken = default);
}
