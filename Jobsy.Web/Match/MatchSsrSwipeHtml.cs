using System.Net;
using System.Text;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;

namespace Jobsy.Web.Match;

internal static class MatchSsrSwipeHtml
{
    internal static void AppendStack(
        StringBuilder sb,
        CultureState culture,
        IReadOnlyList<SwipeViewModel> items,
        int startIndex,
        MatchProfileGateViewModel gate)
    {
        sb.Append("<div class=\"match-page__grid-main\"><div class=\"swipe-stack\" data-match-stack>");
        for (var i = 0; i < items.Count; i++)
        {
            var active = i == startIndex;
            var hidden = i != startIndex;
            AppendCardStage(sb, culture, items[i], i, backdrop: false, active, hidden, gate);
        }

        sb.Append("</div>");
        AppendActions(sb, culture);
        sb.Append("<p class=\"match-page__swipe-hint\">");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.SwipeHint"]));
        sb.Append("</p><p class=\"match-page__keyboard-hint lobsy-dialog--match__hints\" aria-hidden=\"true\"><span>");
        sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.KeyboardLead"]));
        sb.Append("</span> <kbd>←</kbd> ");
        sb.Append(WebUtility.HtmlEncode(culture["Match.Reject"]));
        sb.Append(" · <kbd>→</kbd> / <kbd>Enter</kbd> ");
        sb.Append(WebUtility.HtmlEncode(culture["Match.Interest"]));
        sb.Append("</p></div>");
    }

    private static void AppendCardStage(
        StringBuilder sb,
        CultureState culture,
        SwipeViewModel model,
        int index,
        bool backdrop,
        bool active,
        bool hidden,
        MatchProfileGateViewModel gate)
    {
        sb.Append("<div class=\"swipe-card-stage");
        if (backdrop)
        {
            sb.Append(" swipe-card-stage--backdrop");
        }

        sb.Append("\" data-match-stage=\"");
        sb.Append(index);
        sb.Append('"');
        if (hidden)
        {
            sb.Append(" hidden");
        }

        sb.Append("><article class=\"swipe-card swipe-card--mobile");
        if (backdrop)
        {
            sb.Append(" swipe-card--backdrop");
        }
        else if (active)
        {
            sb.Append(" swipe-card--enter");
        }

        if (model.IsHiddenMode)
        {
            sb.Append(" swipe-card--hidden-mode");
        }

        sb.Append("\" data-match-card data-match-card-index=\"");
        sb.Append(index);
        sb.Append("\" data-vacancy-id=\"");
        sb.Append(model.VacancyId?.ToString("D") ?? "");
        sb.Append("\" data-testid=\"swipe-card\"><div class=\"swipe-card__photo\">");
        if (!string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            sb.Append("<img src=\"");
            sb.Append(WebUtility.HtmlEncode(model.ImageUrl));
            sb.Append("\" alt=\"\" loading=\"");
            sb.Append(backdrop ? "eager" : "lazy");
            sb.Append("\" decoding=\"async\" />");
        }
        else
        {
            sb.Append("<div class=\"swipe-card__photo-placeholder\" aria-hidden=\"true\"></div>");
        }

        if (!backdrop)
        {
            MatchSsrFitHtml.AppendOverlay(sb, culture, model);
            sb.Append("<div class=\"swipe-card__stamp swipe-card__stamp--no\" aria-hidden=\"true\">");
            sb.Append(WebUtility.HtmlEncode(culture["Match.StampNope"]));
            sb.Append("</div><div class=\"swipe-card__stamp swipe-card__stamp--yes\" aria-hidden=\"true\">");
            sb.Append(WebUtility.HtmlEncode(culture["Match.StampLike"]));
            sb.Append("</div>");
        }

        sb.Append("</div><h2 class=\"swipe-card__title\">");
        sb.Append(WebUtility.HtmlEncode(model.JobTitle));
        sb.Append("</h2><p class=\"swipe-card__company\"><span class=\"swipe-card__company-name\">");
        sb.Append(WebUtility.HtmlEncode(CompanyLine(culture, model)));
        sb.Append("</span>");
        if (!string.IsNullOrWhiteSpace(model.LocationPrimary) && !model.IsHiddenMode)
        {
            sb.Append("<span class=\"swipe-card__company-loc\"> · ");
            sb.Append(WebUtility.HtmlEncode(model.LocationPrimary));
            sb.Append("</span>");
        }

        sb.Append("</p><div class=\"swipe-card__facts\" role=\"group\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Match.KeyFacts"]));
        sb.Append("\">");
        MatchSsrTravelHtml.Append(sb, culture, model.TravelTimeMinutes, model.TransportMode, model.TravelToBureau);
        if (!string.IsNullOrWhiteSpace(model.HoursPrimary))
        {
            sb.Append("<span class=\"swipe-card__fact\"><span class=\"swipe-card__fact-icon\" aria-hidden=\"true\">");
            sb.Append("<svg viewBox=\"0 0 24 24\" width=\"14\" height=\"14\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 2\"/></svg>");
            sb.Append("</span><span>");
            sb.Append(WebUtility.HtmlEncode(model.HoursPrimary));
            sb.Append(' ');
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Match.HoursUnit"]));
            sb.Append("</span></span>");
        }

        if (!string.IsNullOrWhiteSpace(model.SalaryPrimary))
        {
            sb.Append("<span class=\"swipe-card__fact\"><span class=\"swipe-card__fact-icon\" aria-hidden=\"true\">€</span><span>");
            sb.Append(WebUtility.HtmlEncode(model.SalaryPrimary));
            sb.Append("</span></span>");
        }

        sb.Append("</div>");
        AppendRankLower(sb, culture, model);
        AppendWhyBlock(sb, culture, model, gate);
        AppendTags(sb, model);
        sb.Append("</article></div>");
    }

    private static void AppendWhyBlock(StringBuilder sb, CultureState culture, SwipeViewModel model, MatchProfileGateViewModel gate)
    {
        var dnaRows = MatchDnaRows.Build(model.FitDimensions, model.FitWhyKinds, includeMissing: false, maxWithData: 3, gate);
        if (dnaRows.Count == 0 && string.IsNullOrWhiteSpace(model.WhyYouFit))
        {
            return;
        }

        sb.Append("<div class=\"swipe-card__copy\"><div class=\"swipe-card__why\" data-testid=\"swipe-why\"><span class=\"swipe-card__why-label\">");
        sb.Append(WebUtility.HtmlEncode(culture["Match.WhyYouFit"]));
        sb.Append("</span>");
        if (dnaRows.Count > 0)
        {
            sb.Append("<ul class=\"kb-dna kb-dna--match kb-dna--match-mobile\" role=\"list\">");
            foreach (var row in dnaRows)
            {
                sb.Append("<li class=\"kb-dna__row\" data-dna=\"");
                sb.Append(WebUtility.HtmlEncode(row.Kind));
                sb.Append("\"><span class=\"kb-dna__label\">");
                sb.Append(WebUtility.HtmlEncode(culture[row.LabelKey]));
                sb.Append("</span>");
                if (row.Percent is int barPct)
                {
                    sb.Append("<span class=\"kb-dna__band\" aria-label=\"");
                    sb.Append(WebUtility.HtmlEncode(culture[row.LabelKey]));
                    sb.Append("\">");
                    sb.Append(WebUtility.HtmlEncode(CandidateMatchDisplay.FitBandLabel(culture, barPct)));
                    sb.Append("</span><span class=\"kb-dna__reason\">");
                    sb.Append(WebUtility.HtmlEncode(culture[row.WhyKey]));
                    sb.Append("</span>");
                }

                sb.Append("</li>");
            }

            sb.Append("</ul>");
        }
        else if (!string.IsNullOrWhiteSpace(model.WhyYouFit))
        {
            sb.Append("<p class=\"swipe-card__why-fallback\">");
            sb.Append(WebUtility.HtmlEncode(model.WhyYouFit));
            sb.Append("</p>");
        }

        sb.Append("</div></div>");
    }

    private static void AppendTags(StringBuilder sb, SwipeViewModel model)
    {
        if (model.Tags.Count == 0)
        {
            return;
        }

        sb.Append("<ul class=\"swipe-card__tags\">");
        foreach (var tag in model.Tags.Take(1))
        {
            sb.Append("<li>");
            sb.Append(WebUtility.HtmlEncode(tag));
            sb.Append("</li>");
        }

        sb.Append("</ul>");
    }

    private static void AppendRankLower(StringBuilder sb, CultureState culture, SwipeViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.RankLowerReason))
        {
            return;
        }

        var reason = culture[model.RankLowerReason];
        if (string.IsNullOrWhiteSpace(reason) || reason == model.RankLowerReason)
        {
            var slash = model.RankLowerReason.LastIndexOf('.');
            reason = slash >= 0 ? model.RankLowerReason[(slash + 1)..] : model.RankLowerReason;
        }

        sb.Append("<span class=\"kb-badges__chip kb-badges__chip--rank-lower\" data-testid=\"kb-rank-lower\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["Kb.Rank.Lower"], reason)));
        sb.Append("</span>");
    }

    private static void AppendActions(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div class=\"swipe-actions swipe-actions--mobile\" role=\"group\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["Match.Actions"]));
        sb.Append("\"><button type=\"button\" class=\"swipe-actions__btn swipe-actions__btn--reject\" data-match-action=\"reject\"><span class=\"swipe-actions__circle\" aria-hidden=\"true\"><svg viewBox=\"0 0 24 24\" width=\"26\" height=\"26\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.5\" stroke-linecap=\"round\"><path d=\"M7 7l10 10M17 7 7 17\"/></svg></span><span class=\"swipe-actions__label\">");
        sb.Append(WebUtility.HtmlEncode(culture["Match.Reject"]));
        sb.Append("</span></button><button type=\"button\" class=\"swipe-actions__btn swipe-actions__btn--more\" data-match-action=\"more\"><span class=\"swipe-actions__circle swipe-actions__circle--ghost\" aria-hidden=\"true\"><svg viewBox=\"0 0 24 24\" width=\"22\" height=\"22\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M4 6h10M4 12h10M4 18h7\"/><path d=\"m14 9 5 3-5 3\"/></svg></span><span class=\"swipe-actions__label\">");
        sb.Append(WebUtility.HtmlEncode(culture["Match.MoreInfo"]));
        sb.Append("</span></button><button type=\"button\" class=\"swipe-actions__btn swipe-actions__btn--interest\" data-match-action=\"interest\"><span class=\"swipe-actions__circle\" aria-hidden=\"true\"><svg viewBox=\"0 0 24 24\" width=\"26\" height=\"26\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M7 16c1.5-1 3-1.5 5-1.5s3.5.5 5 1.5\"/><path d=\"M8 10h.01M16 10h.01\"/><path d=\"M6 8c0-2.5 2.2-4.5 6-4.5s6 2 6 4.5v2.2c0 1.3-.6 2.4-1.6 3.1L12 18l-4.4-4.7C6.6 12.6 6 11.5 6 10.2V8z\"/></svg></span><span class=\"swipe-actions__label\">");
        sb.Append(WebUtility.HtmlEncode(culture["Match.Interest"]));
        sb.Append("</span></button></div>");
    }

    private static string CompanyLine(CultureState culture, SwipeViewModel model)
    {
        if (model.IsHiddenMode)
        {
            return string.Format(culture["Kb.Via.Bureau"], model.CompanyName);
        }

        return string.IsNullOrWhiteSpace(model.CompanyName)
            ? culture["Match.CompanyFallback"]
            : model.CompanyName;
    }
}
