namespace Jobsy.Core.Features;

public interface IFeatureFlags
{
    ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default);

    void Invalidate();
}

public sealed record FeatureFlagSnapshot(
    bool EmployersEnabled,
    bool CandidatePassportEnabled)
{
    public static FeatureFlagSnapshot Defaults { get; } = new(EmployersEnabled: true, CandidatePassportEnabled: true);

    public bool IsEnabled(PlatformFeature feature) => feature switch
    {
        PlatformFeature.Employers => EmployersEnabled,
        PlatformFeature.CandidatePassport => CandidatePassportEnabled,
        _ => false
    };
}
