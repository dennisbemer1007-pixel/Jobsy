namespace Jobsy.Web.Onboarding;

/// <summary>Versioned panorama hero strips for calm onboarding (kreeftreis v4).</summary>
public static class OnboardingRustigPanoramaAssets
{
    public static string StartDesktop => OnboardingKreeftStripAssets.StartDesktop;

    public static string StartMobile => OnboardingKreeftStripAssets.StartMobile;

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

    public static string DesktopForStep(int step) => OnboardingKreeftStripAssets.StepDesktop(step);

    public static string MobileForStep(int step) => OnboardingKreeftStripAssets.StepMobile(step);

    public static string Desktop(string slug) =>
        OnboardingKreeftStripAssets.PanoramaDesktop(slug);

    public static string Mobile(string slug) =>
        OnboardingKreeftStripAssets.PanoramaMobile(slug);
}
