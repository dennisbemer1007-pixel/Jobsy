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
            var islandMeta = string.Format(culture["Leerling.G78.Island.JourneyMeta"], 1);
            sb.Append(PupilLeerlingSsrParts.Groep78SceneHeader(
                culture, registry, themeKey, "pauze-eiland", "pauze-eiland",
                islandMeta, true, "center 45%", sceneMetaIsHtml: true));
            sb.Append("""<div class="wrap ll-g78-overlap ll-g78-overlap--below-journey ll-g78-island-wrap"><div class="q-layout"><div class="ll-g78-q-main">""");
        }
        else
        {
            sb.Append("""<div class="ll-reis ll-island">""");
        }

        sb.Append(groep78 ? """<section class="card ll-island-card card-deco">""" : """<section class="ll-card ll-island-card">""");
        sb.Append("<form method=\"post\" action=\"").Append(PupilLeerlingSsrParts.E(savePath)).Append("\" data-ll-chips-form=\"1\" data-testid=\"ll-island-form\">");
        sb.Append(antiforgery);
        sb.Append("<input type=\"hidden\" name=\"Likes\" value=\"").Append(PupilLeerlingSsrParts.E(string.Join(",", state.Likes))).Append("\" />");
        sb.Append("<input type=\"hidden\" name=\"Dislikes\" value=\"").Append(PupilLeerlingSsrParts.E(string.Join(",", state.Dislikes))).Append("\" />");
        if (groep78)
        {
            AppendGroep78IslandSteps(sb, state, culture, error);
        }
        else
        {
            AppendLegacyIsland(sb, state, culture, error);
        }

        sb.Append("</form></section></div>");
        if (groep78)
        {
            sb.Append(PupilGroep78SsrParts.Groep78CoachAside(
                culture,
                culture["Leerling.G78.Island.CoachLikes"],
                culture["Leerling.G78.Island.CoachLikes"],
                "ll-island-coach-likes",
                "kreeft-welkom.png"));
            sb.Append("</div></div>");
        }
        else
        {
            sb.Append("</div>");
        }

        return sb.ToString();
    }

    private static void AppendGroep78IslandSteps(StringBuilder sb, PupilProgressStateDto state, CultureState culture, string? error)
    {
        var likeSet = new HashSet<string>(state.Likes, StringComparer.OrdinalIgnoreCase);
        var dislikeSet = new HashSet<string>(state.Dislikes, StringComparer.OrdinalIgnoreCase);
        sb.Append("""<div class="ll-island-step" data-ll-island-step="1">""");
        sb.Append("<div class=\"qmeta\"><span class=\"kicker\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.World.Island.Title"]));
        sb.Append("</span><span aria-hidden=\"true\">·</span><span>").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.MetaProgress"])).Append("</span></div>");
        sb.Append("<span class=\"step-pill\">").Append(PupilLeerlingSsrParts.E(string.Format(culture["Leerling.G78.Island.Step"], 1))).Append("</span>");
        sb.Append("<p class=\"island-sub\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.LikesTitle"])).Append("</p>");
        sb.Append("<h1 class=\"island-lead\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.LikesLead"])).Append("</h1>");
        sb.Append(PupilGroep78SsrParts.Groep78SpeakRow(culture["Leerling.G78.Island.LikesLead"], "ll-island-likes", culture, "Leerling.G78.ReadQuestion"));
        sb.Append("""<div class="pick-chips" role="group" aria-label="Leuk">""");
        foreach (var chip in PupilInterestChipCatalog.LikeChips)
        {
            sb.Append(PupilGroep78MockHtml.PchipButton(culture, chip, likeSet.Contains(chip.Key), dislike: false));
        }

        sb.Append("""<button type="button" class="pchip other" data-ll-other-toggle="like"><span class="ico">""").Append(PupilGroep78ChipIcons.Svg("__other__"));
        sb.Append("</span>").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.Other"])).Append("</button>");
        sb.Append("</div>");
        sb.Append("""<div class="qfoot"><button type="button" class="btn btn-ghost" disabled>""").Append(PupilGroep78SsrParts.BackSvg);
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.Prev"])).Append("</button>");
        sb.Append("""<button type="button" class="btn btn-main" data-ll-island-next="1">""").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.Done"]));
        sb.Append(PupilGroep78SsrParts.NextSvg).Append("</button></div></div>");

        sb.Append("""<div class="ll-island-step" data-ll-island-step="2" hidden>""");
        sb.Append("<div class=\"qmeta\"><span class=\"kicker\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.World.Island.Title"]));
        sb.Append("</span><span aria-hidden=\"true\">·</span><span>").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.MetaProgress"])).Append("</span></div>");
        sb.Append("<span class=\"step-pill\">").Append(PupilLeerlingSsrParts.E(string.Format(culture["Leerling.G78.Island.Step"], 2))).Append("</span>");
        sb.Append("<p class=\"island-sub\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.DislikesTitle"])).Append("</p>");
        sb.Append("<h1 class=\"island-lead\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.DislikesLead"])).Append("</h1>");
        sb.Append(PupilGroep78SsrParts.Groep78SpeakRow(culture["Leerling.G78.Island.DislikesLead"], "ll-island-dislikes", culture, "Leerling.G78.ReadQuestion"));
        sb.Append("""<div class="pick-chips" role="group" aria-label="Niet leuk">""");
        foreach (var chip in PupilInterestChipCatalog.DislikeChips)
        {
            sb.Append(PupilGroep78MockHtml.PchipButton(culture, chip, dislikeSet.Contains(chip.Key), dislike: true));
        }

        sb.Append("""<button type="button" class="pchip other" data-ll-other-toggle="dislike"><span class="ico">""").Append(PupilGroep78ChipIcons.Svg("__other__"));
        sb.Append("</span>").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.Other"])).Append("</button></div>");
        sb.Append("<label class=\"field ll-other\" data-ll-other-panel=\"dislike\"><span class=\"island-sub\">");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.OtherLabel"])).Append("</span><input maxlength=\"24\" name=\"DislikeOther\" value=\"");
        sb.Append(PupilLeerlingSsrParts.E(state.DislikeOtherWord)).Append("\" placeholder=\"");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.OtherPlaceholder"])).Append("\" /></label>");
        sb.Append("<p class=\"warn\"><b>").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.OtherWarn"])).Append("</b> ");
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.OtherWarnBody"])).Append("</p>");
        if (!string.IsNullOrEmpty(error))
        {
            sb.Append("<p class=\"ll-error\" role=\"alert\">").Append(PupilLeerlingSsrParts.E(error)).Append("</p>");
        }

        sb.Append("""<div class="qfoot"><button type="button" class="btn btn-ghost" data-ll-island-prev="1">""").Append(PupilGroep78SsrParts.BackSvg);
        sb.Append(PupilLeerlingSsrParts.E(culture["Leerling.Reis.Prev"])).Append("</button>");
        sb.Append("<button type=\"submit\" class=\"btn btn-main\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.G78.Island.Done"]));
        sb.Append(PupilGroep78SsrParts.NextSvg).Append("</button></div></div>");
    }

    private static void AppendLegacyIsland(StringBuilder sb, PupilProgressStateDto state, CultureState culture, string? error)
    {
        var pct = state.TotalItems <= 0 ? 0 : 100.0 * state.AnsweredCount / state.TotalItems;
        sb.Append("<div class=\"ll-progress-bar\"><span class=\"ll-progress-bar__fill\" style=\"width:");
        sb.Append(pct.ToString("0.#", CultureInfo.InvariantCulture)).Append("%\"></span></div>");
        sb.Append("<h1 class=\"ll-question__text\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Island.Lead"])).Append("</h1>");
        sb.Append(PupilLeerlingSsrParts.CoachCue(culture["Leerling.Coach.Island"]));
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
        sb.Append("<button type=\"submit\" class=\"btn-primary\">").Append(PupilLeerlingSsrParts.E(culture["Leerling.Island.Continue"])).Append("</button></div>");
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
