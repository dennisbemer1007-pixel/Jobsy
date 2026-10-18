namespace Jobsy.Web.Features;

/// <summary>Config <c>Onboarding:FinaleV2Enabled</c> (default false). Acceptatie enables via host env.</summary>
public static class OnboardingFinaleV2Gate
{
    public const string ConfigKey = "Onboarding:FinaleV2Enabled";

    public static bool IsEnabled(IConfiguration configuration)
        => configuration.GetValue(ConfigKey, false);
}
