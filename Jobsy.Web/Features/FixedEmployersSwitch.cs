namespace Jobsy.Web.Features;

/// <summary>Test / DI double that pins employers ON or OFF.</summary>
public sealed class FixedEmployersSwitch(bool enabled) : IEmployersSwitch
{
    public ValueTask<bool> IsEnabledAsync(CancellationToken ct = default)
        => ValueTask.FromResult(enabled);

    public ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default)
        => ValueTask.FromResult(enabled ? LandingVariant.On : LandingVariant.Zw);
}
