namespace Jobsy.Web.OnboardingRustigMockups;

public static class OnboardingRustigPanoramaAssets
{
    public const string Version = "20261020-p1";

    public static string Desktop(string slug) =>
        $"/images/mockups/onboarding-rustig-panorama/{slug}-desktop.jpg?v={Version}";

    public static string Mobile(string slug) =>
        $"/images/mockups/onboarding-rustig-panorama/{slug}-mobile.jpg?v={Version}";
}
