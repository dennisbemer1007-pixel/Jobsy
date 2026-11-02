using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Scholen;

public static class PupilDitBenJijSsrHtml
{
    public static string BuildBody(
        PupilResultPageDto result,
        CultureState culture,
        bool groep78,
        string? themeKey,
        IPupilQuestionSetRegistry registry,
        bool passportV2,
        string pdfHref)
    {
        var sb = new StringBuilder();
        if (groep78)
        {
            sb.Append(PupilLeerlingSsrParts.Groep78SceneHeader(
                culture, registry, themeKey, "koraalrif", "koraalrif",
                culture["LeerlingStory.Title"], showJourney: false, backgroundPosition: "center 30%"));
            sb.Append("""<div class="wrap ll-g78-overlap ll-result--g78" data-testid="leerling-dit-ben-jij">""");
        }
        else
        {
            sb.Append(PupilLeerlingSsrParts.CoachCue(culture["Leerling.Coach.Story"]));
            sb.Append("""<div class="ll-reis ll-result" data-testid="leerling-dit-ben-jij">""");
        }

        sb.Append(groep78 ? """<section class="card">""" : """<section class="ll-card">""");
        sb.Append("<p class=\"ll-question__meta\"><span class=\"ll-pill\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["LeerlingStory.DonePill"])).Append("</span></p><h1>");
        sb.Append(PupilLeerlingSsrParts.E(culture["LeerlingStory.Title"])).Append("</h1>");
        sb.Append(PupilLeerlingSsrParts.SpeakButton(result.Story.Body, "ll-story", culture));
        sb.Append("<p class=\"ll-result__story\">").Append(PupilLeerlingSsrParts.E(result.Story.Body)).Append("</p>");
        sb.Append("<div class=\"ll-tile-grid\">");
        foreach (var tile in result.Story.Tiles)
        {
            sb.Append("<div class=\"ll-tile\"><p class=\"ll-tile__label\">").Append(PupilLeerlingSsrParts.E(tile.KidLabel));
            sb.Append("</p><p class=\"ll-tile__value\">").Append(PupilLeerlingSsrParts.E(tile.Explanation)).Append("</p></div>");
        }
        sb.Append("</div></section><div class=\"ll-result__actions\">");
        if (passportV2)
        {
            sb.Append("""<a class="btn-secondary" href="/leerling/paspoort">""");
            sb.Append(PupilLeerlingSsrParts.E(culture["LeerlingPassport.Page.Title"])).Append("</a>");
        }
        sb.Append("<a class=\"btn-secondary\" href=\"").Append(PupilLeerlingSsrParts.E(pdfHref)).Append("\" data-enhance-nav=\"false\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["LeerlingStory.Pdf"])).Append("</a>");
        sb.Append("""<a class="btn-primary" href="/leerling/droombaan">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["LeerlingStory.CheckDream"])).Append("</a></div></div>");
        return sb.ToString();
    }
}
