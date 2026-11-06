using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Match scores stay visible when availability is known; only hide when both open-for-work is off and availability is empty.
/// </summary>
public static class CandidateOpenForWorkGate
{
    public static bool HidesMatchScores(ProfileVacancyMatchContext context)
        => !context.OpenForWork && (context.Prefs.Availability?.Count ?? 0) == 0;
}
