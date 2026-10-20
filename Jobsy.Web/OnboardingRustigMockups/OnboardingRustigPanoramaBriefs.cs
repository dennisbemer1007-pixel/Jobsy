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
        "Ultra-wide panoramic banner. Warm Lobsy cream #F7F4F0 flat illustration NOT photo. " +
        "Small full-body figures on a ground line in the vertical middle; generous sky above; " +
        "scenery continuous to left and right edges. Every person FULL BODY head to toe. No text.";
}
