using Jobsy.Core.Features;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

internal static class QuickScanTestSupport
{
    internal static readonly QuickScanVersionPolicy VersionPolicy = new();
    internal static readonly IFeatureFlags FeatureFlagsOff = new OffFeatureFlags();

    private sealed class OffFeatureFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(FeatureFlagSnapshot.Defaults);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(false);

        public void Invalidate()
        {
        }
    }
}
