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
        if (groep78)
        {
            sb.Append(PupilLeerlingSsrParts.Groep78SceneHeader(
                culture, registry, themeKey, bandWorld, key, null, true, PuzzleBandPosition(themeKey)));
            sb.Append("""<div class="wrap ll-g78-overlap">""");
        }

        sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(savePath)).Append("\" data-ll-puzzle-form=\"1\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"Outcome\" value=\"\" />");
        var kind = PuzzleKind(key);
        sb.Append(groep78 ? """<section class="card ll-puzzle" """ : """<section class="ll-card ll-puzzle" """);
        sb.Append("data-ll-puzzle-root=\"1\" data-ll-puzzle-kind=\"").Append(kind).Append("\" data-testid=\"ll-puzzle-root\">");
        sb.Append("<h1 class=\"ll-question__text\">").Append(PupilLeerlingSsrParts.E(title)).Append("</h1>");
        sb.Append("<p class=\"ll-puzzle__lead\">").Append(PupilLeerlingSsrParts.E(lead)).Append("</p>");
        if (kind == "schelpenrij")
        {
            sb.Append("<div class=\"ll-puzzle__shells\">");
            for (var i = 0; i < 4; i++)
            {
                sb.Append("<button type=\"button\" class=\"ll-puzzle__shell\" data-ll-shell-index=\"").Append(i).Append("\" data-testid=\"ll-puzzle-shell\"><span class=\"ll-puzzle__shell-icon\"></span></button>");
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

        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">").Append(PupilLeerlingSsrParts.E(error)).Append("</p>");
        }

        sb.Append("""<div class="ll-puzzle__actions"><button type="button" class="btn-secondary" data-ll-puzzle-skip="1">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Puzzle.Skip"])).Append("</button>");
        sb.Append("""<button type="button" class="btn-primary" data-ll-puzzle-continue="1" data-testid="ll-puzzle-continue">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Puzzle.Continue"])).Append("</button></div></section></form>");
        if (groep78)
        {
            sb.Append("</div>");
        }

        return sb.ToString();
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
