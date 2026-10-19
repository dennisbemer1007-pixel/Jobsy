using Jobsy.Core.Features;

namespace Jobsy.Web.UwvFeatures;

public interface IUwvFeaturesGate
{
    ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default);
}

public sealed class UwvFeaturesGate(IFeatureFlags featureFlags) : IUwvFeaturesGate
{
    public ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
        => featureFlags.IsEnabledAsync(PlatformFeature.UwvFeatures, cancellationToken);
}
