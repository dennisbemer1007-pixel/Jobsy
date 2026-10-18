using System.Reflection;
using Jobsy.Core.Features;

namespace Jobsy.Web.Features;

public sealed class ReferenceVerificationFeatureGate(IFeatureFlags featureFlags) : IReferenceVerificationFeatureGate
{
    public async ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
    {
        var snap = await featureFlags.GetAsync(cancellationToken);
        if (TryReadSnapshotFlag(snap, out var enabled))
        {
            return enabled;
        }

        return await featureFlags.IsEnabledAsync(PlatformFeature.ReferenceVerification, cancellationToken);
    }

    /// <summary>PR #724 adds <c>ReferenceVerificationEnabled</c> to the snapshot record.</summary>
    private static bool TryReadSnapshotFlag(FeatureFlagSnapshot snap, out bool enabled)
    {
        enabled = false;
        var prop = snap.GetType().GetProperty(
            "ReferenceVerificationEnabled",
            BindingFlags.Public | BindingFlags.Instance);
        if (prop?.PropertyType != typeof(bool))
        {
            return false;
        }

        enabled = prop.GetValue(snap) is true;
        return true;
    }
}
