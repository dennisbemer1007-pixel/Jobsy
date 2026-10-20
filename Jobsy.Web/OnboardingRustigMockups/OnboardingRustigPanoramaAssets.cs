namespace Jobsy.Web.OnboardingRustigMockups;

public static class OnboardingRustigPanoramaAssets
{
    public const string Version = "20261020-u8b";

    public static string Desktop(string slug) =>
        $"/images/onboarding/rustig-panorama/{slug}-desktop.jpg?v={Version}";

    public static string Mobile(string slug) =>
        $"/images/onboarding/rustig-panorama/{slug}-mobile.jpg?v={Version}";
}
