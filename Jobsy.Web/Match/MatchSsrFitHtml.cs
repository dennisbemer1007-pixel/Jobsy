using System.Net;
using System.Text;
using Jobsy.Core.Rules;
using Jobsy.Core.Rules.KandidaatBanen;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Models;

namespace Jobsy.Web.Match;

internal static class MatchSsrFitHtml
{
    private const string SparkleSvg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 16 16\" fill=\"currentColor\" focusable=\"false\">" +
        "<path d=\"M8 1.2l1.1 3.4L12.5 6 9.1 7.4 8 10.8 6.9 7.4 3.5 6l3.4-1.4L8 1.2zm4.8 7.2l.6 1.8 1.8.6-1.8.6-.6 1.8-.6-1.8-1.8-.6 1.8-.6.6-1.8z\"/>" +
        "</svg>";

    internal static void AppendOverlay(StringBuilder sb, CultureState culture, SwipeViewModel model)
    {
        if (string.Equals(model.FitGate, CandidateFitApply.FitGateClosed, StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("<div class=\"swipe-card__match-hero swipe-card__fit-overlay\"><a class=\"kb-fit kb-fit--gate\" href=\"");
            sb.Append(KbRoutes.PaspoortTests);
            sb.Append("\" data-enhance-nav=\"false\"><span class=\"kb-fit__icon\" aria-hidden=\"true\">");
            sb.Append(SparkleSvg);
            sb.Append("</span><span class=\"kb-fit__label\">");
            sb.Append(WebUtility.HtmlEncode(culture["Kb.Fit.Gate"]));
            sb.Append("</span></a></div>");
            return;
        }

        if (!Enum.TryParse<KbFitBand>(model.FitBand, ignoreCase: true, out var band))
        {
            return;
        }

        sb.Append("<div class=\"swipe-card__match-hero swipe-card__fit-overlay\"><span class=\"kb-fit kb-fit--");
        sb.Append(KbLabels.FitBandCss(band));
        sb.Append("\"><span class=\"kb-fit__icon\" aria-hidden=\"true\">");
        sb.Append(SparkleSvg);
        sb.Append("</span><span class=\"kb-fit__label\">");
        sb.Append(WebUtility.HtmlEncode(KbLabels.FitBand(culture, band)));
        sb.Append("</span></span></div>");
    }
}
