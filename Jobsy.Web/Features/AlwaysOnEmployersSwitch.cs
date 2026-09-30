namespace Jobsy.Web.Features;

/// <summary>
/// Default production registration while <c>IFeatureFlags</c> is absent.
/// OFF behaviour is exercised via test doubles and Development <c>?_variant=zw</c> / <c>Landing:ForceVariant</c>.
/// </summary>
public sealed class AlwaysOnEmployersSwitch : IEmployersSwitch
{
    public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default)
        => ValueTask.FromResult(true);

    public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
        => ValueTask.FromResult(LandingVariant.On);
}
