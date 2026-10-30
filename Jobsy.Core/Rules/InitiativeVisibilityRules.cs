using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class InitiativeVisibilityRules
{
    public static bool EmployerMaySeePii(WorkInitiativeStatus status)
        => status == WorkInitiativeStatus.Accepted;

    public static bool IsOpenForEmployer(WorkInitiativeStatus status)
        => status is WorkInitiativeStatus.Pending or WorkInitiativeStatus.Viewed;

    public static bool IsTerminalForCandidate(WorkInitiativeStatus status)
        => status is WorkInitiativeStatus.Declined
            or WorkInitiativeStatus.AutoDeclined
            or WorkInitiativeStatus.Withdrawn
            or WorkInitiativeStatus.Accepted;
}
