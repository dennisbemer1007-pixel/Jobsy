using Jobsy.Core.Features;

namespace Jobsy.Tests;

/// <summary>Test double: employers ON and passport ON. Production defaults are employers OFF.</summary>
internal sealed class AlwaysOnFeatureFlags : IFeatureFlags
{
    private static readonly FeatureFlagSnapshot On = new(EmployersEnabled: true, CandidatePassportEnabled: true);

    public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(On);

    public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(On.IsEnabled(feature));

    public void Invalidate()
    {
    }
}

/// <summary>
/// Web hosts whose API stand-in cannot answer <c>api/settings/feature-flags</c> fall back to
/// <see cref="FeatureFlagSnapshot.Defaults"/> (employers OFF). Suites that still exercise the
/// employers-on product pass the flag explicitly.
/// </summary>
internal sealed class FixedFeatureFlags(bool employersEnabled, bool candidatePassportEnabled = true) : IFeatureFlags
{
    private readonly FeatureFlagSnapshot _snapshot = new(employersEnabled, candidatePassportEnabled);

    public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_snapshot);

    public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_snapshot.IsEnabled(feature));

    public void Invalidate()
    {
    }
}
