namespace Jobsy.Web.Features;

/// <summary>
/// Seam for "Werkgevers actief". Default registration is <see cref="AlwaysOnEmployersSwitch"/>.
/// When werkgevers-actief lands it must:
/// (1) register an <see cref="IEmployersSwitch"/> adapter over <c>IFeatureFlags</c>,
/// (2) anonymous "/" OFF renders the landing -zw variant instead of redirecting to /ontdek; the home canonical stays "/",
/// (3) ON-candidate home = /banenkaart,
/// (4) replace <see cref="EmployersGate"/> with <c>RequiresFeature</c>.
/// See <c>docs/feature-flags-landing-followup.md</c>.
/// </summary>
public interface IEmployersSwitch
{
    ValueTask<bool> IsEnabledAsync(CancellationToken ct = default);

    ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default);
}
