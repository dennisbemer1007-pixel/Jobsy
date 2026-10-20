namespace Jobsy.Web.Onboarding;

/// <summary>Versioned panorama hero JPEGs for calm onboarding (PR #762 assets).</summary>
public static class OnboardingRustigPanoramaAssets
{
    public const string StartDesktopVersion = "20261020-start-g";
    public const string StartMobileVersion = "20261020-start-d";

    public static string StartDesktop =>
        $"/images/onboarding/rustig-panorama/start-desktop.jpg?v={StartDesktopVersion}";

    public static string StartMobile =>
        $"/images/onboarding/rustig-panorama/start-mobile.jpg?v={StartMobileVersion}";
}
