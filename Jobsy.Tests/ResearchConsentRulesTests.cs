using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Privacy;

namespace Jobsy.Tests;

public class ResearchConsentRulesTests
{
    [Fact]
    public void Research_consent_requires_current_text_version()
    {
        var user = new User { Role = UserRole.Candidate };
        Assert.False(ResearchConsentRules.HasCurrentResearchConsent(user));

        user.ResearchConsentAt = DateTime.UtcNow;
        user.ResearchConsentVersion = "oud";
        Assert.False(ResearchConsentRules.HasCurrentResearchConsent(user));

        user.ResearchConsentVersion = PrivacyConstants.ResearchConsentTextVersion;
        Assert.True(ResearchConsentRules.HasCurrentResearchConsent(user));
    }
}
