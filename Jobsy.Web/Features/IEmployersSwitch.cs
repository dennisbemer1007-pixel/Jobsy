namespace Jobsy.Web.Features;

/// <summary>
/// Seam for "Werkgevers actief". Production registration is <see cref="FeatureFlagEmployersSwitch"/>
/// over <c>IFeatureFlags</c>. Anonymous "/" OFF renders the landing -zw variant; the home canonical stays "/".
/// OFF-anonymous home is "/" (was /ontdek). <see cref="EmployersGate"/> still gates /banenkaart and /banen
/// until that middleware is folded into <c>RequiresFeature</c> (follow-up item 4).
/// </summary>
public interface IEmployersSwitch
{
    ValueTask<bool> IsEnabledAsync(CancellationToken ct = default);

    ValueTask<LandingVariant> VariantAsync(CancellationToken ct = default);
}
