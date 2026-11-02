using System.Text;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

public static class PupilGroep78RouteSsrHtml
{
    public static string BuildBody(
        PupilQuestionSetDef def,
        CultureState culture,
        string? themeKey)
    {
        themeKey = PupilJourneyTheme.NormalizeOrDefault(themeKey);
        var sb = new StringBuilder();
        var bandStem = themeKey switch
        {
            PupilJourneyTheme.Eilandtuin => "tuin-baai",
            PupilJourneyTheme.Sterrenkust => "sterren-strand",
            _ => "oceaan",
        };
        var bandPos = themeKey switch
        {
            PupilJourneyTheme.Eilandtuin => "center 22%",
            PupilJourneyTheme.Sterrenkust => "center 12%",
            _ => "center 45%",
        };
        var subKey = themeKey switch
        {
            PupilJourneyTheme.Eilandtuin => "Leerling.G78.Route.Sub.Tuin",
            PupilJourneyTheme.Sterrenkust => "Leerling.G78.Route.Sub.Ster",
            _ => "Leerling.G78.Route.Sub.Zee",
        };
        var mapImg = themeKey switch
        {
            PupilJourneyTheme.Eilandtuin => "tuin-kaart.jpg",
            PupilJourneyTheme.Sterrenkust => "sterren-kaart.jpg",
            _ => "zeekaart.jpg",
        };
        var firstWorld = def.Worlds.Count > 0 ? def.Worlds[0].WorldKey : "koraalrif";
        var firstStrip = culture[PupilJourneyTheme.StripTitleKey(themeKey, new PupilGroep78JourneyNode(firstWorld, PupilGroep78JourneyNodeKind.World))];
        var toFirst = string.Format(
            culture[themeKey == PupilJourneyTheme.Eilandtuin ? "Leerling.G78.Route.ToFirstDe" : "Leerling.G78.Route.ToFirst"],
            firstStrip);

        sb.Append("<section class=\"band kb\" style=\"background-image:url('/leerling/werelden/img/").Append(bandStem);
        sb.Append(".jpg');background-position:").Append(bandPos).Append(";min-height:230px\">");
        sb.Append("""<div class="wrap kb-hero"><h1 class="rk-h serif">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Route.Title"]));
        sb.Append("</h1><p class=\"rk-sub\">").Append(PupilLeerlingSsrParts.E(culture[subKey])).Append("</p></div></section>");
        sb.Append("""<div class="wrap rk-overlap"><div class="card rk">""");
        sb.Append("""<div class="rk-grid"><div class="rk-map-wrap"><div class="rk-map">""");
        sb.Append("<img src=\"/leerling/werelden/img/").Append(mapImg).Append("\" alt=\"\" />");
        sb.Append(RouteOverlaySvg(themeKey));
        sb.Append("</div>");
        sb.Append("""<div class="d-only rk-lob"><div class="ring" aria-hidden="true"></div><div class="bubble tail-l"><span class="who">Lobsy</span>""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Route.Bubble"]));
        sb.Append("""<div class="bt">""");
        sb.Append(PupilGroep78SsrParts.Groep78SpeakButton(
            culture["Leerling.G78.Route.Bubble"],
            "ll-route-coach",
            culture));
        sb.Append("<span class=\"speak-label\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Wereld.ReadAloud"])).Append("</span></div></div></div></div>");
        sb.Append("<div class=\"rk-list\"><ol class=\"rk-stops\">");
        sb.Append(RouteListItems(def, culture, themeKey));
        sb.Append("</ol>");
        sb.Append("""<a class="btn btn-main rk-go" href="/leerling/reis">""").Append(PupilLeerlingSsrParts.E(toFirst));
        sb.Append(PupilGroep78SsrParts.NextSvg).Append("</a>");
        sb.Append("""<a class="btn btn-quiet" href="/leerling/wereld">""").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Route.ChangeWorld"])).Append("</a>");
        sb.Append("</div></div></div>");
        sb.Append("""<div class="m-only wrap rk-mobile-coach"><div class="guide"><div class="ring" aria-hidden="true"></div><div class="bubble tail-l"><span class="who">Lobsy</span>""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Route.Bubble"]));
        sb.Append("""<div class="bt">""");
        sb.Append(PupilGroep78SsrParts.Groep78SpeakButton(
            culture["Leerling.G78.Route.Bubble"],
            "ll-route-coach",
            culture));
        sb.Append("<span class=\"speak-label\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Wereld.ReadAloud"])).Append("</span></div></div></div></div>");
        return sb.ToString();
    }

    private static string RoutePuzzleTitle(CultureState culture, string puzzleKey, string? themeKey)
        => puzzleKey.ToLowerInvariant() switch
        {
            "vuurtorenlampen" => culture["Leerling.G78.Route.Puzzle.Vuurtorenlampen"],
            _ => culture[PupilGroep78Journey.TitleKey(themeKey, new(puzzleKey, PupilGroep78JourneyNodeKind.Puzzle))],
        };

    private static string RouteListItems(PupilQuestionSetDef def, CultureState culture, string? themeKey)
    {
        var sb = new StringBuilder();
        var labels = BuildRouteLabels(def, culture, themeKey);
        for (var i = 0; i < labels.Count; i++)
        {
            var (title, sub, kind) = labels[i];
            var here = i == 0
                ? $""" <span class="rk-here">{PupilLeerlingSsrParts.E(culture["Leerling.G78.Route.Here"])}</span>"""
                : "";
            sb.Append("<li><span class=\"rk-dot rk-dot--").Append(kind).Append("\">").Append(i + 1).Append("</span><span><b>");
            sb.Append(PupilLeerlingSsrParts.E(title)).Append("</b>").Append(here).Append("<br><span class=\"rk-subline\">");
            sb.Append(PupilLeerlingSsrParts.E(sub)).Append("</span></span></li>");
        }

        return sb.ToString();
    }

    private static List<(string Title, string Sub, string Kind)> BuildRouteLabels(
        PupilQuestionSetDef def,
        CultureState culture,
        string? themeKey)
    {
        var list = new List<(string, string, string)>();
        var startKey = themeKey switch
        {
            PupilJourneyTheme.Eilandtuin => "Leerling.G78.Route.Start.Tuin",
            PupilJourneyTheme.Sterrenkust => "Leerling.G78.Route.Start.Ster",
            _ => "Leerling.G78.Route.Start.Zee",
        };
        list.Add((culture[startKey], culture["Leerling.G78.Route.Start"], "s"));
        var puzzleNo = 0;
        var cumulative = 0;
        var worldNo = 0;
        foreach (var world in def.Worlds)
        {
            worldNo++;
            var fromQ = cumulative + 1;
            cumulative += world.ItemCount;
            var sub = string.Format(culture["Leerling.G78.Route.WorldQuestions"], fromQ, cumulative);
            list.Add((
                culture[PupilJourneyTheme.StripTitleKey(themeKey, new PupilGroep78JourneyNode(world.WorldKey, PupilGroep78JourneyNodeKind.World))],
                sub,
                "w"));
            foreach (var slot in def.PuzzleSlots.Where(s => s.AfterItem == cumulative))
            {
                puzzleNo++;
                list.Add((
                    RoutePuzzleTitle(culture, slot.PuzzleKey, themeKey),
                    string.Format(culture["Leerling.G78.Route.PuzzleN"], puzzleNo),
                    "p"));
            }

            if (cumulative == def.IslandAfter)
            {
                list.Add((
                    culture[PupilJourneyTheme.StripTitleKey(themeKey, new PupilGroep78JourneyNode("pauze-eiland", PupilGroep78JourneyNodeKind.Island))],
                    culture["Leerling.G78.Route.IslandSub"],
                    "i"));
            }
        }

        list.Add((
            culture["LeerlingStory.Title"],
            culture["Leerling.G78.Route.FinalSub"],
            "f"));
        return list;
    }

    private static string RouteOverlaySvg(string themeKey)
    {
        themeKey = PupilJourneyTheme.NormalizeOrDefault(themeKey);
        var pins = themeKey switch
        {
            PupilJourneyTheme.Eilandtuin => TuinPins,
            PupilJourneyTheme.Sterrenkust => SterPins,
            _ => ZeePins,
        };
        var sb = new StringBuilder();
        sb.Append("""<svg viewBox="0 0 1024 576" class="rk-overlay" aria-hidden="true">""");
        AppendRoutePaths(sb, themeKey, pins);
        AppendRouteMarkers(sb, themeKey, pins);
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static void AppendRoutePaths(StringBuilder sb, string themeKey, (int X, int Y)[] pins)
    {
        if (themeKey == PupilJourneyTheme.Sterrenkust)
        {
            sb.Append("<path d=\"M");
            for (var i = 0; i < pins.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(pins[i].X).Append(',').Append(pins[i].Y);
            }

            sb.Append("\" fill=\"none\" stroke=\"#f3e2a8\" stroke-width=\"3\" opacity=\".75\"/>");
            return;
        }

        var path = themeKey == PupilJourneyTheme.Eilandtuin ? TuinPath : ZeePath;
        var lineColor = themeKey == PupilJourneyTheme.Eilandtuin ? "#b5452c" : "#d4532f";
        sb.Append("<path d=\"").Append(path).Append("\" fill=\"none\" stroke=\"#fffdf8\" stroke-width=\"9\" stroke-linecap=\"round\" opacity=\".85\"/>");
        sb.Append("<path d=\"").Append(path).Append("\" fill=\"none\" stroke=\"").Append(lineColor).Append("\" stroke-width=\"4.5\" stroke-dasharray=\"2 11\" stroke-linecap=\"round\"/>");
    }

    private static void AppendRouteMarkers(StringBuilder sb, string themeKey, (int X, int Y)[] pins)
    {
        var isSter = themeKey == PupilJourneyTheme.Sterrenkust;
        var stroke = isSter ? "#f3e2a8" : "#fffdf8";
        var cols = isSter
            ? new Dictionary<char, string> { ['w'] = "#24305a", ['p'] = "#b7a06a", ['i'] = "#4a6a8c", ['s'] = "#4a7a64", ['f'] = "#c7802f" }
            : themeKey == PupilJourneyTheme.Eilandtuin
                ? new Dictionary<char, string> { ['w'] = "#3a2230", ['p'] = "#c08a4a", ['i'] = "#4d7a5c", ['s'] = "#4d7a5c", ['f'] = "#c4553a" }
                : new Dictionary<char, string> { ['w'] = "#10284a", ['p'] = "#b98f3e", ['i'] = "#0f5b66", ['s'] = "#2f7d5b", ['f'] = "#d4532f" };

        for (var i = 0; i < pins.Length && i < RouteStopTypes.Length; i++)
        {
            var (x, y) = pins[i];
            var t = RouteStopTypes[i];
            var col = cols[t];
            if (isSter && t != 'p')
            {
                sb.Append("<circle cx=\"").Append(x).Append("\" cy=\"").Append(y).Append("\" r=\"26\" fill=\"#f3e2a8\" opacity=\".18\"/>");
            }

            if (t == 'p')
            {
                sb.Append("<rect x=\"").Append(x - 14).Append("\" y=\"").Append(y - 14).Append("\" width=\"28\" height=\"28\" rx=\"5\" transform=\"rotate(45 ")
                    .Append(x).Append(' ').Append(y).Append("\" fill=\"").Append(col).Append("\" stroke=\"").Append(stroke).Append("\" stroke-width=\"3\"/>");
                sb.Append("<text x=\"").Append(x).Append("\" y=\"").Append(y + 6).Append("\" text-anchor=\"middle\" font-family=\"Lexend\" font-weight=\"700\" font-size=\"17\" fill=\"#fff\" transform=\"rotate(-45 ")
                    .Append(x).Append(' ').Append(y).Append("\">").Append(i + 1).Append("</text>");
            }
            else
            {
                sb.Append("<circle cx=\"").Append(x).Append("\" cy=\"").Append(y).Append("\" r=\"17\" fill=\"").Append(col).Append("\" stroke=\"").Append(stroke).Append("\" stroke-width=\"3\"/>");
                sb.Append("<text x=\"").Append(x).Append("\" y=\"").Append(y + 6).Append("\" text-anchor=\"middle\" font-family=\"Lexend\" font-weight=\"700\" font-size=\"17\" fill=\"#fff\">").Append(i + 1).Append("</text>");
            }
        }

        var (x0, y0) = pins[0];
        sb.Append("<circle cx=\"").Append(x0).Append("\" cy=\"").Append(y0).Append("\" r=\"27\" fill=\"none\" stroke=\"#ffd98a\" stroke-width=\"4\"/>");
    }

    private static readonly char[] RouteStopTypes = ['s', 'w', 'p', 'w', 'p', 'i', 'w', 'p', 'w', 'f'];

    private static readonly (int X, int Y)[] ZeePins =
    [
        (160, 430), (300, 345), (352, 262), (425, 222), (505, 300), (578, 262), (718, 192), (770, 262), (812, 345), (855, 126),
    ];

    private static readonly (int X, int Y)[] TuinPins =
    [
        (228, 452), (350, 388), (318, 292), (392, 196), (482, 252), (566, 292), (694, 178), (772, 262), (800, 355), (868, 140),
    ];

    private static readonly (int X, int Y)[] SterPins =
    [
        (110, 366), (207, 345), (282, 323), (346, 280), (397, 323), (477, 266), (572, 236), (673, 217), (749, 181), (849, 111),
    ];

    private const string ZeePath =
        "M160,430 C220,400 260,370 300,345 S330,280 352,262 S400,225 425,222 S480,270 505,300 S560,270 578,262 S680,200 718,192 S760,235 770,262 S800,320 812,345 S900,300 905,230 S880,150 855,126";

    private const string TuinPath =
        "M228,452 C280,430 320,410 350,388 S320,320 318,292 S360,210 392,196 S460,230 482,252 S540,290 566,292 S660,200 694,178 S760,235 772,262 S790,330 800,355 S910,300 915,230 S890,160 868,140";
}
