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

        if (HasSubstantialWorkExperience(preferences))
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

    /// <summary>Adults with paid work history get the standard recruiter paspoort, not the student layout.</summary>
    public static bool HasSubstantialWorkExperience(CandidatePreferencesDto preferences)
    {
        if (preferences.NoWorkExperience == true)
        {
            return false;
        }

        var employers = preferences.Employers ?? [];
        if (employers.Count >= 2)
        {
            return true;
        }

        if (employers.Count != 1)
        {
            return false;
        }

        var job = employers[0];
        var blob = $"{job.Role} {job.EmployerName}";
        if (blob.Contains("stage", StringComparison.OrdinalIgnoreCase)
            || blob.Contains("intern", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(job.StartMonth);
    }

    private static bool IsHigherEducation(string level)
        => level.Contains("MBO", StringComparison.OrdinalIgnoreCase)
           || level.Contains("HBO", StringComparison.OrdinalIgnoreCase)
           || level.Contains("WO", StringComparison.OrdinalIgnoreCase)
           || level.Contains("Universiteit", StringComparison.OrdinalIgnoreCase);
}
