using Microsoft.Extensions.Configuration;

namespace Jobsy.Web.Features;

/// <summary>Calm onboarding visual shell on /candidate/start (config: Onboarding:RustigEnabled).</summary>
public interface IOnboardingRustigGate
{
    bool Enabled { get; }
}

public sealed class OnboardingRustigGate(IConfiguration configuration) : IOnboardingRustigGate
{
    public bool Enabled => configuration.GetValue("Onboarding:RustigEnabled", true);
}
