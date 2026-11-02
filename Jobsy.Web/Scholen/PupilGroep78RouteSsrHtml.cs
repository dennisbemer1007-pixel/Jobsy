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
        var firstTitle = culture[PupilJourneyTheme.WorldTitleKey(themeKey, firstWorld)];
        var toFirst = string.Format(
            culture[themeKey == PupilJourneyTheme.Eilandtuin ? "Leerling.G78.Route.ToFirstDe" : "Leerling.G78.Route.ToFirst"],
            firstTitle);

        sb.Append("<section class=\"band kb\" style=\"background-image:url('/leerling/werelden/img/").Append(bandStem);
        sb.Append(".jpg');background-position:").Append(bandPos).Append(";min-height:230px\" aria-hidden=\"true\"></section>");
        sb.Append("""<div class="wrap kb-head"><h1 class="rk-h serif">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Route.Title"]));
        sb.Append("</h1><p class=\"rk-sub\">").Append(PupilLeerlingSsrParts.E(culture[subKey])).Append("</p></div>");
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
        sb.Append("</div></div></div>");
        return sb.ToString();
    }

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
                culture[PupilJourneyTheme.WorldTitleKey(themeKey, world.WorldKey)],
                sub,
                "w"));
            foreach (var slot in def.PuzzleSlots.Where(s => s.AfterItem == cumulative))
            {
                puzzleNo++;
                list.Add((
                    culture[PupilGroep78Journey.TitleKey(themeKey, new(slot.PuzzleKey, PupilGroep78JourneyNodeKind.Puzzle))],
                    string.Format(culture["Leerling.G78.Route.PuzzleN"], puzzleNo),
                    "p"));
            }

            if (cumulative == def.IslandAfter)
            {
                list.Add((
                    culture[PupilJourneyTheme.WorldTitleKey(themeKey, "pauze-eiland")],
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

    private static string RouteOverlaySvg(string themeKey) => themeKey switch
    {
        PupilJourneyTheme.Sterrenkust => SterOverlay,
        PupilJourneyTheme.Eilandtuin => TuinOverlay,
        _ => ZeeOverlay,
    };

    // Approved mockup overlays (zee/tuin/ster) from docs/mockups/leerling/werelden/gen.py
    private const string ZeeOverlay =
        """<svg viewBox="0 0 1024 576" class="rk-overlay" aria-hidden="true"><path d="M160,430 C220,400 260,370 300,345 S330,280 352,262 S400,225 425,222 S480,270 505,300 S560,270 578,262 S680,200 718,192 S760,235 770,262 S800,320 812,345 S900,300 905,230 S880,150 855,126" fill="none" stroke="#fffdf8" stroke-width="9" stroke-linecap="round" opacity=".85"/><path d="M160,430 C220,400 260,370 300,345 S330,280 352,262 S400,225 425,222 S480,270 505,300 S560,270 578,262 S680,200 718,192 S760,235 770,262 S800,320 812,345 S900,300 905,230 S880,150 855,126" fill="none" stroke="#d4532f" stroke-width="4.5" stroke-dasharray="2 11" stroke-linecap="round"/></svg>""";

    private const string TuinOverlay =
        """<svg viewBox="0 0 1024 576" class="rk-overlay" aria-hidden="true"><path d="M228,452 C280,430 320,410 350,388 S320,320 318,292 S360,210 392,196 S460,230 482,252 S540,290 566,292 S660,200 694,178 S760,235 772,262 S790,330 800,355 S910,300 915,230 S890,160 868,140" fill="none" stroke="#fffdf8" stroke-width="9" stroke-linecap="round" opacity=".85"/><path d="M228,452 C280,430 320,410 350,388 S320,320 318,292 S360,210 392,196 S460,230 482,252 S540,290 566,292 S660,200 694,178 S760,235 772,262 S790,330 800,355 S910,300 915,230 S890,160 868,140" fill="none" stroke="#b5452c" stroke-width="4.5" stroke-dasharray="2 11" stroke-linecap="round"/></svg>""";

    private const string SterOverlay =
        """<svg viewBox="0 0 1024 576" class="rk-overlay" aria-hidden="true"><path d="M110,366 L207,345 L282,323 L346,280 L397,323 L477,266 L572,236 L673,217 L749,181 L849,111" fill="none" stroke="#f3e2a8" stroke-width="3" opacity=".75"/></svg>""";
}
