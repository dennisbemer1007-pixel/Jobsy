using Jobsy.Core.Interfaces;

namespace Jobsy.Core.Sales;

/// <summary>
/// Server-side gate for Ambassadeur surfaces while AmbassadorsEnabled is off (Dependencies H).
/// </summary>
public static class AmbassadorsFeatureGate
{
    public const string FeatureDisabledJson = """{"error":"feature_disabled"}""";

    public static async Task<bool> IsEnabledAsync(
        IPlatformFeatureService features,
        CancellationToken cancellationToken = default)
    {
        var snap = await features.GetAsync(cancellationToken);
        return snap.AmbassadorsEnabled;
    }
}
