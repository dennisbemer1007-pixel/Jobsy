using Jobsy.Core.Entities;
using Jobsy.Core.Features;

namespace Jobsy.Core.WerkgeverApk;

/// <summary>
/// Page/API helper when platform and company toggles both apply to Loopbaan-APK.
/// </summary>
public static class WerkgeverApkFeatureGate
{
    public static async Task<bool> IsGlobalEnabledAsync(
        IFeatureFlags features,
        CancellationToken cancellationToken = default)
    {
        return await features.IsEnabledAsync(PlatformFeature.EmployerLoopbaanApk, cancellationToken);
    }

    public static bool IsCompanyEnabled(Company company)
        => company.LoopbaanApkEnabled;

    public static async Task<bool> IsAvailableAsync(
        Company company,
        IFeatureFlags features,
        CancellationToken cancellationToken = default)
    {
        if (!IsCompanyEnabled(company))
        {
            return false;
        }

        return await IsGlobalEnabledAsync(features, cancellationToken);
    }
}