using Jobsy.Core.Entities;

namespace Jobsy.Core.Privacy;

public static class ResearchConsentRules
{
    public static bool HasCurrentResearchConsent(User user)
        => user.ResearchConsentAt is not null
           && string.Equals(
               user.ResearchConsentVersion,
               PrivacyConstants.ResearchConsentTextVersion,
               StringComparison.Ordinal);

    public static bool DemographicsAllowed(User user)
        => HasCurrentResearchConsent(user);
}
