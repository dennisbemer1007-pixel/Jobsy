using Jobsy.Web.Media;

namespace Jobsy.Web.Onboarding;

/// <summary>Onboarding wizard steps → kreeft v4 strips (screens 03–14).</summary>
public static class OnboardingKreeftStripAssets
{
    public static string StartDesktop => KreeftHeaderStripAssets.RouteKaartDesk;
    public static string StartMobile => KreeftHeaderStripAssets.RouteKaartMob;

    public static string StepDesktop(int step) => step switch
    {
        1 => KreeftHeaderStripAssets.Desk("kust-kennismaken"),
        2 => KreeftHeaderStripAssets.Desk("kust-wanneer"),
        3 => KreeftHeaderStripAssets.Desk("kust-vervoer"),
        4 => KreeftHeaderStripAssets.Desk("rotsen-werk"),
        5 => KreeftHeaderStripAssets.Desk("rotsen-leren"),
        6 => KreeftHeaderStripAssets.Desk("rotsen-droom"),
        7 => KreeftHeaderStripAssets.Desk("diepte-werken"),
        8 => KreeftHeaderStripAssets.Desk("diepte-leuk"),
        9 => KreeftHeaderStripAssets.Desk("diepte-thuis"),
        10 => KreeftHeaderStripAssets.Desk("diepte-waarden"),
        _ => KreeftHeaderStripAssets.Desk($"step-{step:D2}"),
    };

    public static string StepMobile(int step) => step switch
    {
        1 => KreeftHeaderStripAssets.Mob("kust-kennismaken"),
        2 => KreeftHeaderStripAssets.Mob("kust-wanneer"),
        3 => KreeftHeaderStripAssets.Mob("kust-vervoer"),
        4 => KreeftHeaderStripAssets.Mob("rotsen-werk"),
        5 => KreeftHeaderStripAssets.Mob("rotsen-leren"),
        6 => KreeftHeaderStripAssets.Mob("rotsen-droom"),
        7 => KreeftHeaderStripAssets.Mob("diepte-werken"),
        8 => KreeftHeaderStripAssets.Mob("diepte-leuk"),
        9 => KreeftHeaderStripAssets.Mob("diepte-thuis"),
        10 => KreeftHeaderStripAssets.Mob("diepte-waarden"),
        _ => KreeftHeaderStripAssets.Mob($"step-{step:D2}"),
    };
}
