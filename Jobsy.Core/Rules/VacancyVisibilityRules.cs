using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class VacancyVisibilityRules
{
    public static bool IsPubliclyVisible(Vacancy vacancy, DateOnly today) =>
        vacancy.Status == VacancyStatus.Active
        && vacancy.StartDate <= today
        && vacancy.EndDate >= today;

    public static bool CanAcceptApplications(Vacancy vacancy, DateOnly today, int currentApplicationCount) =>
        IsPubliclyVisible(vacancy, today)
        && (vacancy.MaxApplications <= 0 || currentApplicationCount < vacancy.MaxApplications);
}
