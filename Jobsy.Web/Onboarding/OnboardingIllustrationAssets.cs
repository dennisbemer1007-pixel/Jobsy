namespace Jobsy.Web.Onboarding;

/// <summary>Shared onboarding illustration paths (WebP + JPG fallback).</summary>
public static class OnboardingIllustrationAssets
{
    public const string Version = "20261018-03";

    public const string DreamJobWebp = $"/images/onboarding/droombaan.webp?v={Version}";
    public const string DreamJobJpg = $"/images/onboarding/droombaan.jpg?v={Version}";
}
