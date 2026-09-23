using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Unlock gate for the candidate Match &amp; Swipe tab.
/// Basics + education + competency (IPIP) + career + DISC — Wie ben ik–style checklist.
/// </summary>
public static class MatchProfileCompleteness
{
    public static bool HasEducationLevel(IEnumerable<string>? educations)
    {
        if (educations is null)
        {
            return false;
        }

        foreach (var item in educations)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            if (string.Equals(item.Trim(), EducationLevelLabels.None, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public static bool HasEducationLevel(CandidatePreferencesDto? prefs)
        => HasEducationLevel(prefs?.Educations);

    /// <summary>
    /// <c>IsProfileComplete</c> for Match: all required checklist items done.
    /// </summary>
    public static bool IsProfileComplete(
        bool profileBasicsFilled,
        bool hasEducationLevel,
        bool competencyCompleted,
        bool careerCompleted,
        bool discCompleted)
        => profileBasicsFilled
           && hasEducationLevel
           && competencyCompleted
           && careerCompleted
           && discCompleted;

    public static int CompletedCount(
        bool profileBasicsFilled,
        bool hasEducationLevel,
        bool competencyCompleted,
        bool careerCompleted,
        bool discCompleted)
        => (profileBasicsFilled ? 1 : 0)
           + (hasEducationLevel ? 1 : 0)
           + (competencyCompleted ? 1 : 0)
           + (careerCompleted ? 1 : 0)
           + (discCompleted ? 1 : 0);

    public const int RequiredStepCount = 5;
}
