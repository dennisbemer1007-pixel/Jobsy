namespace Jobsy.Web.Media;

/// <summary>
/// Kreeftreis v4 full-width header strips (PR #878 @ 53863b26, docs/mockups/kandidaat/kreeft/src/strips/).
/// Desktop ~225px, mobile ~105px (CSS crops source art).
/// </summary>
public static class KreeftHeaderStripAssets
{
    public const string Version = "53863b26-kreeft-v4";

    private const string Base = "/images/kreeft/strips";

    public static string Desk(string key) => $"{Base}/{key}-desk.webp?v={Version}";

    public static string Mob(string key) => $"{Base}/{key}-mob.webp?v={Version}";

    public static string LandingDesk => Desk("landing");
    public static string LandingMob => Mob("landing");

    public static string InloggenDesk => Desk("inloggen");
    public static string InloggenMob => Mob("inloggen");

    public static string RouteKaartDesk => Desk("route-kaart");
    public static string RouteKaartMob => Mob("route-kaart");

    public static string LichtKompasDesk => Desk("licht-kompas");
    public static string LichtKompasMob => Mob("licht-kompas");

    public static string LichtPaspoortDesk => Desk("licht-paspoort");
    public static string LichtPaspoortMob => Mob("licht-paspoort");

    public static string BanenkaartDesk => Desk("banenkaart");
    public static string BanenkaartMob => Mob("banenkaart");

    public static string BedrijvenkaartDesk => Desk("bedrijvenkaart");
    public static string BedrijvenkaartMob => Mob("bedrijvenkaart");

    public static string BewaardDesk => Desk("bewaard");
    public static string BewaardMob => Mob("bewaard");

    public static string SollicitatiesDesk => Desk("sollicitaties");
    public static string SollicitatiesMob => Mob("sollicitaties");

    public static string ActiviteitenDesk => Desk("activiteiten");
    public static string ActiviteitenMob => Mob("activiteiten");

    public static string BerichtenDesk => Desk("berichten");
    public static string BerichtenMob => Mob("berichten");

    public static string NaDeMatchDesk => Desk("na-de-match");
    public static string NaDeMatchMob => Mob("na-de-match");

    public static string MomentDesk => Desk("moment");
    public static string MomentMob => Mob("moment");

    public static string NuNietDesk => Desk("nu-niet");
    public static string NuNietMob => Mob("nu-niet");

    public static string CarriereDesk => Desk("carriere");
    public static string CarriereMob => Mob("carriere");

    public static string OmscholenDesk => Desk("omscholen");
    public static string OmscholenMob => Mob("omscholen");

    public static string WatDrijftDesk => Desk("wat-drijft");
    public static string WatDrijftMob => Mob("wat-drijft");

    public static string OndernemerDesk => Desk("ondernemer");
    public static string OndernemerMob => Mob("ondernemer");

    public static string AdviseurDesk => Desk("adviseur");
    public static string AdviseurMob => Mob("adviseur");

    public static string ProfielDesk => Desk("profiel");
    public static string ProfielMob => Mob("profiel");

    public static string InstellingenDesk => Desk("instellingen");
    public static string InstellingenMob => Mob("instellingen");

    public static string MatchDesk => Desk("match");
    public static string MatchMob => Mob("match");

    public static string ToevoegenDesk => Desk("toevoegen");
    public static string ToevoegenMob => Mob("toevoegen");

}
