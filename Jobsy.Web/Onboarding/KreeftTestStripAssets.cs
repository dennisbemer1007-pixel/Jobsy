namespace Jobsy.Web.Onboarding;

/// <summary>Standalone Quick-Scan test header strips (kreeftreis v4, screens 10–14).</summary>
public static class KreeftTestStripAssets
{
    public const string Version = OnboardingKreeftStripAssets.Version;

    private const string Base = "/images/onboarding/kreeft-v4/tests";

    public static string Desktop(string slug) =>
        $"{Base}/bg-{slug}-desktop.webp?v={Version}";

    public static string Mobile(string slug) =>
        $"{Base}/bg-{slug}-mobile.webp?v={Version}";
}
