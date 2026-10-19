namespace Jobsy.Core.Features;

public interface IFeatureFlags
{
    ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default);

    void Invalidate();
}

public sealed record FeatureFlagSnapshot(
    bool EmployersEnabled,
    bool CandidatePassportEnabled,
    bool PassportPartnersEnabled = false,
    bool PassportPdfV2Enabled = false,
    bool PupilPassportV2Enabled = false,
    bool StudentPassportEnabled = false,
    bool PhoneVerificationEnabled = false,
    bool SchoolsEnabled = false,
    bool AmbassadorsEnabled = false,
    bool WhatsAppRemindersEnabled = false,
    bool CompactTestPdfEnabled = false,
    bool HonestAdviceEnabled = false,
    bool FutureJobsForYouEnabled = false,
    bool EmployerPhase2Enabled = false,
    bool CandidateExternalVacanciesEnabled = false,
    bool ProactiveMatchingEnabled = false,
    bool TestsV2Enabled = false,
    bool EmployerFitQuestionnairesEnabled = false,
    bool KandidatenkaartEnabled = false,
    bool CompanyMapInitiativeEnabled = true,
    bool AfterMatchEnabled = false,
    bool ReferenceVerificationEnabled = false,
    bool SensitiveExperienceEnabled = false,
    bool TrainingPartnersEnabled = false,
    bool TestJourneyIllustrationEnabled = false,
    bool OnboardingFinaleV2Enabled = false,
    bool OccupationDayInLifeEnabled = true,
    bool TestEditModeEnabled = false,
    bool SamplePdfEnabled = false,
    bool TestDepthValueMockupEnabled = false)
{
    public static FeatureFlagSnapshot Defaults { get; } = new(
        EmployersEnabled: false,
        CandidatePassportEnabled: true,
        CompanyMapInitiativeEnabled: true);

    public bool IsEnabled(PlatformFeature feature) => feature switch
    {
        PlatformFeature.Employers => EmployersEnabled,
        PlatformFeature.CandidatePassport => CandidatePassportEnabled,
        PlatformFeature.PassportPartners => PassportPartnersEnabled,
        PlatformFeature.PassportPdfV2 => PassportPdfV2Enabled,
        PlatformFeature.PupilPassportV2 => PupilPassportV2Enabled,
        PlatformFeature.StudentPassport => StudentPassportEnabled,
        PlatformFeature.Schools => SchoolsEnabled,
        PlatformFeature.Ambassadors => AmbassadorsEnabled,
        PlatformFeature.WhatsAppReminders => WhatsAppRemindersEnabled,
        PlatformFeature.CompactTestPdf => CompactTestPdfEnabled,
        PlatformFeature.HonestAdvice => HonestAdviceEnabled,
        PlatformFeature.FutureJobsForYou => FutureJobsForYouEnabled,
        PlatformFeature.EmployerPhase2 => EmployersEnabled && EmployerPhase2Enabled,
        PlatformFeature.CandidateExternalVacancies => CandidateExternalVacanciesEnabled,
        PlatformFeature.ProactiveMatching => ProactiveMatchingEnabled,
        PlatformFeature.TestsV2 => TestsV2Enabled,
        PlatformFeature.EmployerFitQuestionnaires => EmployerFitQuestionnairesEnabled,
        PlatformFeature.Kandidatenkaart => KandidatenkaartEnabled,
        PlatformFeature.CompanyMapInitiative => EmployersEnabled && CompanyMapInitiativeEnabled,
        PlatformFeature.AfterMatch => AfterMatchEnabled,
        PlatformFeature.ReferenceVerification => ReferenceVerificationEnabled,
        PlatformFeature.SensitiveExperience => SensitiveExperienceEnabled,
        PlatformFeature.TrainingPartners => TrainingPartnersEnabled,
        PlatformFeature.TestDepthValueMockup => TestDepthValueMockupEnabled,
        _ => false
    };
}
