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
        var firstName = AfterMatchMailSsrFormatting.CandidateFirstName(thread);
        var prefs = thread.ReminderPreference;

        var sb = new StringBuilder();
        sb.Append("<div class=\"ndm ndm-page\" data-testid=\"ndm-candidate-live\" data-ndm-view=\"herinneringen\" data-ndm-herinneringen-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\"><main class=\"wrap\"><p class=\"crumb\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.Crumb"]));
        sb.Append("</p><h1 style=\"font-size:1.8rem;margin-bottom:14px\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.Title"]));
        sb.Append("</h1><div class=\"mgrid\"><div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.EmailLabel"]));
        sb.Append("</span><div class=\"mclient\"><div class=\"mail ndm-mail-preview\"><div class=\"mtop\"><span class=\"ndm-mail-logo\"><img src=\"");
        sb.Append(WebUtility.HtmlEncode(BrandImages.MarkSvg));
        sb.Append("\" alt=\"\" width=\"18\" height=\"18\" decoding=\"async\" draggable=\"false\"></span><span class=\"xs muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Mail.PlannedTag"]));
        sb.Append("</span></div><p class=\"xs muted ndm-mail-from\"><b>Lobsy</b> &lt;gesprek@lobsy.nl&gt;</p><h2>Morgen is je gesprek</h2><p class=\"small\">Hoi ");
        sb.Append(WebUtility.HtmlEncode(firstName));
        sb.Append(", nog even op een rij. Je kunt het.</p></div></div></div><div><span class=\"lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.PushLabel"]));
        sb.Append("</span><div class=\"phone\"><div class=\"lock\"><div class=\"ltime\">8:30</div><div class=\"push\"><div><b>Over 2 uur je gesprek</b></div></div></div></div>");
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

        sb.Append("</main></div>");
        return sb.ToString();
    }

    private static void AppendReminderSettings(
        StringBuilder sb,
        CultureState culture,
        AfterMatchReminderPreferenceDto prefs,
        bool editable)
    {
        sb.Append("<div class=\"card\" id=\"reminders\" data-ndm-reminder-settings");
        if (editable)
        {
            sb.Append(" data-ndm-reminder-editable=\"1\"");
        }

        sb.Append("><b style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.Settings.Title"]));
        sb.Append("</b><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen07.Settings.Lead"]));
        sb.Append("</p><div class=\"list\" style=\"margin-top:10px\" data-ndm-reminder-list>");
        AppendSwitchRow(sb, "dayBefore18", prefs.DayBefore18, culture["AfterMatch.Screen07.Pref.DayBefore"]);
        AppendSwitchRow(sb, "twoHoursBefore", prefs.TwoHoursBefore, culture["AfterMatch.Screen07.Pref.TwoHours"]);
        AppendSwitchRow(sb, "weekBefore", prefs.WeekBefore, culture["AfterMatch.Screen07.Pref.WeekBefore"]);
        sb.Append("</div>");
        if (editable)
        {
            sb.Append("<button type=\"button\" class=\"btn sm\" style=\"margin-top:12px\" data-ndm-reminder-save>");
            sb.Append(WebUtility.HtmlEncode(culture["Common.Save"]));
            sb.Append("</button>");
        }

        sb.Append("</div>");
    }

    private static void AppendSwitchRow(StringBuilder sb, string key, bool on, string label)
    {
        sb.Append("<div data-ndm-reminder-row=\"");
        sb.Append(WebUtility.HtmlEncode(key));
        sb.Append("\"><button type=\"button\" class=\"sw");
        if (on)
        {
            sb.Append(" on");
        }

        sb.Append("\" data-ndm-reminder=\"");
        sb.Append(WebUtility.HtmlEncode(key));
        sb.Append("\" aria-pressed=\"");
        sb.Append(on ? "true" : "false");
        sb.Append("\"></button><span class=\"small\">");
        sb.Append(WebUtility.HtmlEncode(label));
        sb.Append("</span></div>");
    }
}
