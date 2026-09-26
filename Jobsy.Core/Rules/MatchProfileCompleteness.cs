using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>
/// Unlock gate for the candidate Match &amp; Swipe tab.
/// Basics + education answer (including "Geen") + onboarding wizard completed.
/// Full tests deepen the first impression; they are no longer required to unlock Match.
/// </summary>
public static class MatchProfileCompleteness
{
    /// <summary>
    /// Education step is complete when the candidate made an explicit choice,
    /// including <see cref="EducationLevelLabels.None"/> ("Geen" / geen specifiek niveau).
    /// Empty / unset is incomplete.
    /// </summary>
    public static bool HasEducationLevel(IEnumerable<string>? educations)
    {
        if (educations is null)
        {
            return false;
        }

        foreach (var item in educations)
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasEducationLevel(CandidatePreferencesDto? prefs)
        => HasEducationLevel(prefs?.Educations);

    /// <summary>
    /// <c>IsProfileComplete</c> for Match: basics + education + wizard done.
    /// </summary>
    public static bool IsProfileComplete(
        bool profileBasicsFilled,
        bool hasEducationLevel,
        bool wizardCompleted)
        => profileBasicsFilled
           && hasEducationLevel
           && wizardCompleted;

    public static int CompletedCount(
        bool profileBasicsFilled,
        bool hasEducationLevel,
        bool wizardCompleted)
        => (profileBasicsFilled ? 1 : 0)
           + (hasEducationLevel ? 1 : 0)
           + (wizardCompleted ? 1 : 0);

    public const int RequiredStepCount = 3;
}
