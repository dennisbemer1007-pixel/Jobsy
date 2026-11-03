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
        var kind = PuzzleKind(key);
        var introSubKey = PupilJourneyTheme.PuzzleIntroSubKey(themeKey, key);
        var introSub = culture[introSubKey];
        var coachKey = PupilJourneyTheme.PuzzleIntroCoachKey(key);
        var coachText = culture[coachKey];

        if (groep78)
        {
            var puzzleShort = culture[PupilGroep78Journey.TitleKey(themeKey, new(key, PupilGroep78JourneyNodeKind.Puzzle))];
            var journeyMeta = string.Format(
                culture["Leerling.G78.Puzzle.JourneyMeta"],
                puzzleNo,
                puzzleShort,
                PupilJourneyTheme.PuzzleGateQuestion(key));
            sb.Append(PupilLeerlingSsrParts.Groep78SceneHeader(
                culture, registry, themeKey, bandWorld, key, journeyMeta, true, PuzzleBandPosition(themeKey), sceneMetaIsHtml: true));
            sb.Append("""<div class="wrap ll-g78-overlap ll-g78-overlap--below-journey"><div class="q-layout"><div class="ll-g78-q-main">""");
        }

        sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(savePath)).Append("\" data-ll-puzzle-form=\"1\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"Outcome\" value=\"\" />");

        if (groep78)
        {
            sb.Append("<div class=\"card ll-puzzle ll-puzzle-g78\" data-testid=\"ll-puzzle-root\" data-ll-puzzle-kind=\"")
                .Append(kind)
                .Append("\">");
            if (kind == "schelpenrij")
            {
                sb.Length -= 1;
                sb.Append(" data-ll-puzzle-mode=\"pattern\" data-ll-puzzle-answer=\"")
                    .Append(PupilJourneyTheme.SchelpenrijPatternAnswer())
                    .Append("\">");
            }

            sb.Append("""<div class="ll-puzzle-intro" data-ll-puzzle-phase="intro">""");
            sb.Append("<p class=\"kicker\">Puzzel ").Append(puzzleNo).Append(" van 3</p>");
            sb.Append("<h1 class=\"in-h\">").Append(PupilLeerlingSsrParts.E(title)).Append("</h1>");
            sb.Append("<p class=\"ll-puzzle-intro-sub\">").Append(PupilLeerlingSsrParts.E(introSub)).Append("</p>");
            sb.Append(AppendIntroPreview(kind, themeKey));
            for (var step = 1; step <= 3; step++)
            {
                if (step == 1)
                {
                    sb.Append("<ol class=\"steps\"><li><span>1</span>")
                        .Append(PupilLeerlingSsrParts.E(culture[PupilJourneyTheme.PuzzleIntroStepKey(key, 1)]));
                }
                else
                {
                    sb.Append("</li><li><span>").Append(step).Append("</span>")
                        .Append(PupilLeerlingSsrParts.E(culture[PupilJourneyTheme.PuzzleIntroStepKey(key, step)]));
                }
            }

            sb.Append("</li></ol>");
            sb.Append("<div class=\"facts\"><span class=\"facts-badge\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Puzzle.FactNoClock"]));
            sb.Append("</span><span class=\"facts-badge\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Puzzle.FactSkip"])).Append("</span></div>");
            sb.Append("""<div class="qfoot ll-puzzle-intro-foot">""");
            sb.Append(PupilGroep78SsrParts.Groep78SpeakRow(introSub, "ll-puzzle-intro", culture, "Leerling.G78.Puzzle.ReadIntro"));
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
        else if (kind == "schelpenrij")
        {
            sb.Append("<h1 class=\"qtitle\">").Append(PupilLeerlingSsrParts.E(introSub)).Append("</h1>");
        }
        else
        {
            sb.Append("<h1 class=\"qtitle\">").Append(PupilLeerlingSsrParts.E(lead)).Append("</h1>");
        }

        AppendPlayUi(sb, kind, groep78, themeKey, culture);
        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">").Append(PupilLeerlingSsrParts.E(error)).Append("</p>");
        }

        sb.Append("""<div class="ll-puzzle__actions qfoot"><button type="button" class="btn btn-ghost" data-ll-puzzle-skip="1">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Puzzle.Skip"])).Append("</button>");
        sb.Append("""<button type="button" class="btn btn-main" data-ll-puzzle-continue="1" data-testid="ll-puzzle-continue" disabled aria-disabled="true">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Puzzle.Continue"])).Append("</button></div></section>");
        if (groep78)
        {
            sb.Append("</div></div></form></div>");
            sb.Append(PupilGroep78SsrParts.Groep78CoachAside(
                culture,
                coachText,
                coachText,
                "ll-puzzle-coach",
                "kreeft-kaart.png"));
            sb.Append("</div></div>");
        }
        else
        {
            sb.Append("</form>");
        }

        return sb.ToString();
    }

    private static string AppendIntroPreview(string kind, string? themeKey)
        => kind switch
        {
            "schelpenrij" => PupilGroep78MockHtml.PuzzlePreviewRow(),
            "schatkaart" => PupilGroep78MockHtml.PuzzleMatchPreviewRow(themeKey),
            _ => PupilGroep78MockHtml.PuzzleLampPreviewRow(themeKey),
        };

    private static void AppendPlayUi(StringBuilder sb, string kind, bool groep78, string? themeKey, CultureState culture)
    {
        if (groep78 && kind == "schelpenrij")
        {
            sb.Append(PupilGroep78MockHtml.PuzzlePatternPlay(culture));
            return;
        }

        if (groep78 && kind == "schatkaart")
        {
            sb.Append(PupilGroep78MockHtml.PuzzleMatchPlay(themeKey, culture));
            return;
        }

        if (groep78 && kind == "vuurtorenlampen")
        {
            sb.Append(PupilGroep78MockHtml.PuzzleLampPlay(themeKey, culture));
            return;
        }

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
            var faces = new List<string> { "🐚", "⚓", "🐠", "🐚", "⚓", "🐠" };
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
