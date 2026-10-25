using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Applications;

internal static class AfterMatchHerinneringenSsrShells
{
    internal static string BuildBody(
        CultureState culture,
        AfterMatchHerinneringenSsrData.PageData data,
        Guid applicationId,
        bool includeBackLink,
        bool editableReminders)
    {
        var thread = data.Thread;
        var prefs = thread.ReminderPreference;
        var fields = data.Fields;
        var logoHtml =
            $"""<img src="{WebUtility.HtmlEncode(BrandImages.HorizontalSvg)}" alt="" style="height:28px" decoding="async" draggable="false">""";
        var appIcon =
            $"""<img src="{WebUtility.HtmlEncode(BrandImages.AppIconSvg)}" alt="" width="34" height="34" decoding="async" draggable="false">""";

        var sb = new StringBuilder();
        sb.Append("<div class=\"ndm ndm-page ndm-mail-page\" data-testid=\"ndm-candidate-live\" data-ndm-view=\"herinneringen\" data-ndm-herinneringen-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\"><main class=\"wrap\"><p class=\"crumb\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.Crumb"]));
        sb.Append("</p><h1 style=\"font-size:1.8rem;margin-bottom:14px\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.Title"]));
        sb.Append("</h1><div class=\"mgrid\"><div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.EmailLabel"]));
        sb.Append("</span><div class=\"mclient\"><div class=\"mhead\"><b>Lobsy</b> &lt;gesprek@lobsy.nl&gt;<br><span class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(fields.EmailSubject));
        sb.Append("</span></div><div class=\"mail\"><div class=\"mtop\">");
        sb.Append(logoHtml);
        sb.Append("<span class=\"xs muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.PlannedTag"]));
        sb.Append("</span></div><h2>");
        sb.Append(WebUtility.HtmlEncode(fields.EmailTitle));
        sb.Append("</h2><p class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(fields.EmailLead));
        sb.Append("</p><div class=\"mbox\">");
        AppendMboxRow(sb, culture["AfterMatch.Mail.When"], fields.WhenLine);
        AppendMboxRow(sb, culture["AfterMatch.Mail.Where"], fields.WhereLine);
        AppendMboxRow(sb, culture["AfterMatch.Mail.Travel"], fields.TravelLine);
        if (!string.IsNullOrWhiteSpace(fields.WeatherLine))
        {
            AppendMboxRow(sb, culture["AfterMatch.Screen07.WeatherLabel"], fields.WeatherLine);
        }

        sb.Append("</div><div class=\"addc\"><a class=\"btn sm\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.RouteHref));
        sb.Append("\" target=\"_blank\" rel=\"noopener noreferrer\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.BtnRoute"]));
        sb.Append("</a><a class=\"btn line sm\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.TipsHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.BtnTips"]));
        sb.Append("</a><a class=\"btn line sm\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.PracticeHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.BtnPractice"]));
        sb.Append("</a></div><div class=\"mfoot\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.EmailMfootLead"]));
        sb.Append(" · <a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.RescheduleHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen06.CandidateMfootReschedule"]));
        sb.Append("</a></div></div></div></div><div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.PushLabel"]));
        sb.Append("</span><div class=\"phone\"><div class=\"lock\"><div class=\"ltime\">8:30</div><div class=\"ldate\">");
        sb.Append(WebUtility.HtmlEncode(fields.LockDateLabel));
        sb.Append("</div>");
        AppendPush(sb, appIcon, fields.PushTwoHoursTitle, fields.PushTwoHoursBody, "nu");
        AppendPush(sb, appIcon, fields.PushDayBeforeTitle, fields.PushDayBeforeBody, "gisteren 18:00");
        sb.Append("</div></div>");
        AppendReminderSettings(sb, culture, prefs, editableReminders);
        sb.Append("</div></div>");

        if (includeBackLink)
        {
            sb.Append("<p style=\"margin-top:16px\"><a class=\"lk\" href=\"");
            sb.Append(WebUtility.HtmlEncode(data.SollicitatiesHref));
            sb.Append("\" data-enhance-nav=\"false\">");
            sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Link.BackApplications"]));
            sb.Append("</a></p>");
        }

        sb.Append("</main>");
        AfterMatchMailSsrChrome.AppendChatDock(sb, culture, data.AssistantHref);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendMboxRow(StringBuilder sb, string label, string value)
    {
        sb.Append("<div class=\"r\"><span>");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</span><b>");
        sb.Append(WebUtility.HtmlEncode(value));
        sb.Append("</b></div>");
    }

    private static void AppendPush(StringBuilder sb, string appIcon, string title, string bodyHtml, string whenLabel)
    {
        sb.Append("<div class=\"push\">");
        sb.Append(appIcon);
        sb.Append("<div><div class=\"row\" style=\"gap:6px\"><b>Lobsy</b><small>");
        sb.Append(WebUtility.HtmlEncode(whenLabel));
        sb.Append("</small></div><p><b>");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</b><br>");
        sb.Append(WebUtility.HtmlEncode(bodyHtml));
        sb.Append("</p></div></div>");
    }

    private static void AppendReminderSettings(
        StringBuilder sb,
        CultureState culture,
        AfterMatchReminderPreferenceDto prefs,
        bool editable)
    {
        sb.Append("<div class=\"card\" style=\"margin-top:16px\" id=\"reminders\" data-ndm-reminder-settings");
        if (editable)
        {
            sb.Append(" data-ndm-reminder-editable=\"1\"");
        }

        sb.Append("><b style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.Settings.Title"]));
        sb.Append("</b><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.Settings.Lead"]));
        sb.Append("</p><div class=\"list\" style=\"margin-top:10px\" data-ndm-reminder-list>");
        AppendSwitchRow(sb, "dayBefore18", prefs.DayBefore18, culture["AfterMatch.Screen07.Pref.DayBefore"], editable);
        AppendSwitchRow(sb, "twoHoursBefore", prefs.TwoHoursBefore, culture["AfterMatch.Screen07.Pref.TwoHours"], editable);
        AppendSwitchRow(sb, "weekBefore", prefs.WeekBefore, culture["AfterMatch.Screen07.Pref.WeekBefore"], editable);
        sb.Append("</div>");
        if (editable)
        {
            sb.Append("<button type=\"button\" class=\"btn sm\" style=\"margin-top:12px\" data-ndm-reminder-save>");
            sb.Append(WebUtility.HtmlEncode(culture["Common.Save"]));
            sb.Append("</button>");
        }

        sb.Append("</div>");
    }

    private static void AppendSwitchRow(StringBuilder sb, string key, bool on, string label, bool editable)
    {
        sb.Append("<div data-ndm-reminder-row=\"");
        sb.Append(WebUtility.HtmlEncode(key));
        sb.Append("\">");
        if (editable)
        {
            sb.Append("<button type=\"button\" class=\"sw");
            if (on)
            {
                sb.Append(" on");
            }

            sb.Append("\" data-ndm-reminder=\"");
            sb.Append(WebUtility.HtmlEncode(key));
            sb.Append("\" aria-pressed=\"");
            sb.Append(on ? "true" : "false");
            sb.Append("\"></button>");
        }
        else
        {
            sb.Append("<span class=\"sw");
            if (on)
            {
                sb.Append(" on");
            }

            sb.Append("\" aria-hidden=\"true\"></span>");
        }

        sb.Append("<span class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</span></div>");
    }
}
