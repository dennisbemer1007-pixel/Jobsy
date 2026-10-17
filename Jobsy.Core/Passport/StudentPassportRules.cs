using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Passport;

public static class StudentPassportRules
{
    public static bool Qualifies(CandidatePreferencesDto? preferences)
    {
        if (preferences is null)
        {
            return false;
        }

        if (WorkStatusCatalog.PrefersInternshipsOrSideJobs(preferences.WorkStatuses))
        {
            return true;
        }

        foreach (var level in preferences.Educations ?? [])
        {
            if (IsHigherEducation(level))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsHigherEducation(string level)
        => level.Contains("MBO", StringComparison.OrdinalIgnoreCase)
           || level.Contains("HBO", StringComparison.OrdinalIgnoreCase)
           || level.Contains("WO", StringComparison.OrdinalIgnoreCase)
           || level.Contains("Universiteit", StringComparison.OrdinalIgnoreCase);
}
