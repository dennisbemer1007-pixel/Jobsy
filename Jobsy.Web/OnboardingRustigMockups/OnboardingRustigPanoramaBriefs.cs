namespace Jobsy.Web.OnboardingRustigMockups;

/// <summary>PO topic brief per mockup header (unique art, no reuse).</summary>
public static class OnboardingRustigPanoramaBriefs
{
    public static string TopicNl(string slug) => slug switch
    {
        "start" => "Welkom: start van je reis, pad dat opengaat, iemand zwaait bij een trailhead",
        "step-01" => "Kennismaken: huis en buurt, adres, geboortedatum-kalender, werkstatus",
        "step-02" => "Beschikbaarheid: weekkalender, klokken ochtend-middag-avond, rooster",
        "step-03" => "Vervoer: lopen, fiets, trein, bus, auto, scooter over de volle breedte",
        "step-04" => "Werkervaring: horeca, winkel, magazijn als eerdere banen",
        "step-05" => "Opleiding: boeken, diploma, campus/bibliotheek",
        "step-06" => "Droombaan: horizon, sterrenpad, droomberoepen in de verte",
        "step-07" => "Vier tests: reis met vier duidelijke borden/stations",
        "step-08" => "Hoe je werkt: samenwerken, team, whiteboard",
        "step-09" => "Leuk: energie van doen, gereedschap, makerplezier",
        "step-10" => "Sfeer: warme werksfeer, lounge, koffie, informeel",
        "step-11" => "Waarden: zorg, community, duurzaamheid, zinvol werk",
        "kompas" => "Kompas klaar: heuveltop, kompas, uitzicht vooruit",
        _ => slug,
    };

    public const string CompositionRules =
        "Soft watercolor panorama (match start, step-01, step-03): warm Lobsy cream #F7F4F0, same brush and daylight. " +
        "Full-width edge-to-edge scenery; small full-body figures with generous sky above heads; soft fade into cream at bottom and sides. " +
        "Unique on-topic scene per screen. No text in the art.";
}
