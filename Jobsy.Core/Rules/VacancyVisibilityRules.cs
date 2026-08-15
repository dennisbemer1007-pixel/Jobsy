using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class VacancyVisibilityRules
{
    public static bool IsPubliclyVisible(Vacancy vacancy, DateOnly today) =>
        IsPubliclyVisible(vacancy.Status, vacancy.StartDate, vacancy.EndDate, today);

    public static bool IsPubliclyVisible(VacancyDiscoveryRecord record, DateOnly today) =>
        IsPubliclyVisible(record.Status, record.StartDate, record.EndDate, today);

    public static bool IsPubliclyVisible(
        VacancyStatus status,
        DateOnly startDate,
        DateOnly endDate,
        DateOnly today) =>
        status == VacancyStatus.Active
        && startDate <= today
        && endDate >= today;

    public static bool CanAcceptApplications(Vacancy vacancy, DateOnly today, int currentApplicationCount) =>
        IsPubliclyVisible(vacancy, today)
        && (vacancy.MaxApplications <= 0 || currentApplicationCount < vacancy.MaxApplications);
}
