using Jobsy.Web.Components.Candidate.Onboarding;
using Jobsy.Web.Models;
using Jobsy.Web.Onboarding;

namespace Jobsy.Tests;

public sealed class OnboardingKompasSsrDataTests
{
    [Fact]
    public void ResolveHomeCityLabel_uses_city_after_postcode_in_street_address()
    {
        var profile = new MeProfile
        {
            Preferences = new CandidatePreferences
            {
                HomeAddress = "Kalverstraat 1, 1012NX Amsterdam",
            },
        };
        var draft = new OnboardingProfileDraft { City = "Kalverstraat 1, 1012NX Amsterdam" };

        Assert.Equal("Amsterdam", OnboardingKompasSsrData.ResolveHomeCityLabel(profile, draft));
    }
}
