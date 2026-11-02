using System.Text;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

public static class PupilPuzzelSsrHtml
{
    public static string BuildBody(
        string key,
        CultureState culture,
        string antiforgery,
        string savePath,
        bool groep78,
        string? themeKey,
        IPupilQuestionSetRegistry registry,
        string? error)
    {
        var title = groep78
            ? culture[PupilJourneyTheme.PuzzleTitleKey(themeKey, key)]
            : TitleFallback(culture, key);
        var lead = groep78
            ? culture[PupilJourneyTheme.PuzzleLeadKey(themeKey, key)]
            : LeadFallback(culture, key);
        var bandWorld = PupilJourneyTheme.BandWorldKeyForProgress(null, key);
        var sb = new StringBuilder();
        var puzzleNo = PupilJourneyTheme.PuzzleOrdinal(key);
        if (groep78)
        {
            var puzzleShort = culture[PupilGroep78Journey.TitleKey(themeKey, new(key, PupilGroep78JourneyNodeKind.Puzzle))];
            var journeyMeta = string.Format(
                culture["Leerling.G78.Puzzle.JourneyMeta"],
                puzzleNo,
                puzzleShort);
            sb.Append(PupilLeerlingSsrParts.Groep78SceneHeader(
                culture, registry, themeKey, bandWorld, key, journeyMeta, true, PuzzleBandPosition(themeKey), sceneMetaIsHtml: true));
            sb.Append("""<div class="wrap ll-g78-overlap ll-g78-overlap--below-journey"><div class="q-layout">""");
        }

        sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(savePath)).Append("\" data-ll-puzzle-form=\"1\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"Outcome\" value=\"\" />");
        var kind = PuzzleKind(key);
        if (groep78)
        {
            sb.Append("""<div class="card ll-puzzle ll-puzzle-g78" data-testid="ll-puzzle-root" data-ll-puzzle-kind=""").Append(kind).Append("\">");
            sb.Append("""<div class="ll-puzzle-intro" data-ll-puzzle-phase="intro">""");
            sb.Append("<p class=\"kicker\">Puzzel ").Append(puzzleNo).Append(" van 3</p>");
            sb.Append("<h1 class=\"in-h\">").Append(PupilLeerlingSsrParts.E(title)).Append("</h1>");
            sb.Append("<p class=\"ll-puzzle-intro-sub\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Puzzle.IntroSub.Zee"])).Append("</p>");
            if (kind == "schelpenrij")
            {
                sb.Append(PupilGroep78MockHtml.PuzzlePreviewRow());
            }

            sb.Append("<ol class=\"steps\"><li><span>1</span>").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Puzzle.Step1"]));
            sb.Append("</li><li><span>2</span>").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Puzzle.Step2"]));
            sb.Append("</li><li><span>3</span>").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Puzzle.Step3"])).Append("</li></ol>");
            sb.Append("<div class=\"facts\"><span>").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Puzzle.FactNoClock"]));
            sb.Append("</span><span>").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Puzzle.FactSkip"])).Append("</span></div>");
            sb.Append("""<div class="qfoot ll-puzzle-intro-foot">""");
            sb.Append(PupilGroep78SsrParts.Groep78SpeakRow(culture["Leerling.G78.Puzzle.IntroSub.Zee"], "ll-puzzle-intro", culture, "Leerling.G78.Puzzle.ReadIntro"));
            sb.Append("""<button type="button" class="btn btn-main btn-big" data-ll-puzzle-start="1">""").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Puzzle.Start"]));
            sb.Append(PupilGroep78SsrParts.NextSvg).Append("</button></div></div>");
            sb.Append("""<div class="ll-puzzle-play" data-ll-puzzle-phase="play" hidden>""");
        }

        sb.Append(groep78 ? """<section class="ll-puzzle-play-inner" """ : """<section class="ll-card ll-puzzle" data-ll-puzzle-root="1" """);
        if (!groep78)
        {
            sb.Append(" data-ll-puzzle-kind=\"").Append(kind).Append("\" data-testid=\"ll-puzzle-root\"");
        }
        else
        {
            sb.Append(" data-ll-puzzle-play-section=\"1\"");
        }

        sb.Append('>');
        if (!groep78)
        {
            sb.Append("<h1 class=\"ll-question__text\">").Append(PupilLeerlingSsrParts.E(title)).Append("</h1>");
            sb.Append("<p class=\"ll-puzzle__lead\">").Append(PupilLeerlingSsrParts.E(lead)).Append("</p>");
        }
        else
        {
            sb.Append("<h1 class=\"qtitle\">").Append(PupilLeerlingSsrParts.E(lead)).Append("</h1>");
        }

        AppendPlayUi(sb, kind, groep78, themeKey);
        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">").Append(PupilLeerlingSsrParts.E(error)).Append("</p>");
        }

        sb.Append("""<div class="ll-puzzle__actions qfoot"><button type="button" class="btn btn-ghost" data-ll-puzzle-skip="1">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Puzzle.Skip"])).Append("</button>");
        sb.Append("""<button type="button" class="btn btn-main" data-ll-puzzle-continue="1" data-testid="ll-puzzle-continue">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Puzzle.Continue"])).Append("</button></div></section>");
        if (groep78)
        {
            sb.Append("</div></div>");
            sb.Append(PupilGroep78SsrParts.Groep78CoachAside(
                culture,
                culture["Leerling.G78.Puzzle.IntroBubble"],
                culture["Leerling.G78.Puzzle.IntroBubble"],
                "ll-puzzle-coach",
                "kreeft-kaart.png"));
            sb.Append("</div></div>");
        }

        sb.Append("</form>");
        if (groep78)
        {
            sb.Append("</div>");
        }

        return sb.ToString();
    }

    private static void AppendPlayUi(StringBuilder sb, string kind, bool groep78, string? themeKey)
    {
        if (kind == "schelpenrij")
        {
            sb.Append("<div class=\"ll-puzzle__shells answers\">");
            for (var i = 0; i < 4; i++)
            {
                sb.Append("<button type=\"button\" class=\"ll-puzzle__shell ans\" data-ll-shell-index=\"").Append(i).Append("\" data-testid=\"ll-puzzle-shell\"><span class=\"ll-puzzle__shell-icon\"></span></button>");
            }

            sb.Append("</div>");
        }
        else if (kind == "schatkaart")
        {
            var faces = groep78
                ? PupilJourneyTheme.PuzzleMatchPairIds(themeKey).ToList()
                : ["🐚", "⚓", "🐠", "🐚", "⚓", "🐠"];
            sb.Append("<div class=\"ll-puzzle__cards\">");
            for (var i = 0; i < faces.Count; i++)
            {
                sb.Append("<button type=\"button\" class=\"ll-puzzle__card\" data-ll-card-index=\"").Append(i);
                sb.Append("\" data-ll-card-face=\"").Append(PupilLeerlingSsrParts.E(faces[i])).Append("\" data-testid=\"ll-puzzle-card\"></button>");
            }

            sb.Append("</div>");
        }
        else
        {
            sb.Append("<div class=\"ll-puzzle__lamps\">");
            for (var i = 0; i < 3; i++)
            {
                sb.Append("<button type=\"button\" class=\"ll-puzzle__lamp\" data-ll-lamp-index=\"").Append(i).Append("\" data-testid=\"ll-puzzle-lamp\"></button>");
            }

            sb.Append("</div>");
        }
    }

    private static string PuzzleKind(string key) => key.ToLowerInvariant() switch
    {
        "schelpenrij" => "schelpenrij",
        "schatkaart" => "schatkaart",
        _ => "vuurtorenlampen",
    };

    private static string TitleFallback(CultureState culture, string key) => key.ToLowerInvariant() switch
    {
        "schelpenrij" => culture["Leerling.Puzzle.Schelpenrij.Title"],
        "schatkaart" => culture["Leerling.Puzzle.Schatkaart.Title"],
        _ => culture["Leerling.Puzzle.Vuurtoren.Title"],
    };

    private static string LeadFallback(CultureState culture, string key) => key.ToLowerInvariant() switch
    {
        "schelpenrij" => culture["Leerling.Puzzle.Schelpenrij.Lead"],
        "schatkaart" => culture["Leerling.Puzzle.Schatkaart.Lead"],
        _ => culture["Leerling.Puzzle.Vuurtoren.Lead"],
    };

    private static string PuzzleBandPosition(string? themeKey)
        => string.Equals(themeKey, PupilJourneyTheme.Sterrenkust, StringComparison.OrdinalIgnoreCase)
            ? "center 30%"
            : "center 82%";
}
