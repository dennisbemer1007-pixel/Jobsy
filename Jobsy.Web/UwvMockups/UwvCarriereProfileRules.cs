using Jobsy.Web.Models;

namespace Jobsy.Web.UwvMockups;

public static class UwvCarriereProfileRules
{
    public static bool PreferencesLookEmpty(CandidatePreferences? prefs)
    {
        if (prefs is null)
        {
            return true;
        }

        return prefs.Roles.Count == 0
               && string.IsNullOrWhiteSpace(prefs.AboutMe)
               && string.IsNullOrWhiteSpace(prefs.HomeAddress)
               && string.IsNullOrWhiteSpace(prefs.WorkRegion)
               && prefs.Employers.Count == 0
               && prefs.Educations.Count == 0
               && prefs.WorkStatuses.Count == 0;
    }
}
