namespace Jobsy.Web.Onboarding;

/// <summary>Kreeftreis v4 header strips (PR #878, docs/mockups/kandidaat/kreeft/src/strips/).</summary>
public static class OnboardingKreeftStripAssets
{
    public const string Version = "20261025-kreeft-v4";

    private const string Panorama = $"/images/onboarding/kreeft-v4/rustig-panorama";
    private const string Mockup = $"/images/onboarding/kreeft-v4/rustig-mockup";

    public static string PanoramaDesktop(string slug) =>
        $"{Panorama}/{slug}-desktop.webp?v={Version}";

    public static string PanoramaMobile(string slug) =>
        $"{Panorama}/{slug}-mobile.webp?v={Version}";

    public static string MockupDesktop(string slug) =>
        $"{Mockup}/{slug}-desktop.webp?v={Version}";

    public static string MockupMobile(string slug) =>
        $"{Mockup}/{slug}-mobile.webp?v={Version}";

    public static string StartDesktop => PanoramaDesktop("start");
    public static string StartMobile => PanoramaMobile("start");

    public static string StepDesktop(int step) => step switch
    {
        6 => MockupDesktop("step-06-droom"),
        7 => MockupDesktop("step-07-werken"),
        8 => PanoramaDesktop("step-08"),
        9 => PanoramaDesktop("step-09"),
        10 => PanoramaDesktop("step-10"),
        _ => PanoramaDesktop(ForSlug(step)),
    };

    public static string StepMobile(int step) => step switch
    {
        6 => MockupMobile("step-06-droom"),
        7 => MockupMobile("step-07-werken"),
        8 => PanoramaMobile("step-08"),
        9 => PanoramaMobile("step-09"),
        10 => PanoramaMobile("step-10"),
        _ => PanoramaMobile(ForSlug(step)),
    };

    private static string ForSlug(int step) => step switch
    {
        1 => "step-01",
        2 => "step-02",
        3 => "step-03",
        4 => "step-04",
        5 => "step-05",
        _ => $"step-{step:D2}",
    };
}
