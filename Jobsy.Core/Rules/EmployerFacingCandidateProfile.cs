using Jobsy.Core.Contracts;

namespace Jobsy.Core.Rules;

/// <summary>Single source for employer-visible candidate profile slices (API, CV, passport preview).</summary>
public static class EmployerFacingCandidateProfile
{
    public static CandidatePreferencesDto FilterPreferences(CandidatePreferencesDto preferences)
        => SensitiveExperienceRules.ForEmployerView(preferences);

    public static IReadOnlyList<string> VisibleSkillLabels(CandidatePreferencesDto preferences)
    {
        var labels = new List<string>();
        foreach (var entry in SensitiveExperienceRules.VisibleToEmployers(preferences.Employers))
        {
            if (entry.Skills is { Count: > 0 })
            {
                labels.AddRange(entry.Skills);
            }
            else if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                labels.AddRange(
                    entry.Description.Split(['\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }

        foreach (var hidden in (preferences.Employers ?? Array.Empty<CandidateEmployerHistoryDto>()).Where(SensitiveExperienceRules.IsOnlyForMe))
        {
            if (hidden.Skills is { Count: > 0 })
            {
                labels.AddRange(hidden.Skills);
            }
        }

        return labels
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(40)
            .ToList();
    }
}
