using Jobsy.Core.Features;

namespace Jobsy.Tests;

/// <summary>Test double: employers ON, passport ON (production defaults).</summary>
internal sealed class AlwaysOnFeatureFlags : IFeatureFlags
{
    public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(FeatureFlagSnapshot.Defaults);

    public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(FeatureFlagSnapshot.Defaults.IsEnabled(feature));

    public void Invalidate()
    {
    }
}
