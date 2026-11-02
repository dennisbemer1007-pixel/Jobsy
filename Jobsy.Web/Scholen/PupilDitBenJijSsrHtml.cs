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
        if (!groep78)
        {
            return BuildLegacy(result, culture, passportV2, pdfHref);
        }

        var (bandStem, bandPos) = PupilJourneyTheme.ResultSceneBand(themeKey);
        var tileStems = PupilJourneyTheme.ResultTileStems(themeKey);
        var sb = new StringBuilder();
        sb.Append("<section class=\"band rb ll-g78-result-band\" style=\"background-image:url('/leerling/werelden/img/");
        sb.Append(bandStem).Append(".jpg');background-position:").Append(bandPos).Append(";min-height:460px\">");
        sb.Append("""<div class="wrap ll-g78-result-band__journey">""");
        sb.Append(PupilLeerlingSsrParts.Groep78JourneyStrip(
            culture, registry, themeKey, "dit-ben-jij", culture["Leerling.G78.Result.JourneyMeta"], 0, 0, sceneMetaIsHtml: true));
        sb.Append("</div></section>");
        sb.Append("""<div class="wrap rs-hdr"><div class="rs-title"><span class="pill-done">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["LeerlingStory.DonePill"]));
        sb.Append("""</span><h1 class="rs-h serif">""").Append(PupilLeerlingSsrParts.E(culture["LeerlingStory.Title"])).Append("</h1></div></div>");
        sb.Append("""<div class="wrap ll-g78-overlap ll-result--g78" data-testid="leerling-dit-ben-jij"><div class="q-layout"><div>""");
        sb.Append("""<div class="card ll-result-story"><div class="qrow">""");
        sb.Append(PupilGroep78SsrParts.Groep78SpeakRow(result.Story.Body, "ll-story", culture, "Leerling.G78.Result.ReadStory"));
        sb.Append("</div><p class=\"story-body\">").Append(PupilLeerlingSsrParts.E(result.Story.Body)).Append("</p></div>");
        sb.Append("<div class=\"rtiles\">");
        for (var i = 0; i < result.Story.Tiles.Count && i < tileStems.Count; i++)
        {
            sb.Append(PupilGroep78MockHtml.ResultTile(culture, tileStems[i], result.Story.Tiles[i]));
        }

        sb.Append("</div><div class=\"card ll-result-chips\"><h2 class=\"rsub\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Result.LikesTitle"])).Append("</h2><div class=\"chips\">");
        foreach (var likeKey in result.Likes)
        {
            var chip = PupilInterestChipCatalog.LikeChips.FirstOrDefault(c => c.Key == likeKey);
            var label = chip is null ? likeKey : culture[chip.LabelKey];
            sb.Append("<span class=\"chip on\">").Append(PupilLeerlingSsrParts.E(label)).Append("</span>");
        }

        sb.Append("</div><h2 class=\"rsub\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Result.JobsTitle"])).Append("</h2>");
        sb.Append(PupilGroep78SsrParts.Groep78SpeakRow(string.Join(", ", result.Story.JobIdeas), "ll-jobs", culture, "Leerling.G78.Result.ReadJobs"));
        sb.Append("<div class=\"chips\">");
        foreach (var job in result.Story.JobIdeas)
        {
            sb.Append("<span class=\"chip\">").Append(PupilLeerlingSsrParts.E(job)).Append("</span>");
        }

        sb.Append("</div><p class=\"note\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Result.TeacherNote"])).Append("</p></div>");
        sb.Append("""<div class="qfoot res-foot">""");
        if (passportV2)
        {
            sb.Append("""<a class="btn btn-ghost" href="/leerling/paspoort">""").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Result.Passport"])).Append("</a>");
        }

        sb.Append("<a class=\"btn btn-ghost\" href=\"").Append(PupilLeerlingSsrParts.E(pdfHref)).Append("\" data-enhance-nav=\"false\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["LeerlingStory.Pdf"])).Append("</a>");
        sb.Append("""<a class="btn btn-main" href="/leerling/droombaan">""").Append(PupilLeerlingSsrParts.E(culture["LeerlingStory.CheckDream"]));
        sb.Append(PupilGroep78SsrParts.NextSvg).Append("</a></div></div>");
        sb.Append(PupilGroep78SsrParts.Groep78CoachAside(
            culture,
            culture["Leerling.G78.Result.Coach"],
            culture["Leerling.G78.Result.Coach"],
            "ll-result-coach",
            "kreeft-kijker.png"));
        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static string BuildLegacy(PupilResultPageDto result, CultureState culture, bool passportV2, string pdfHref)
    {
        var sb = new StringBuilder();
        sb.Append(PupilLeerlingSsrParts.CoachCue(culture["Leerling.Coach.Story"]));
        sb.Append("""<div class="ll-reis ll-result" data-testid="leerling-dit-ben-jij">""");
        sb.Append("""<section class="ll-card"><p class="ll-question__meta"><span class="ll-pill">""");
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
