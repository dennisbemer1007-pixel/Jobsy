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

    IReadOnlyDictionary<Guid, ProfileVacancyMatch> Score(
        ProfileVacancyMatchContext context,
        IEnumerable<(VacancyDiscoveryRecord Record, int? TravelMinutes)> vacancies);

    /// <summary>
    /// Authoritative MatchPercent: score with travel from the candidate's saved home location.
    /// </summary>
    IReadOnlyDictionary<Guid, ProfileVacancyMatch> ScoreFromHome(
        ProfileVacancyMatchContext context,
        IEnumerable<VacancyDiscoveryRecord> vacancies);
}
