using Jobsy.Core.Features;

namespace Jobsy.Web.Features;

public sealed class ReferenceVerificationFeatureGate(IFeatureFlags featureFlags) : IReferenceVerificationFeatureGate
{
    public ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
        => featureFlags.IsEnabledAsync(PlatformFeature.ReferenceVerification, cancellationToken);
}
