using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class VacancyVisibilityRules
{
    /// <summary>
    /// Product window for company-hub eligibility: vacancy must still be live and
    /// its remaining term must not exceed this many days from today.
    /// </summary>
    public const int CompanyHubMaxRemainingDays = 30;

    public static bool IsPubliclyVisible(Vacancy vacancy, DateOnly today) =>
        vacancy.Status == VacancyStatus.Active
        && vacancy.StartDate <= today
        && vacancy.EndDate >= today;

    /// <summary>
    /// Company appears in the Bedrijven-hub when it has an active vacancy whose
    /// remaining run (today…EndDate) is at most <see cref="CompanyHubMaxRemainingDays"/> days.
    /// </summary>
    public static bool QualifiesForCompanyHub(Vacancy vacancy, DateOnly today)
    {
        if (!IsPubliclyVisible(vacancy, today))
        {
            return false;
        }

        var remaining = vacancy.EndDate.DayNumber - today.DayNumber;
        return remaining >= 0 && remaining <= CompanyHubMaxRemainingDays;
    }

    public static bool CanAcceptApplications(Vacancy vacancy, DateOnly today, int currentApplicationCount) =>
        IsPubliclyVisible(vacancy, today)
        && (vacancy.MaxApplications <= 0 || currentApplicationCount < vacancy.MaxApplications);
}
