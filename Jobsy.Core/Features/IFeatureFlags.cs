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
    bool PhoneVerificationEnabled = false,
    bool SchoolsEnabled = false,
    bool AmbassadorsEnabled = false,
    bool WhatsAppRemindersEnabled = false,
    bool CompactTestPdfEnabled = false,
    bool HonestAdviceEnabled = false,
    bool FutureJobsForYouEnabled = false,
    bool EmployerPhase2Enabled = false)
{
    public static FeatureFlagSnapshot Defaults { get; } = new(EmployersEnabled: false, CandidatePassportEnabled: true);

    public bool IsEnabled(PlatformFeature feature) => feature switch
    {
        PlatformFeature.Employers => EmployersEnabled,
        PlatformFeature.CandidatePassport => CandidatePassportEnabled,
        PlatformFeature.PassportPartners => PassportPartnersEnabled,
        PlatformFeature.PassportPdfV2 => PassportPdfV2Enabled,
        PlatformFeature.Schools => SchoolsEnabled,
        PlatformFeature.Ambassadors => AmbassadorsEnabled,
        PlatformFeature.WhatsAppReminders => WhatsAppRemindersEnabled,
        PlatformFeature.CompactTestPdf => CompactTestPdfEnabled,
        PlatformFeature.HonestAdvice => HonestAdviceEnabled,
        PlatformFeature.FutureJobsForYou => FutureJobsForYouEnabled,
        PlatformFeature.EmployerPhase2 => EmployersEnabled && EmployerPhase2Enabled,
        _ => false
    };
}
