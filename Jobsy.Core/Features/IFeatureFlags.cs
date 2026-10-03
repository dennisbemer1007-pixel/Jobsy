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
    bool PhoneVerificationEnabled = false)
{
    public static FeatureFlagSnapshot Defaults { get; } = new(EmployersEnabled: true, CandidatePassportEnabled: true);

    public bool IsEnabled(PlatformFeature feature) => feature switch
    {
        PlatformFeature.Employers => EmployersEnabled,
        PlatformFeature.CandidatePassport => CandidatePassportEnabled,
        PlatformFeature.PassportPartners => PassportPartnersEnabled,
        PlatformFeature.PassportPdfV2 => PassportPdfV2Enabled,
        _ => false
    };
}
