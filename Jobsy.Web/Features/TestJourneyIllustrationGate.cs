using Jobsy.Core.Features;

namespace Jobsy.Web.Features;

/// <summary>
/// Illustrated "Wie ben jij" test journey intro (step 7). Resolved via platform feature flags API.
/// </summary>
public interface ITestJourneyIllustrationGate
{
    ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default);
}

public sealed class TestJourneyIllustrationGate(IFeatureFlags featureFlags) : ITestJourneyIllustrationGate
{
    public async ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
        => (await featureFlags.GetAsync(cancellationToken)).TestJourneyIllustrationEnabled;
}
