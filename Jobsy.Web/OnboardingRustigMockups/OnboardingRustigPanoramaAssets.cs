namespace Jobsy.Web.OnboardingRustigMockups;

public static class OnboardingRustigPanoramaAssets
{
    public const string Version = "20261020-u10a";
    public const string StartVersion = "20261020-start-g";

    public static string Desktop(string slug) =>
        $"/images/onboarding/rustig-panorama/{slug}-desktop.jpg?v={CacheVersion(slug)}";

    public static string Mobile(string slug) =>
        $"/images/onboarding/rustig-panorama/{slug}-mobile.jpg?v={CacheVersion(slug)}";

    private static string CacheVersion(string slug) =>
        string.Equals(slug, "start", StringComparison.OrdinalIgnoreCase) ? StartVersion : Version;
}
