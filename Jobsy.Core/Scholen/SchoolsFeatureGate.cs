using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Page/API helper when <c>RequiresFeatureAttribute</c> is absent (Dependencies C).
/// Reads <c>SchoolsEnabled</c> from platform features.
/// </summary>
public static class SchoolsFeatureGate
{
    public static async Task<bool> IsEnabledAsync(
        IPlatformFeatureService features,
        CancellationToken cancellationToken = default)
    {
        var snap = await features.GetAsync(cancellationToken);
        return snap.SchoolsEnabled;
    }

    public static async Task<bool> IsEnabledAsync(
        IFeatureFlags features,
        CancellationToken cancellationToken = default)
    {
        var snap = await features.GetAsync(cancellationToken);
        return snap.SchoolsEnabled;
    }
}
