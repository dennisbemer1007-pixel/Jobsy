using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Who may remove a vacancy. Active vacancies are archived first.
/// Draft and archived vacancies without applications can be deleted by the employer and by admin.
/// Admin may also remove a vacancy that still has applications (QA cleanup): archive, then delete the applications.
/// </summary>
public static class VacancyDeletionRules
{
    public static bool EmployerMayDelete(VacancyStatus status, int applicationCount)
        => applicationCount == 0
           && status is VacancyStatus.Draft or VacancyStatus.Archived;

    public static bool AdminMayDeleteWithoutApplications(VacancyStatus status, int applicationCount)
        => EmployerMayDelete(status, applicationCount);

    /// <summary>Admin cleanup of a vacancy that already has applications.</summary>
    public static bool AdminMayPurgeWithApplications(int applicationCount)
        => applicationCount > 0;

    /// <summary>
    /// Admin cleanup with purgeApplications. A live vacancy (including Active with
    /// zero applications) is archived first, then removed. Employers still cannot
    /// delete an active vacancy.
    /// </summary>
    public static bool AdminMayPurge(VacancyStatus status, int applicationCount)
        => applicationCount >= 0 && Enum.IsDefined(status);
}
