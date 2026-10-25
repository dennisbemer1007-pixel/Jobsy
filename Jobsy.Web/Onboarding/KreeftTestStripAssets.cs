using Jobsy.Web.Media;

namespace Jobsy.Web.Onboarding;

public static class KreeftTestStripAssets
{
    public static string Desktop(string slug) => slug switch
    {
        "hoe-je-werkt" => KreeftHeaderStripAssets.Desk("diepte-werken"),
        "leuk" => KreeftHeaderStripAssets.Desk("diepte-leuk"),
        "sfeer" => KreeftHeaderStripAssets.Desk("diepte-thuis"),
        "waarden" => KreeftHeaderStripAssets.Desk("diepte-waarden"),
        _ => KreeftHeaderStripAssets.Desk("diepte-werken"),
    };

    public static string Mobile(string slug) => slug switch
    {
        "hoe-je-werkt" => KreeftHeaderStripAssets.Mob("diepte-werken"),
        "leuk" => KreeftHeaderStripAssets.Mob("diepte-leuk"),
        "sfeer" => KreeftHeaderStripAssets.Mob("diepte-thuis"),
        "waarden" => KreeftHeaderStripAssets.Mob("diepte-waarden"),
        _ => KreeftHeaderStripAssets.Mob("diepte-werken"),
    };
}
