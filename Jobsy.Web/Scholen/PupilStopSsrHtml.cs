using System.Text;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

public static class PupilStopSsrHtml
{
    public static string BuildBody(
        CultureState culture,
        string? done,
        string antiforgeryField,
        bool groep78 = false,
        string? themeKey = null,
        IPupilQuestionSetRegistry? registry = null)
    {
        if (!groep78 || registry is null)
        {
            return BuildLegacyBody(culture, done, antiforgeryField);
        }

        var paused = string.Equals(done, "1", StringComparison.Ordinal)
                     || string.Equals(done, "deel1", StringComparison.Ordinal);
        var (bandStem, bandPos) = PupilJourneyTheme.ResultSceneBand(themeKey);
        var sb = new StringBuilder();
        sb.Append("""<section class="ll-g78-scene ll-g78-result-scene" data-testid="leerling-g78-scene">""");
        sb.Append("<section class=\"band ll-g78-world-band ll-g78-result-band\" style=\"background-image:url('/leerling/werelden/img/");
        sb.Append(bandStem).Append(".jpg');background-position:").Append(bandPos).Append("\" aria-hidden=\"true\"></section>");
        sb.Append("""<div class="ll-g78-scene__journey ll-g78-result-band__journey">""");
        sb.Append(PupilLeerlingSsrParts.Groep78JourneyStrip(
            culture,
            registry,
            themeKey,
            paused ? "dit-ben-jij" : "pauze-eiland",
            paused ? culture["Leerling.G78.Result.JourneyMeta"] : culture["Leerling.Pause"],
            0,
            0,
            sceneMetaIsHtml: paused));
        sb.Append("</div></section>");
        sb.Append("""<div class="wrap ll-g78-overlap ll-g78-overlap--below-journey ll-stop--g78" data-testid="leerling-stop-pause"><div class="q-layout">""");
        sb.Append("""<section class="card ll-stop-card">""");
        if (paused)
        {
            sb.Append("<h1 class=\"island-lead qtitle\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Stop.Title"])).Append("</h1>");
            var body = string.Equals(done, "deel1", StringComparison.Ordinal)
                ? culture["Leerling.Vo.Stop.Part1Body"]
                : culture["Leerling.Stop.Body"];
            sb.Append(PupilGroep78SsrParts.Groep78SpeakRow(
                SpokenText.Join(culture["Leerling.Stop.Title"], body),
                "ll-stop",
                culture,
                "Leerling.G78.ReadQuestion"));
            sb.Append("<p class=\"ll-stop-lead\">").Append(PupilLeerlingSsrParts.E(body)).Append("</p>");
            sb.Append("""<div class="qfoot"><a class="btn btn-main" href="/leerling">""");
            sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Stop.Again"])).Append("</a></div>");
        }
        else
        {
            sb.Append("<h1 class=\"island-lead qtitle\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Stop.AskTitle"])).Append("</h1>");
            sb.Append(PupilGroep78SsrParts.Groep78SpeakRow(
                SpokenText.Join(culture["Leerling.Stop.AskTitle"], culture["Leerling.Stop.AskBody"]),
                "ll-stop-ask",
                culture,
                "Leerling.G78.ReadQuestion"));
            sb.Append("<p class=\"ll-stop-lead\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Stop.AskBody"])).Append("</p>");
            sb.Append("<div class=\"qfoot\">").Append(PupilLeerlingSsrFrame.PauseForm(antiforgeryField, culture["Leerling.Pause"], groep78: true, journeyThemeKey: themeKey)).Append("</div>");
        }

        sb.Append("</section>");
        sb.Append(PupilGroep78SsrParts.Groep78CoachAside(
            culture,
            culture["Leerling.Coach.Stop"],
            culture["Leerling.Coach.Stop"],
            "ll-stop-coach",
            "kreeft-welkom.png",
            mobileGuideFirst: true));
        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static string BuildLegacyBody(CultureState culture, string? done, string antiforgeryField)
    {
        var paused = string.Equals(done, "1", StringComparison.Ordinal)
            || string.Equals(done, "deel1", StringComparison.Ordinal);
        var sb = new StringBuilder();
        sb.Append(PupilLeerlingSsrParts.CoachCue(culture["Leerling.Coach.Stop"]));
        sb.Append("""<section class="ll-card ll-stop">""");
        if (paused)
        {
            sb.Append("<h1>").Append(PupilLeerlingSsrParts.E(culture["Leerling.Stop.Title"])).Append("</h1>");
            var body = string.Equals(done, "deel1", StringComparison.Ordinal)
                ? culture["Leerling.Vo.Stop.Part1Body"]
                : culture["Leerling.Stop.Body"];
            sb.Append(PupilLeerlingSsrParts.SpeakButton(
                SpokenText.Join(culture["Leerling.Stop.Title"], body), "ll-stop", culture));
            sb.Append("<p>").Append(PupilLeerlingSsrParts.E(body)).Append("</p>");
            sb.Append("""<a class="btn-primary" href="/leerling">""");
            sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Stop.Again"])).Append("</a>");
        }
        else
        {
            sb.Append("<h1>").Append(PupilLeerlingSsrParts.E(culture["Leerling.Stop.AskTitle"])).Append("</h1>");
            sb.Append(PupilLeerlingSsrParts.SpeakButton(
                SpokenText.Join(culture["Leerling.Stop.AskTitle"], culture["Leerling.Stop.AskBody"]),
                "ll-stop-ask", culture));
            sb.Append("<p>").Append(PupilLeerlingSsrParts.E(culture["Leerling.Stop.AskBody"])).Append("</p>");
            sb.Append(PupilLeerlingSsrFrame.PauseForm(antiforgeryField, culture["Leerling.Pause"]));
        }

        sb.Append("</section>");
        return sb.ToString();
    }
}
