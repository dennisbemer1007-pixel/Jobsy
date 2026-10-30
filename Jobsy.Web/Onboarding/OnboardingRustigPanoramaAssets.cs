namespace Jobsy.Web.Onboarding;

/// <summary>Versioned panorama hero strips for calm onboarding (kreeftreis v4).</summary>
public static class OnboardingRustigPanoramaAssets
{
    public static string StartDesktop => OnboardingKreeftStripAssets.StartDesktop;

    public static string StartMobile => OnboardingKreeftStripAssets.StartMobile;

    public static string Desktop(int step) => DesktopForStep(step);

    public static string Mobile(int step) => MobileForStep(step);

    public static string DesktopForStep(int step) => OnboardingKreeftStripAssets.StepDesktop(step);

    public static string MobileForStep(int step) => OnboardingKreeftStripAssets.StepMobile(step);
}
