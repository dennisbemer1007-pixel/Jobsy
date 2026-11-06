namespace Jobsy.Web.Onboarding;

public static class OnboardingRustigKompasIllustrationAssets
{
    public const string Version = "20261022-kompasc";

    public static string DesktopJpg => Jobsy.Web.Media.KreeftHeaderStripAssets.LichtKompasDesk;

    public static string MobileJpg => Jobsy.Web.Media.KreeftHeaderStripAssets.LichtKompasMob;

    public static string DirectionZorg =>
        $"/images/onboarding/rustig-mockup/kompas-b-direction-zorg.jpg?v={Version}";

    public static string DirectionGroen =>
        $"/images/onboarding/rustig-mockup/kompas-b-direction-groen.jpg?v={Version}";

    public static string DirectionTeam =>
        $"/images/onboarding/rustig-mockup/kompas-b-direction-team.jpg?v={Version}";
}
