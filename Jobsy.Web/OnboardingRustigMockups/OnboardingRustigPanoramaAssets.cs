namespace Jobsy.Web.OnboardingRustigMockups;

public static class OnboardingRustigPanoramaAssets
{
    public const string Version = "20261020-u10a";
    public const string StartVersion = "20261020-start-g";
    public const string Step01Version = "20261020-u1b";

    public static string Desktop(string slug) =>
        $"/images/onboarding/rustig-panorama/{slug}-desktop.jpg?v={CacheVersion(slug)}";

    public static string Mobile(string slug) =>
        $"/images/onboarding/rustig-panorama/{slug}-mobile.jpg?v={CacheVersion(slug)}";

    private static string CacheVersion(string slug)
    {
        if (string.Equals(slug, "start", StringComparison.OrdinalIgnoreCase))
            return StartVersion;
        if (string.Equals(slug, "step-01", StringComparison.OrdinalIgnoreCase))
            return Step01Version;
        return Version;
    }
}
