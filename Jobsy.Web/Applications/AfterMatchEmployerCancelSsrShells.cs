using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerCancelSsrShells
{
    internal sealed record PageData(
        AfterMatchThreadDto Thread,
        string BackHref,
        string ApplicationDetailHref);

    internal static string BuildBody(CultureState culture, PageData data, Guid applicationId)
    {
        var sb = new StringBuilder();
        var thread = data.Thread;
        sb.Append("<div class=\"ndm ndm-wg\" data-testid=\"ndm-werkgever-live\" data-ndm-panel=\"cancel\" data-ndm-employer-ssr=\"1\" data-ndm-employer-cancel-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\" data-back-href=\"");
        sb.Append(WebUtility.HtmlEncode(data.BackHref));
        sb.Append("\"><div class=\"g2\"><div class=\"wc stack\">");
        AppendCandidateHeader(sb, culture, thread);
        sb.Append("<div class=\"hr\"></div>");
        AppendTimeline(sb, culture, thread);
        sb.Append("<div class=\"hr\"></div>");
        AppendCancelForm(sb, culture, data);
        sb.Append("</div>");
        AppendAside(sb, culture, thread);
        sb.Append("</div><p style=\"margin-top:16px\"><a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.ApplicationDetailHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["WgApp.Back"]));
        sb.Append("</a></p></div>");
        return sb.ToString();
    }

    private static void AppendCandidateHeader(StringBuilder sb, CultureState culture, AfterMatchThreadDto thread)
    {
        sb.Append("<div class=\"row\"><span class=\"av w\" style=\"width:46px;height:46px\">");
        sb.Append(NdMIcons.User);
        sb.Append("</span><div><h3 style=\"font-size:1.25rem\">");
        sb.Append(WebUtility.HtmlEncode(thread.CandidateName));
        sb.Append("</h3><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(CandidateSubtitle(culture, thread)));
        sb.Append("</p></div></div>");
    }

    private static void AppendTimeline(StringBuilder sb, CultureState culture, AfterMatchThreadDto thread)
    {
        var yesDate = thread.AcceptedAtUtc?.ToLocalTime().ToString(
            "d MMM",
            CultureInfo.GetCultureInfo("nl-NL"));
        var interviewDate = thread.ScheduledStartUtc?.ToLocalTime().ToString(
            "d MMM",
            CultureInfo.GetCultureInfo("nl-NL"));
        sb.Append("<nav class=\"tl\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("\"><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Yes"]));
        if (!string.IsNullOrWhiteSpace(yesDate))
        {
            sb.Append("<small>");
            sb.Append(WebUtility.HtmlEncode(yesDate));
            sb.Append("</small>");
        }

        sb.Append("</div><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.PlanNow"]));
        sb.Append("</small></div><div class=\"s now\"><i>");
        sb.Append(NdMIcons.Handshake);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Interview"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(interviewDate ?? ""));
        sb.Append("</small></div><div class=\"s\"><i>");
        sb.Append(NdMIcons.Flag);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Outcome"]));
        sb.Append("<small></small></div></nav>");
    }

    private static void AppendCancelForm(StringBuilder sb, CultureState culture, PageData data)
    {
        sb.Append("<div class=\"note warm\"><span>");
        sb.Append(NdMIcons.Info);
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Cancel.Lead"]));
        sb.Append("</span></div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.ReasonLong"]));
        sb.Append("</span><div class=\"row wrapflex\" style=\"gap:6px\">");
        AppendReasonChip(sb, culture["AfterMatch.Screen08.ReasonChip.Sick"]);
        AppendReasonChip(sb, culture["AfterMatch.Screen08.ReasonChip.Family"]);
        AppendReasonChip(sb, culture["AfterMatch.Screen08.ReasonChip.Transport"]);
        AppendReasonChip(sb, culture["AfterMatch.Screen08.ReasonChip.OtherAppt"]);
        AppendReasonChip(sb, culture["AfterMatch.Screen08.ReasonChip.Other"]);
        sb.Append("</div><textarea class=\"inp area\" style=\"width:100%;margin-top:8px\" rows=\"3\" data-ndm-cancel-reason placeholder=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.ReasonPlaceholder"]));
        sb.Append("\"></textarea><div class=\"row wrapflex\" style=\"margin-top:14px\"><button type=\"button\" class=\"btn\" data-ndm-employer-cancel-submit>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.CancelConfirm"]));
        sb.Append("</button><a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.BackHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.NeverMind"]));
        sb.Append("</a></div><p class=\"xs muted\" style=\"margin-top:10px\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Reschedule.Footnote"]));
        sb.Append("</p>");
    }

    private static void AppendAside(StringBuilder sb, CultureState culture, AfterMatchThreadDto thread)
    {
        sb.Append("<aside class=\"wc\"><h2 style=\"font-size:1.15rem\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.SideTitle.Cancel"]));
        sb.Append("</h2><p class=\"small muted\" style=\"margin-top:8px\">");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Screen08.SideLead"], thread.CandidateName)));
        sb.Append("</p></aside>");
    }

    private static void AppendReasonChip(StringBuilder sb, string label)
    {
        sb.Append("<button type=\"button\" class=\"chip\" data-ndm-reason-chip=\"");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</button>");
    }

    private static string CandidateSubtitle(CultureState culture, AfterMatchThreadDto thread)
    {
        var yesDate = thread.AcceptedAtUtc?.ToLocalTime().ToString(
            "d MMMM",
            CultureInfo.GetCultureInfo("nl-NL"));
        var yesPart = yesDate is not null
            ? string.Format(culture["AfterMatch.Employer.YesOn"], yesDate)
            : null;
        return string.Join(
            " · ",
            new[] { thread.VacancyTitle, culture["AfterMatch.Demo.Team"], yesPart }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
