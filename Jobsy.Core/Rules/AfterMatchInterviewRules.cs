using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

public static class AfterMatchInterviewRules
{
    public const int MinEmployerSlots = 2;
    public const int MaxEmployerSlots = 3;
    public const int DefaultDurationMinutes = 30;

    public static bool CanProposeEmployerSlots(int count) =>
        count is >= MinEmployerSlots and <= MaxEmployerSlots;

    public static bool ApplicationEligible(ApplicationStatus status) =>
        status is ApplicationStatus.Accepted
            or ApplicationStatus.EmployerContacting
            or ApplicationStatus.Hired;
}
