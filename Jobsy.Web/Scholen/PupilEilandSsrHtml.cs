using System.Globalization;
using System.Text;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;

namespace Jobsy.Web.Scholen;

public static class PupilEilandSsrHtml
{
    public static string PartBreak(CultureState culture, string antiforgery)
    {
        var sb = new StringBuilder();
        sb.Append(PupilLeerlingSsrParts.CoachCue(culture["Leerling.Coach.Part"]));
        sb.Append("""<section class="ll-card" role="status"><h1>""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Vo.PartBreak.Title"])).Append("</h1><p>");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Vo.PartBreak.Body"])).Append("</p>");
        sb.Append("""<div class="ll-question__nav"><a class="btn-primary" href="/leerling/reis">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Vo.PartBreak.Continue"])).Append("</a>");
        sb.Append(PupilLeerlingSsrFrame.PauseForm(antiforgery, culture["Leerling.Vo.PartBreak.Stop"], "1"));
        sb.Append("</div></section>");
        return sb.ToString();
    }

    public static string BuildBody(
        PupilProgressStateDto state,
        PupilQuestionSetDef? _,
        CultureState culture,
        IPupilQuestionSetRegistry registry,
        string antiforgery,
        string savePath,
        string? error,
        bool groep78,
        string? themeKey)
    {
        var sb = new StringBuilder();
        if (groep78)
        {
            sb.Append(PupilLeerlingSsrParts.Groep78SceneHeader(
                culture, registry, themeKey, "pauze-eiland", "pauze-eiland",
                culture["Leerling.Island.Lead"], true, "center 40%"));
            sb.Append("""<div class="ll-g78-island wrap ll-g78-overlap">""");
        }
        else
        {
            sb.Append("""<div class="ll-reis ll-island">""");
        }

        sb.Append(groep78 ? """<section class="card ll-island-card">""" : """<section class="ll-card ll-island-card">""");
        var pct = state.TotalItems <= 0 ? 0 : 100.0 * state.AnsweredCount / state.TotalItems;
        sb.Append("<div class=\"ll-progress-bar\"><span class=\"ll-progress-bar__fill\" style=\"width:");
        sb.Append(pct.ToString("0.#", CultureInfo.InvariantCulture)).Append("%\"></span></div>");
        sb.Append("<h1 class=\"ll-question__text\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Island.Lead"])).Append("</h1>");
        sb.Append(PupilLeerlingSsrParts.CoachCue(culture["Leerling.Coach.Island"]));
        sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(savePath)).Append("\" data-ll-chips-form=\"1\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"Likes\" value=\"").Append(PupilLeerlingSsrParts.E(string.Join(",", state.Likes))).Append("\" />");
        sb.Append("<input type=\"hidden\" name=\"Dislikes\" value=\"").Append(PupilLeerlingSsrParts.E(string.Join(",", state.Dislikes))).Append("\" />");
        AppendChipFieldset(sb, culture, culture["Leerling.Island.Likes"], PupilInterestChipCatalog.LikeChips, like: true);
        AppendChipFieldset(sb, culture, culture["Leerling.Island.Dislikes"], PupilInterestChipCatalog.DislikeChips, like: false);
        sb.Append("<label class=\"ll-other\" data-ll-other-panel=\"like\"><input maxlength=\"24\" name=\"LikeOther\" value=\"");
        sb.Append(PupilLeerlingSsrParts.E(state.LikeOtherWord)).Append("\" /></label>");
        sb.Append("<label class=\"ll-other\" data-ll-other-panel=\"dislike\"><input maxlength=\"24\" name=\"DislikeOther\" value=\"");
        sb.Append(PupilLeerlingSsrParts.E(state.DislikeOtherWord)).Append("\" /></label>");
        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">").Append(PupilLeerlingSsrParts.E(error)).Append("</p>");
        }

        sb.Append("""<div class="ll-question__nav"><a class="btn-secondary" href="/leerling/reis">""");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.Prev"])).Append("</a>");
        sb.Append("<button type=\"submit\" class=\"btn-primary\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Island.Continue"])).Append("</button></div></form></section></div>");
        return sb.ToString();
    }

    private static void AppendChipFieldset(StringBuilder sb, CultureState culture, string legend, IEnumerable<PupilInterestChipCatalog.Chip> chips, bool like)
    {
        sb.Append("<fieldset class=\"ll-chips\"><legend>").Append(PupilLeerlingSsrParts.E(legend)).Append("</legend><div class=\"ll-chips__row\">");
        foreach (var chip in chips)
        {
            var attr = like ? "data-ll-chip-like" : "data-ll-chip-dislike";
            sb.Append("<button type=\"button\" class=\"ll-chip\" ").Append(attr).Append("=\"");
            sb.Append(PupilLeerlingSsrParts.E(chip.Key)).Append("\">").Append(PupilLeerlingSsrParts.E(culture[chip.LabelKey])).Append("</button>");
        }

        sb.Append("</div></fieldset>");
    }
}
