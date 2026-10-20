namespace Jobsy.Web.Onboarding;

/// <summary>Versioned panorama hero JPEGs for calm onboarding (PR #762 assets).</summary>
public static class OnboardingRustigPanoramaAssets
{
    public const string StartDesktopVersion = "20261020-start-g";
    public const string StartMobileVersion = "20261020-start-d";
    public const string Step01Version = "20261020-u1b";
    public const string Step03Version = "20261020-u3c";
    public const string DefaultVersion = "20261020-u10a";

    public static string StartDesktop =>
        $"/images/onboarding/rustig-panorama/start-desktop.jpg?v={StartDesktopVersion}";

    public static string StartMobile =>
        $"/images/onboarding/rustig-panorama/start-mobile.jpg?v={StartMobileVersion}";

    public static string ForStep(int step) => step switch
    {
        1 => "step-01",
        2 => "step-02",
        3 => "step-03",
        4 => "step-04",
        5 => "step-05",
        _ => $"step-{step:D2}",
    };

    public static string Desktop(int step) => Desktop(ForStep(step));

    public static string Mobile(int step) => Mobile(ForStep(step));

    public static string Desktop(string slug) =>
        $"/images/onboarding/rustig-panorama/{slug}-desktop.jpg?v={CacheVersion(slug)}";

    public static string Mobile(string slug) =>
        $"/images/onboarding/rustig-panorama/{slug}-mobile.jpg?v={CacheVersion(slug)}";

    private static string CacheVersion(string slug) =>
        slug switch
        {
            "start" => StartDesktopVersion,
            "step-01" => Step01Version,
            "step-03" => Step03Version,
            _ => DefaultVersion,
        };
}
