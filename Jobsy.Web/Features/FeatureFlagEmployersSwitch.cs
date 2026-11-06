using Jobsy.Core.Features;

namespace Jobsy.Web.Features;

/// <summary>
/// Production <see cref="IEmployersSwitch"/>: reads <see cref="IFeatureFlags.EmployersEnabled"/>.
/// <see cref="AlwaysOnEmployersSwitch"/> and <see cref="FixedEmployersSwitch"/> stay for tests.
/// </summary>
public sealed class FeatureFlagEmployersSwitch(IFeatureFlags flags) : IEmployersSwitch
{
    public async ValueTask<bool> IsEnabledAsync(CancellationToken ct = default)
        => (await flags.GetAsync(ct)).EmployersEnabled;

    public async ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
        => (await flags.GetAsync(ct)).EmployersEnabled ? LandingVariant.On : LandingVariant.Zw;
}
