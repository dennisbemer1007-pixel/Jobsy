using Jobsy.Core.Scholen;

namespace Jobsy.Web.Scholen;

public sealed record PupilRouteStop(
    int Num,
    string TitleKey,
    string SubKey,
    string Tone,
    bool Diamond = false,
    bool ShowHere = false);

public sealed record PupilRouteThemeView(
    string JourneyKey,
    string LeadKey,
    string MainButtonKey,
    string HeroImageStem,
    string HeroBgPosition,
    string MapImageStem,
    string MapSvgMarkup,
    IReadOnlyList<PupilRouteStop> Stops);

public static class PupilGroep78RouteCatalog
{
    private static readonly PupilRouteStop[] ZeeStops =
    [
        new(1, "Leerling.Route.Stop.Haven.Title", "Leerling.Route.Stop.Haven.Sub", "t-ok", ShowHere: true),
        new(2, "Leerling.Route.Stop.Reef.Title", "Leerling.Route.Stop.Reef.Sub", "t-navy"),
        new(3, "Leerling.Route.Stop.Schelpenrij.Title", "Leerling.Route.Stop.Schelpenrij.Sub", "t-brass", Diamond: true),
        new(4, "Leerling.Route.Stop.Cave.Title", "Leerling.Route.Stop.Cave.Sub", "t-navy"),
        new(5, "Leerling.Route.Stop.Schatkaart.Title", "Leerling.Route.Stop.Schatkaart.Sub", "t-brass", Diamond: true),
        new(6, "Leerling.Route.Stop.Island.Title", "Leerling.Route.Stop.Island.Sub", "t-teal"),
        new(7, "Leerling.Route.Stop.Lighthouse.Title", "Leerling.Route.Stop.Lighthouse.Sub", "t-navy"),
        new(8, "Leerling.Route.Stop.Lamps.Title", "Leerling.Route.Stop.Lamps.Sub", "t-brass", Diamond: true),
        new(9, "Leerling.Route.Stop.Lagoon.Title", "Leerling.Route.Stop.Lagoon.Sub", "t-navy"),
        new(10, "Leerling.Route.Stop.Result.Title", "Leerling.Route.Stop.Result.Sub", "t-coral"),
    ];

    private static readonly PupilRouteStop[] SterStops =
    [
        new(1, "Leerling.Route.Ster.Stop.Strandpaal.Title", "Leerling.Route.Ster.Stop.Strandpaal.Sub", "t-ok", ShowHere: true),
        new(2, "Leerling.Route.Ster.Stop.Duinpad.Title", "Leerling.Route.Ster.Stop.Duinpad.Sub", "t-navy"),
        new(3, "Leerling.Route.Stop.Schelpenrij.Title", "Leerling.Route.Stop.Schelpenrij.Sub", "t-brass", Diamond: true),
        new(4, "Leerling.Route.Ster.Stop.Maangrot.Title", "Leerling.Route.Ster.Stop.Maangrot.Sub", "t-navy"),
        new(5, "Leerling.Route.Stop.Schatkaart.Title", "Leerling.Route.Stop.Schatkaart.Sub", "t-brass", Diamond: true),
        new(6, "Leerling.Route.Stop.Island.Title", "Leerling.Route.Stop.Island.Sub", "t-teal"),
        new(7, "Leerling.Route.Stop.Lighthouse.Title", "Leerling.Route.Stop.Lighthouse.Sub", "t-navy"),
        new(8, "Leerling.Route.Stop.Vuurtorenlampen.Title", "Leerling.Route.Stop.Vuurtorenlampen.Sub", "t-brass", Diamond: true),
        new(9, "Leerling.Route.Ster.Stop.Sterrenbaai.Title", "Leerling.Route.Ster.Stop.Sterrenbaai.Sub", "t-navy"),
        new(10, "Leerling.Route.Stop.Result.Title", "Leerling.Route.Ster.Stop.Result.Sub", "t-coral"),
    ];

    private static readonly PupilRouteStop[] TuinStops =
    [
        new(1, "Leerling.Route.Tuin.Stop.Steiger.Title", "Leerling.Route.Tuin.Stop.Steiger.Sub", "t-ok", ShowHere: true),
        new(2, "Leerling.Route.Tuin.Stop.Bloemenbaai.Title", "Leerling.Route.Tuin.Stop.Bloemenbaai.Sub", "t-navy"),
        new(3, "Leerling.Route.Stop.Schelpenrij.Title", "Leerling.Route.Stop.Schelpenrij.Sub", "t-brass", Diamond: true),
        new(4, "Leerling.Route.Tuin.Stop.Koraaltuin.Title", "Leerling.Route.Tuin.Stop.Koraaltuin.Sub", "t-navy"),
        new(5, "Leerling.Route.Stop.Schatkaart.Title", "Leerling.Route.Stop.Schatkaart.Sub", "t-brass", Diamond: true),
        new(6, "Leerling.Route.Stop.Island.Title", "Leerling.Route.Stop.Island.Sub", "t-teal"),
        new(7, "Leerling.Route.Tuin.Stop.Waterval.Title", "Leerling.Route.Tuin.Stop.Waterval.Sub", "t-navy"),
        new(8, "Leerling.Route.Stop.Vuurtorenlampen.Title", "Leerling.Route.Stop.Vuurtorenlampen.Sub", "t-brass", Diamond: true),
        new(9, "Leerling.Route.Tuin.Stop.Zonnelagune.Title", "Leerling.Route.Tuin.Stop.Zonnelagune.Sub", "t-navy"),
        new(10, "Leerling.Route.Stop.Result.Title", "Leerling.Route.Stop.Result.Sub", "t-coral"),
    ];

    private const string ZeeMapSvg = """
        <path d="M160,430 C220,400 260,370 300,345 S330,280 352,262 S400,225 425,222 S480,270 505,300 S560,270 578,262 S680,200 718,192 S760,235 770,262 S800,320 812,345 S900,300 905,230 S880,150 855,126" fill="none" stroke="#fffdf8" stroke-width="9" stroke-linecap="round" opacity=".85" />
        <path d="M160,430 C220,400 260,370 300,345 S330,280 352,262 S400,225 425,222 S480,270 505,300 S560,270 578,262 S680,200 718,192 S760,235 770,262 S800,320 812,345 S900,300 905,230 S880,150 855,126" fill="none" stroke="#d4532f" stroke-width="4.5" stroke-dasharray="2 11" stroke-linecap="round" />
        <circle cx="160" cy="430" r="17" fill="#2f7d5b" stroke="#fffdf8" stroke-width="3" />
        <text x="160" y="436" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">1</text>
        <circle cx="300" cy="345" r="17" fill="#10284a" stroke="#fffdf8" stroke-width="3" />
        <text x="300" y="351" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">2</text>
        <rect x="338" y="248" width="28" height="28" rx="5" transform="rotate(45 352 262)" fill="#b98f3e" stroke="#fffdf8" stroke-width="3" />
        <text x="352" y="268" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">3</text>
        <circle cx="425" cy="222" r="17" fill="#10284a" stroke="#fffdf8" stroke-width="3" />
        <text x="425" y="228" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">4</text>
        <rect x="491" y="286" width="28" height="28" rx="5" transform="rotate(45 505 300)" fill="#b98f3e" stroke="#fffdf8" stroke-width="3" />
        <text x="505" y="306" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">5</text>
        <circle cx="578" cy="262" r="17" fill="#0f5b66" stroke="#fffdf8" stroke-width="3" />
        <text x="578" y="268" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">6</text>
        <circle cx="718" cy="192" r="17" fill="#10284a" stroke="#fffdf8" stroke-width="3" />
        <text x="718" y="198" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">7</text>
        <rect x="756" y="248" width="28" height="28" rx="5" transform="rotate(45 770 262)" fill="#b98f3e" stroke="#fffdf8" stroke-width="3" />
        <text x="770" y="268" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">8</text>
        <circle cx="812" cy="345" r="17" fill="#10284a" stroke="#fffdf8" stroke-width="3" />
        <text x="812" y="351" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">9</text>
        <circle cx="855" cy="126" r="17" fill="#d4532f" stroke="#fffdf8" stroke-width="3" />
        <text x="855" y="132" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">10</text>
        <circle cx="160" cy="430" r="27" fill="none" stroke="#ffd98a" stroke-width="4" />
        """;

    private const string TuinMapSvg = """
        <path d="M228,452 C280,430 320,410 350,388 S320,320 318,292 S360,210 392,196 S460,230 482,252 S540,290 566,292 S660,200 694,178 S760,235 772,262 S790,330 800,355 S910,300 915,230 S890,160 868,140" fill="none" stroke="#fffdf8" stroke-width="9" stroke-linecap="round" opacity=".85" />
        <path d="M228,452 C280,430 320,410 350,388 S320,320 318,292 S360,210 392,196 S460,230 482,252 S540,290 566,292 S660,200 694,178 S760,235 772,262 S790,330 800,355 S910,300 915,230 S890,160 868,140" fill="none" stroke="#b5452c" stroke-width="4.5" stroke-dasharray="2 11" stroke-linecap="round" />
        <circle cx="228" cy="452" r="17" fill="#4d7a5c" stroke="#fffdf8" stroke-width="3" />
        <text x="228" y="458" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">1</text>
        <circle cx="350" cy="388" r="17" fill="#3a2230" stroke="#fffdf8" stroke-width="3" />
        <text x="350" y="394" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">2</text>
        <rect x="304" y="278" width="28" height="28" rx="5" transform="rotate(45 318 292)" fill="#c08a4a" stroke="#fffdf8" stroke-width="3" />
        <text x="318" y="298" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">3</text>
        <circle cx="392" cy="196" r="17" fill="#3a2230" stroke="#fffdf8" stroke-width="3" />
        <text x="392" y="202" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">4</text>
        <rect x="468" y="238" width="28" height="28" rx="5" transform="rotate(45 482 252)" fill="#c08a4a" stroke="#fffdf8" stroke-width="3" />
        <text x="482" y="258" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">5</text>
        <circle cx="566" cy="292" r="17" fill="#4d7a5c" stroke="#fffdf8" stroke-width="3" />
        <text x="566" y="298" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">6</text>
        <circle cx="694" cy="178" r="17" fill="#3a2230" stroke="#fffdf8" stroke-width="3" />
        <text x="694" y="184" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">7</text>
        <rect x="758" y="248" width="28" height="28" rx="5" transform="rotate(45 772 262)" fill="#c08a4a" stroke="#fffdf8" stroke-width="3" />
        <text x="772" y="268" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">8</text>
        <circle cx="800" cy="355" r="17" fill="#3a2230" stroke="#fffdf8" stroke-width="3" />
        <text x="800" y="361" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">9</text>
        <circle cx="868" cy="140" r="17" fill="#c4553a" stroke="#fffdf8" stroke-width="3" />
        <text x="868" y="146" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">10</text>
        <circle cx="228" cy="452" r="27" fill="none" stroke="#ffd98a" stroke-width="4" />
        """;

    private const string SterMapSvg = """
        <path d="M110,366 L207,345 L282,323 L346,280 L397,323 L477,266 L572,236 L673,217 L749,181 L849,111" fill="none" stroke="#f3e2a8" stroke-width="3" opacity=".75" />
        <circle cx="110" cy="366" r="26" fill="#f3e2a8" opacity=".18" />
        <circle cx="110" cy="366" r="17" fill="#4a7a64" stroke="#f3e2a8" stroke-width="3" />
        <text x="110" y="372" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">1</text>
        <circle cx="207" cy="345" r="26" fill="#f3e2a8" opacity=".18" />
        <circle cx="207" cy="345" r="17" fill="#24305a" stroke="#f3e2a8" stroke-width="3" />
        <text x="207" y="351" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">2</text>
        <rect x="268" y="309" width="28" height="28" rx="5" transform="rotate(45 282 323)" fill="#b7a06a" stroke="#f3e2a8" stroke-width="3" />
        <text x="282" y="329" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">3</text>
        <circle cx="346" cy="280" r="26" fill="#f3e2a8" opacity=".18" />
        <circle cx="346" cy="280" r="17" fill="#24305a" stroke="#f3e2a8" stroke-width="3" />
        <text x="346" y="286" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">4</text>
        <rect x="383" y="309" width="28" height="28" rx="5" transform="rotate(45 397 323)" fill="#b7a06a" stroke="#f3e2a8" stroke-width="3" />
        <text x="397" y="329" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">5</text>
        <circle cx="477" cy="266" r="26" fill="#f3e2a8" opacity=".18" />
        <circle cx="477" cy="266" r="17" fill="#4a6a8c" stroke="#f3e2a8" stroke-width="3" />
        <text x="477" y="272" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">6</text>
        <circle cx="572" cy="236" r="26" fill="#f3e2a8" opacity=".18" />
        <circle cx="572" cy="236" r="17" fill="#24305a" stroke="#f3e2a8" stroke-width="3" />
        <text x="572" y="242" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">7</text>
        <rect x="659" y="203" width="28" height="28" rx="5" transform="rotate(45 673 217)" fill="#b7a06a" stroke="#f3e2a8" stroke-width="3" />
        <text x="673" y="223" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">8</text>
        <circle cx="749" cy="181" r="26" fill="#f3e2a8" opacity=".18" />
        <circle cx="749" cy="181" r="17" fill="#24305a" stroke="#f3e2a8" stroke-width="3" />
        <text x="749" y="187" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">9</text>
        <circle cx="849" cy="111" r="26" fill="#f3e2a8" opacity=".18" />
        <circle cx="849" cy="111" r="17" fill="#c7802f" stroke="#f3e2a8" stroke-width="3" />
        <text x="849" y="117" text-anchor="middle" font-family="Lexend, sans-serif" font-weight="700" font-size="17" fill="#fff">10</text>
        <circle cx="110" cy="366" r="27" fill="none" stroke="#ffd98a" stroke-width="4" />
        """;

    public static bool SupportsThemedRoute(string? journeyKey)
        => string.Equals(journeyKey, PupilJourneyTheme.ZeeExpeditie, StringComparison.OrdinalIgnoreCase)
           || string.Equals(journeyKey, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase)
           || string.Equals(journeyKey, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase);

    public static PupilRouteThemeView? Find(string? journeyKey)
    {
        if (string.Equals(journeyKey, PupilJourneyTheme.ZeeExpeditie, StringComparison.OrdinalIgnoreCase))
        {
            return new PupilRouteThemeView(
                PupilJourneyTheme.ZeeExpeditie,
                "Leerling.Route.Lead",
                "Leerling.Route.ToReef",
                "oceaan",
                "center 45%",
                "zeekaart",
                ZeeMapSvg,
                ZeeStops);
        }

        if (string.Equals(journeyKey, PupilJourneyTheme.Eilandtuin, StringComparison.OrdinalIgnoreCase))
        {
            return new PupilRouteThemeView(
                PupilJourneyTheme.Eilandtuin,
                "Leerling.Route.Tuin.Lead",
                "Leerling.Route.Tuin.ToBloemenbaai",
                "tuin-baai",
                "center 22%",
                "tuin-kaart",
                TuinMapSvg,
                TuinStops);
        }

        if (string.Equals(journeyKey, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase))
        {
            return new PupilRouteThemeView(
                PupilJourneyTheme.Sterrenkust,
                "Leerling.Route.Ster.Lead",
                "Leerling.Route.Ster.ToDuinpad",
                "sterren-strand",
                "center 12%",
                "sterren-kaart",
                SterMapSvg,
                SterStops);
        }

        return null;
    }
}
