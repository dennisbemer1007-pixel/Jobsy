using System.Net;
using System.Text;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Applications;

internal static class AfterMatchCandidateCancelledSsrShells
{
    internal static string BuildBody(CultureState culture, AfterMatchCandidateCancelledSsrData.PageData data, Guid applicationId)
    {
        var sb = new StringBuilder();
        KreeftHeaderStripHtml.Append(sb, KreeftHeaderStripAssets.NuNietDesk, KreeftHeaderStripAssets.NuNietMob);
        sb.Append("<div class=\"ndm ndm-page ndm-cancelled-page\" data-testid=\"ndm-candidate-cancelled\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\" data-enhance-nav=\"false\"><main class=\"wrap\"><section class=\"intro\"><nav class=\"bc\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.Breadcrumb"]));
        sb.Append("\"><a href=\"");
        sb.Append(WebUtility.HtmlEncode(data.BackHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.ApplicationsTab"]));
        sb.Append("</a></nav><h1>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.InterviewCancelled.EmployerTitle"]));
        sb.Append("</h1><p class=\"state-message\" role=\"status\">");
        sb.Append(WebUtility.HtmlEncode(
            data.CancelledByEmployer
                ? string.Format(culture["AfterMatch.InterviewCancelled.CandidateNoteEmployerFmt"], data.Reason)
                : string.Format(culture["AfterMatch.InterviewCancelled.CandidateNoteSelfFmt"], data.Reason)));
        sb.Append("</p><p style=\"margin-top:14px\"><a class=\"btn btn-primary\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.BackHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Apps.Ndm.BackAllApplications"]));
        sb.Append("</a></p></section></main></div>");
        return sb.ToString();
    }
}
