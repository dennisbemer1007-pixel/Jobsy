using System.Globalization;
using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;

namespace Jobsy.Web.Applications;

internal static class AfterMatchEmployerRescheduleSsrShells
{
    internal sealed record BuildOptions(
        string InitialPanel,
        AfterMatchEmployerPanelSsrShells.PageData? ProposeData,
        AfterMatchEmployerCancelSsrShells.PageData? CancelData);

    internal static string BuildBody(
        CultureState culture,
        AfterMatchEmployerRescheduleSsrData.PageData data,
        Guid applicationId,
        BuildOptions? options = null)
    {
        options ??= new BuildOptions("", null, null);
        var panel = string.IsNullOrWhiteSpace(options.InitialPanel) ? "reschedule" : options.InitialPanel.Trim().ToLowerInvariant();
        if (panel is "slots")
        {
            panel = "propose";
        }

        var sb = new StringBuilder();
        var thread = data.Thread;
        sb.Append("<div class=\"ndm ndm-wg ndm-wg-page\" data-testid=\"ndm-werkgever-live\" data-ndm-panel=\"");
        sb.Append(WebUtility.HtmlEncode(panel));
        sb.Append("\" data-ndm-employer-ssr=\"1\" data-ndm-employer-reschedule-ssr=\"1\" data-ndm-employer-cancel-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\" data-back-href=\"");
        sb.Append(WebUtility.HtmlEncode($"/werkgever/sollicitaties/{applicationId:D}/after-match/reschedule-ssr"));
        sb.Append("\" data-ndm-title-home=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen09.Hero.Title"]));
        sb.Append("\" data-ndm-lead-home=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen09.Hero.Lead"]));
        sb.Append("\" data-ndm-title-propose=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen03.Hero.Title"]));
        sb.Append("\" data-ndm-lead-propose=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen03.Hero.Lead"]));
        sb.Append("\" data-enhance-nav=\"false\"><div data-ndm-wg-flow><div class=\"g2 ndm-wg-g2\"><div class=\"wc stack ndm-wg-main\">");
        AppendCandidateHeader(sb, culture, thread);
        sb.Append("<div class=\"hr\"></div>");
        AppendTimeline(sb, culture, data);
        sb.Append("<div class=\"hr\"></div>");
        AppendWarning(sb, culture, data);
        AppendActions(sb, culture, data);
        AppendFootnote(sb, culture);
        sb.Append("</div>");
        AppendWeekAside(sb, culture, data);

        if (options.ProposeData is not null)
        {
            AfterMatchEmployerPanelSsrShells.AppendProposePanel(sb, culture, options.ProposeData);
        }

        if (options.CancelData is not null)
        {
            sb.Append("<aside class=\"wc side pop stack\" data-ndm-cancel-panel");
            if (panel is not "cancel")
            {
                sb.Append(" hidden");
            }

            sb.Append('>');
            AfterMatchEmployerCancelSsrShells.AppendCancelFormOnly(sb, culture, options.CancelData);
            sb.Append("</aside>");
        }

        sb.Append("</div></div></div>");
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

    private static void AppendTimeline(StringBuilder sb, CultureState culture, AfterMatchEmployerRescheduleSsrData.PageData data)
    {
        sb.Append("<nav class=\"tl\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("\"><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Yes"]));
        if (!string.IsNullOrWhiteSpace(data.YesStepHint))
        {
            sb.Append("<small>");
            sb.Append(WebUtility.HtmlEncode(data.YesStepHint));
            sb.Append("</small>");
        }

        sb.Append("</div><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(data.PlanStepHint ?? culture["AfterMatch.Timeline.PlanNow"]));
        sb.Append("</small></div><div class=\"s now\"><i>");
        sb.Append(NdMIcons.Handshake);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Interview"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(data.InterviewStepHint ?? ""));
        sb.Append("</small></div><div class=\"s\"><i>");
        sb.Append(NdMIcons.Flag);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Outcome"]));
        sb.Append("<small></small></div></nav>");
    }

    private static void AppendWarning(
        StringBuilder sb,
        CultureState culture,
        AfterMatchEmployerRescheduleSsrData.PageData data)
    {
        var first = AfterMatchEmployerEmployerPresentation.CandidateFirstName(data.Thread);
        sb.Append("<div class=\"warnin\" style=\"background:#fbf1e4;border-color:#f0d3b4;color:#6b3a17\">");
        sb.Append(NdMIcons.Calendar);
        sb.Append("<div><b>");
        sb.Append(WebUtility.HtmlEncode(string.Format(culture["AfterMatch.Employer.Reschedule.WarningTitleFmt"], first)));
        sb.Append("</b><br>");
        sb.Append(data.WarningBodyHtml);
        sb.Append("</div></div>");
    }

    private static void AppendActions(StringBuilder sb, CultureState culture, AfterMatchEmployerRescheduleSsrData.PageData data)
    {
        sb.Append("<div class=\"row wrapflex\" style=\"margin-top:14px\">");
        if (data.CandidateProposal is { } slot)
        {
            sb.Append("<button type=\"button\" class=\"btn\" data-ndm-employer-accept-slot data-slot-id=\"");
            sb.Append(slot.Id.ToString("D"));
            sb.Append("\">");
            sb.Append(WebUtility.HtmlEncode(data.AcceptLabel));
            sb.Append("</button>");
        }

        sb.Append("<button type=\"button\" class=\"btn line\" data-ndm-open-panel=\"propose\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Reschedule.Counter"]));
        sb.Append("</button><span class=\"sp\"></span><button type=\"button\" class=\"lk small\" data-ndm-open-panel=\"cancel\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Reschedule.CancelLink"]));
        sb.Append("</button></div>");
    }

    private static void AppendFootnote(StringBuilder sb, CultureState culture)
    {
        sb.Append("<p class=\"xs muted\" style=\"margin-top:10px\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Reschedule.Footnote"]));
        sb.Append("</p>");
    }

    private static void AppendWeekAside(StringBuilder sb, CultureState culture, AfterMatchEmployerRescheduleSsrData.PageData data)
    {
        var thread = data.Thread;
        sb.Append("<aside class=\"wc side\" data-ndm-wg-aside><h2 style=\"font-size:1.15rem\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Reschedule.WeekAside"]));
        sb.Append("</h2><div class=\"list\" style=\"margin-top:10px\"><div><span class=\"ico navy\">");
        sb.Append(NdMIcons.Calendar);
        sb.Append("</span><div class=\"sp\"><b class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(thread.CandidateName));
        sb.Append("</b><div class=\"xs muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Reschedule.RequestLabel"]));
        sb.Append("</div></div><span class=\"pill warm\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Reschedule.ActionPill"]));
        sb.Append("</span></div><div><span class=\"ico navy\">");
        sb.Append(NdMIcons.Calendar);
        sb.Append("</span><div class=\"sp\"><b class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Reschedule.WeekRow4790Name"]));
        sb.Append("</b><div class=\"xs muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Reschedule.WeekRow4790When"]));
        sb.Append("</div></div><span class=\"pill ok\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Employer.Reschedule.WeekRow4790Status"]));
        sb.Append("</span></div></div></aside>");
    }

    private static string CandidateSubtitle(CultureState culture, AfterMatchThreadDto thread)
    {
        var yesDate = thread.AcceptedAtUtc is DateTime accepted
            ? AfterMatchMailSsrTime.ToAmsterdam(accepted).ToString("d MMMM", CultureInfo.GetCultureInfo("nl-NL"))
            : null;
        var yesPart = yesDate is not null
            ? string.Format(culture["AfterMatch.Employer.YesOn"], yesDate)
            : null;
        return string.Join(
            " · ",
            new[] { thread.VacancyTitle, culture["AfterMatch.Demo.Team"], yesPart }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
