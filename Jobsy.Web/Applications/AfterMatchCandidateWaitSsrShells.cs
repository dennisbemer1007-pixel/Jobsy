using System.Net;
using System.Text;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Applications;

internal static class AfterMatchCandidateWaitSsrShells
{
    internal static string BuildBody(CultureState culture, AfterMatchCandidateWaitSsrData.PageData data, Guid applicationId)
    {
        var sb = new StringBuilder();

        sb.Append("<div class=\"ndm ndm-page ndm-wait-page\" data-testid=\"ndm-candidate-live\" data-ndm-view=\"wait\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\"><main class=\"wrap\">");

        var thread = data.Thread;
        sb.Append("<section class=\"intro\"><nav class=\"bc\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.Breadcrumb"]));
        sb.Append("\"><a href=\"");
        sb.Append(WebUtility.HtmlEncode(data.SollicitatiesHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.ApplicationsTab"]));
        sb.Append("</a><i>›</i><span>");
        sb.Append(WebUtility.HtmlEncode(thread.CompanyName));
        sb.Append("</span></nav><h1>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Candidate.ProposalPending.Title"]));
        sb.Append("</h1><p>");
        sb.Append(WebUtility.HtmlEncode(data.HeroLead));
        sb.Append("</p></section>");

        sb.Append("<div class=\"g2\"><div class=\"card stack\">");
        sb.Append("<div class=\"co\"><span class=\"lg\">");
        sb.Append(WebUtility.HtmlEncode(CompanyInitials(thread.CompanyName)));
        sb.Append("</span><div><b>");
        sb.Append(WebUtility.HtmlEncode(thread.VacancyTitle));
        sb.Append("</b><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(thread.CompanyName));
        sb.Append("</p></div></div>");

        AppendTimeline(sb, culture, data.YesDateLabel, data.PlanHint);

        sb.Append("<div class=\"row wrapflex\" style=\"margin-top:16px\"><span class=\"ico\">");
        sb.Append(NdMIcons.Calendar);
        sb.Append("</span><div class=\"sp\"><b style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Candidate.ProposalPending.Title"]));
        sb.Append("</b><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Candidate.ProposalPending.Body"]));
        sb.Append("</p></div></div>");

        sb.Append("<a class=\"btn line\" style=\"margin-top:14px\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.MessagesHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen10.Hero.Title"]));
        sb.Append("</a></div></div></main>");

        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendTimeline(StringBuilder sb, CultureState culture, string yesDate, string planHint)
    {
        sb.Append("<nav class=\"tl\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("\"><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Yes"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(yesDate));
        sb.Append("</small></div><div class=\"s now\"><i>");
        sb.Append(NdMIcons.Calendar);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(planHint));
        sb.Append("</small></div><div class=\"s\"><i>");
        sb.Append(NdMIcons.Handshake);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Interview"]));
        sb.Append("<small></small></div><div class=\"s\"><i>");
        sb.Append(NdMIcons.Flag);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Outcome"]));
        sb.Append("<small></small></div></nav>");
    }

    private static string CompanyInitials(string company)
    {
        var parts = company.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
            : company.Length >= 2 ? company[..2].ToUpperInvariant() : company.ToUpperInvariant();
    }
}
