using Jobsy.Core.Features;
using Xunit;

namespace Jobsy.Tests;

public sealed class WebFeatureFlagsMappingTests
{
    [Fact]
    public void FeatureFlagSnapshot_kandidatenkaart_maps_after_passport_v2_slots()
    {
        var snap = new FeatureFlagSnapshot(
            EmployersEnabled: true,
            CandidatePassportEnabled: true,
            PassportPartnersEnabled: false,
            PassportPdfV2Enabled: true,
            PupilPassportV2Enabled: false,
            StudentPassportEnabled: false,
            PhoneVerificationEnabled: false,
            SchoolsEnabled: false,
            AmbassadorsEnabled: false,
            WhatsAppRemindersEnabled: false,
            CompactTestPdfEnabled: false,
            HonestAdviceEnabled: false,
            FutureJobsForYouEnabled: false,
            EmployerPhase2Enabled: false,
            CandidateExternalVacanciesEnabled: false,
            ProactiveMatchingEnabled: false,
            TestsV2Enabled: false,
            EmployerFitQuestionnairesEnabled: false,
            KandidatenkaartEnabled: true,
            CompanyMapInitiativeEnabled: true);

        Assert.True(snap.KandidatenkaartEnabled);
        Assert.True(snap.IsEnabled(PlatformFeature.Kandidatenkaart));
        Assert.True(snap.CompanyMapInitiativeEnabled);
        var employersOn = snap with { EmployersEnabled = true };
        Assert.True(employersOn.IsEnabled(PlatformFeature.CompanyMapInitiative));
        Assert.False(snap.EmployerFitQuestionnairesEnabled);
    }

    [Fact]
    public void FeatureFlagSnapshot_uwv_features_maps_platform_feature()
    {
        var snap = new FeatureFlagSnapshot(
            EmployersEnabled: true,
            CandidatePassportEnabled: true,
            UwvFeaturesEnabled: true);

        Assert.True(snap.UwvFeaturesEnabled);
        Assert.True(snap.IsEnabled(PlatformFeature.UwvFeatures));
    }
}
