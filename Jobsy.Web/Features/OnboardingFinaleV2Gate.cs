using Jobsy.Core.Features;

namespace Jobsy.Web.Features;

/// <summary>Onboarding finale v2 cards. Resolved via platform feature flags (admin overrides env).</summary>
public interface IOnboardingFinaleV2Gate
{
    ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default);
}

public sealed class OnboardingFinaleV2Gate(IFeatureFlags featureFlags) : IOnboardingFinaleV2Gate
{
    public async ValueTask<bool> IsEnabledAsync(CancellationToken cancellationToken = default)
        => (await featureFlags.GetAsync(cancellationToken)).OnboardingFinaleV2Enabled;
}
