namespace Jobsy.Web.Features;

/// <summary>
/// Reads reference-verification availability without duplicating PR #724 platform wiring.
/// Default false until <c>ReferenceVerificationEnabled</c> is on the feature snapshot.
/// </summary>
public interface IReferenceVerificationFeatureGate
{
    ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default);
}
