using Jobsy.Core.Privacy;
using Jobsy.Web.Models;

namespace Jobsy.Web.Services;

public static class GratisDnaMergeProfileRules
{
    public static bool CanUseCandidateFeatures(MeProfile profile, DateOnly? today = null)
    {
        var age = CandidateConsentRules.AgeYears(profile.DateOfBirth, today);
        if (age is int value && value < CandidateConsentRules.ParentalConsentAge)
        {
            return profile.ParentalConsentAt is not null;
        }

        return true;
    }

    public static bool HasCurrentTestAiConsent(MeProfile profile)
        => profile.TestAiConsentAt is not null
           && string.Equals(
               profile.TestAiConsentVersion,
               PrivacyConstants.CandidateProfilingConsentVersion,
               StringComparison.Ordinal);
}

public static class GratisDnaServerAnswerParser
{
    public static Dictionary<int, int> Parse(Dictionary<string, int>? answers)
    {
        var result = new Dictionary<int, int>();
        if (answers is null)
        {
            return result;
        }

        foreach (var (key, value) in answers)
        {
            if (int.TryParse(key, out var id) && value is >= 1 and <= 5)
            {
                result[id] = value;
            }
        }

        return result;
    }
}
