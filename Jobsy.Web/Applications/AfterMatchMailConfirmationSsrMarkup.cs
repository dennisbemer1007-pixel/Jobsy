using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Applications;

internal static class AfterMatchMailConfirmationSsrMarkup
{
    internal static void AppendGrid(
        StringBuilder sb,
        CultureState culture,
        AfterMatchThreadDto thread,
        AfterMatchMailConfirmationFields fields,
        AfterMatchBevestigingSsrData.PageData links)
    {
        var firstName = AfterMatchMailSsrFormatting.CandidateFirstName(thread);
        var employerSubject = string.Format(culture["AfterMatch.Mail.EmployerSubject"], thread.CandidateName);
        var candidateTitle = string.Format(culture["AfterMatch.Mail.CandidateTitle"], firstName);
        var outlookEventTitle = string.Format(culture["AfterMatch.Mail.OutlookEventTitle"], thread.CompanyName);
        var calendarBtn = culture["AfterMatch.Screen05.CalendarRow"];
        var logoHtml =
            $"""<img src="{WebUtility.HtmlEncode(BrandImages.HorizontalSvg)}" alt="" style="height:28px" decoding="async" draggable="false">""";

        sb.Append("<div class=\"mgrid\"><div>");
        sb.Append("<span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.LabelCandidate"]));
        sb.Append("</span><div class=\"mclient\"><div class=\"mhead\"><b>Lobsy</b> &lt;gesprek@lobsy.nl&gt;<br><span class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.CandidateSubject"]));
        sb.Append("</span></div><div class=\"mail\"><div class=\"mtop\">");
        sb.Append(logoHtml);
        sb.Append("<span class=\"xs muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.PlannedTag"]));
        sb.Append("</span></div><h2>");
        sb.Append(WebUtility.HtmlEncode(candidateTitle));
        sb.Append("</h2><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.CandidateLead"]));
        sb.Append("</p><div class=\"mbox\">");
        AppendMboxRow(sb, culture["AfterMatch.Mail.When"], fields.WhenLabel);
        AppendMboxRow(sb, culture["AfterMatch.Mail.Where"], fields.CandidateWhere);
        AppendMboxRow(sb, culture["AfterMatch.Mail.With"], fields.WithWhomLabel);
        if (!string.IsNullOrWhiteSpace(fields.TravelLabel))
        {
            AppendMboxRow(sb, culture["AfterMatch.Mail.Travel"], fields.TravelLabel);
        }

        sb.Append("</div>");
        AppendCalendarButtons(sb, calendarBtn);
        sb.Append("<div class=\"mfoot\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.CandidateMfootLead"]));
        sb.Append(" <a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(links.RescheduleHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.CandidateMfootReschedule"]));
        sb.Append("</a> ");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.CandidateMfootMid"]));
        sb.Append(" · <a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(links.TipsHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.CandidateMfootTips"]));
        sb.Append("</a><br>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.MfootBrand"]));
        sb.Append("</div></div></div>");

        sb.Append("<span class=\"lbl\" style=\"margin-top:18px\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.After.Title"]));
        sb.Append("</span>");
        AppendWhatHappensNext(sb, culture);
        sb.Append("</div><div>");

        sb.Append("<span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.LabelEmployer"]));
        sb.Append("</span><div class=\"mclient\"><div class=\"mhead\"><b>Lobsy</b> &lt;gesprek@lobsy.nl&gt;<br><span class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(employerSubject));
        sb.Append("</span></div><div class=\"mail\"><div class=\"mtop\">");
        sb.Append(logoHtml);
        sb.Append("<span class=\"xs muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.PlannedTag"]));
        sb.Append("</span></div><h2>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.EmployerTitle"]));
        sb.Append("</h2><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.EmployerLead"]));
        sb.Append("</p><div class=\"mbox\">");
        AppendMboxRow(sb, culture["AfterMatch.Mail.When"], fields.WhenLabel);
        AppendMboxRow(
            sb,
            culture["AfterMatch.Mail.Candidate"],
            $"{thread.CandidateName}, {thread.VacancyTitle}");
        AppendMboxRow(sb, culture["AfterMatch.Mail.Where"], fields.EmployerWhere);
        AppendMboxRow(sb, culture["AfterMatch.Mail.After"], culture["AfterMatch.Mail.AfterBody"]);
        sb.Append("</div>");
        AppendCalendarButtons(sb, calendarBtn);
        sb.Append("<div class=\"mfoot\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.EmployerMfootLead"]));
        sb.Append(" · <a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(links.OpenInterviewHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.EmployerMfootLink"]));
        sb.Append("</a><br>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.MfootBrand"]));
        sb.Append("</div></div></div>");

        sb.Append("<span class=\"lbl\" style=\"margin-top:18px\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.OutlookTitle"]));
        sb.Append("</span><div class=\"olk\"><div class=\"bar\"><span>Outlook</span><span style=\"opacity:.8\">Agenda</span></div><div class=\"tb\"><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.OutlookSave"]));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.OutlookDelete"]));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.OutlookBusy"]));
        sb.Append("</span></div><div class=\"bd\"><div class=\"t\">");
        sb.Append(WebUtility.HtmlEncode(outlookEventTitle));
        sb.Append("</div><div class=\"f\"><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.OutlookStart"]));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(fields.OutlookStart));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.OutlookEnd"]));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(fields.OutlookEnd));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.OutlookLocation"]));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(fields.OutlookLocation));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.OutlookReminder"]));
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.OutlookReminderVal"]));
        sb.Append("</span></div><div style=\"margin-top:10px;border-top:1px solid #e1e5ea;padding-top:10px\">");
        sb.Append(WebUtility.HtmlEncode(fields.OutlookBodyPrimary));
        sb.Append("<br>");
        sb.Append(WebUtility.HtmlEncode(fields.OutlookRescheduleLine));
        sb.Append("</div></div></div></div></div>");
    }

    private static void AppendCalendarButtons(StringBuilder sb, string calendarBtn)
    {
        sb.Append("<div class=\"addc\"><span class=\"btn sm\">");
        sb.Append(WebUtility.HtmlEncode(calendarBtn));
        sb.Append("</span><span class=\"btn line sm\">Outlook</span><span class=\"btn line sm\">Google</span><span class=\"btn line sm\">Apple (.ics)</span></div>");
    }

    private static void AppendMboxRow(StringBuilder sb, string label, string value)
    {
        sb.Append("<div class=\"r\"><span>");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</span><b>");
        sb.Append(WebUtility.HtmlEncode(value));
        sb.Append("</b></div>");
    }

    private static void AppendWhatHappensNext(StringBuilder sb, CultureState culture)
    {
        sb.Append("<div class=\"card\"><div class=\"list\">");
        AppendWhatNextRow(sb, NdMIcons.Mail, culture["AfterMatch.WhatNext.ReminderDay"], culture["AfterMatch.WhatNext.ReminderDayBody"]);
        AppendWhatNextRow(sb, NdMIcons.Bell, culture["AfterMatch.WhatNext.Reminder2h"], culture["AfterMatch.WhatNext.Reminder2hBody"]);
        AppendWhatNextRow(sb, NdMIcons.Pin, culture["AfterMatch.WhatNext.Interview"], culture["AfterMatch.WhatNext.InterviewBody"]);
        AppendWhatNextRow(sb, NdMIcons.Flag, culture["AfterMatch.WhatNext.Outcome"], culture["AfterMatch.WhatNext.OutcomeBody"]);
        sb.Append("</div><div class=\"note\" style=\"margin-top:14px\"><span>");
        sb.Append(NdMIcons.Shield);
        sb.Append("</span><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.PrivacyNote"]));
        sb.Append("</span></div></div>");
    }

    private static void AppendWhatNextRow(StringBuilder sb, string icon, string title, string body)
    {
        sb.Append("<div><span class=\"ico\">");
        sb.Append(icon);
        sb.Append("</span><div class=\"sp\"><b class=\"small\" style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</b><div class=\"xs muted\">");
        sb.Append(WebUtility.HtmlEncode(body));
        sb.Append("</div></div></div>");
    }
}
