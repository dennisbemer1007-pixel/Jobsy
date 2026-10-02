using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class VacancyVisibilityRules
{
    /// <summary>
    /// Full public visibility including publisher verification
    /// (<see cref="PublicVisibility"/>).
    /// </summary>
    public static bool IsPubliclyVisible(Vacancy vacancy, DateOnly today) =>
        PublicVisibility.IsVacancyPublic(vacancy, today);

    /// <summary>
    /// Full public visibility including publisher verification
    /// (<see cref="PublicVisibility"/>).
    /// </summary>
    public static bool IsPubliclyVisible(VacancyDiscoveryRecord record, DateOnly today) =>
        PublicVisibility.IsVacancyPublic(record, today);

    /// <summary>
    /// Date/status window only — prefer the Vacancy/Record overloads so publisher
    /// verification is enforced. Kept for callers that already filtered the publisher.
    /// </summary>
    public static bool IsPubliclyVisible(
        VacancyStatus status,
        DateOnly startDate,
        DateOnly endDate,
        DateOnly today) =>
        IsDateAndStatusPublic(status, startDate, endDate, today);

    public static bool IsDateAndStatusPublic(
        VacancyStatus status,
        DateOnly startDate,
        DateOnly endDate,
        DateOnly today) =>
        status == VacancyStatus.Active
        && startDate <= today
        && endDate >= today;

    /// <summary>
    /// Closed = was once live and is not publicly visible now because it archived, was fulfilled,
    /// or ran past its end date. Never-published, draft, pending-approval and future-start vacancies
    /// are not "closed" — those stay a plain 404 (unknown).
    /// </summary>
    public static bool IsClosed(Vacancy vacancy, DateOnly today) =>
        vacancy.PublishedAtUtc is not null
        && !IsPubliclyVisible(vacancy, today)
        && (vacancy.Status == VacancyStatus.Archived
            || vacancy.Status == VacancyStatus.Fulfilled
            || (vacancy.Status == VacancyStatus.Active && vacancy.EndDate < today));

    public static bool CanAcceptApplications(Vacancy vacancy, DateOnly today, int currentApplicationCount) =>
        IsPubliclyVisible(vacancy, today)
        && (vacancy.MaxApplications <= 0 || currentApplicationCount < vacancy.MaxApplications);
}
