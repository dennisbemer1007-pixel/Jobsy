using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface IProfileVacancyMatchService
{
    Task<ProfileVacancyMatchContext?> TryLoadForPrincipalAsync(
        System.Security.Claims.ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<ProfileVacancyMatchContext?> TryLoadForUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scores vacancies for a candidate. Loads employer culture once per batch
    /// (own profile, else parent organisation) and applies it per vacancy company.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, ProfileVacancyMatch>> ScoreAsync(
        ProfileVacancyMatchContext context,
        IEnumerable<(VacancyDiscoveryRecord Record, int? TravelMinutes)> vacancies,
        CancellationToken cancellationToken = default);
}
