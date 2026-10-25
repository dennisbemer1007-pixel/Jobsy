using System.Net;
using System.Text;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Applications;

internal static class AfterMatchSsrErrorShells
{
    internal static string BuildInterviewNotFound(
        CultureState culture,
        string? returnUrl,
        string pageTitleKey,
        string backLabelKey)
    {
        var backHref = ApplicationsSsrQuery.TryGetSafeApplicationsReturn(returnUrl, out var safeReturn)
            ? safeReturn
            : KbRoutes.Applications;

        var sb = new StringBuilder();
        sb.Append("<div class=\"sollicitaties-hero-strip\" aria-hidden=\"true\"><div class=\"sollicitaties-hero-strip__inner\">");
        sb.Append("<img class=\"sollicitaties-hero-strip__img desk\" src=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripDesktopWebp));
        sb.Append("\" alt=\"\" width=\"1440\" height=\"150\" loading=\"eager\" decoding=\"async\" />");
        sb.Append("<img class=\"sollicitaties-hero-strip__img mob\" src=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripMobileWebp));
        sb.Append("\" alt=\"\" width=\"390\" height=\"90\" loading=\"eager\" decoding=\"async\" />");
        sb.Append("</div></div>");
        sb.Append("<div class=\"ndm ndm-page ndm-ssr-error-page\" data-testid=\"ndm-ssr-error\"><main class=\"wrap\">");
        sb.Append("<section class=\"intro\"><nav class=\"bc\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.Breadcrumb"]));
        sb.Append("\"><a href=\"");
        sb.Append(WebUtility.HtmlEncode(KbRoutes.Applications));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.ApplicationsTab"]));
        sb.Append("</a></nav><h1>");
        sb.Append(WebUtility.HtmlEncode(culture[pageTitleKey]));
        sb.Append("</h1><p class=\"state-message\" role=\"alert\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Error.NotFound"]));
        sb.Append("</p><p style=\"margin-top:14px\"><a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(backHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture[backLabelKey]));
        sb.Append("</a></p></section></main></div>");
        return sb.ToString();
    }
}
