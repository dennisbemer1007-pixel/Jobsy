using System.Net;
using System.Text;
using Jobsy.Core.Contracts;
using Jobsy.Web.Components.AfterMatch;
using Jobsy.Web.Localization;
using Jobsy.Web.Media;

namespace Jobsy.Web.Applications;

internal static class AfterMatchConfirmedSsrShells
{
    internal static string BuildBody(
        CultureState culture,
        AfterMatchConfirmedSsrData.PageData data,
        Guid applicationId,
        string? activePanel = null)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"sollicitaties-hero-strip\" aria-hidden=\"true\"><div class=\"sollicitaties-hero-strip__inner\">");
        sb.Append("<img class=\"sollicitaties-hero-strip__img desk\" src=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripDesktopWebp));
        sb.Append("\" alt=\"\" width=\"1440\" height=\"150\" loading=\"eager\" decoding=\"async\" />");
        sb.Append("<img class=\"sollicitaties-hero-strip__img mob\" src=\"");
        sb.Append(WebUtility.HtmlEncode(CandidateIllustratedImages.SollicitatiesHeroStripMobileWebp));
        sb.Append("\" alt=\"\" width=\"390\" height=\"90\" loading=\"eager\" decoding=\"async\" />");
        sb.Append("</div></div>");

        sb.Append("<div class=\"ndm ndm-page ndm-confirmed-page\" data-testid=\"ndm-candidate-live\" data-ndm-view=\"confirmed\" data-ndm-confirmed-ssr=\"1\" data-application-id=\"");
        sb.Append(applicationId.ToString("D"));
        sb.Append("\"><main class=\"wrap\">");

        AppendIntro(sb, culture, data);
        sb.Append("<div class=\"g2 ndm-confirmed-g2\">");
        AppendMainStack(sb, culture, data, activePanel);
        AppendRemindersAside(sb, culture, data);
        sb.Append("</div></main>");

        AppendChatDock(sb, culture, data);
        AppendMockupFooter(sb, culture);
        sb.Append("</div>");
        return sb.ToString();
    }

    private static void AppendIntro(StringBuilder sb, CultureState culture, AfterMatchConfirmedSsrData.PageData data)
    {
        var thread = data.Thread;
        sb.Append("<section class=\"intro\"><nav class=\"bc\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.Breadcrumb"]));
        sb.Append("\"><a href=\"");
        sb.Append(WebUtility.HtmlEncode(data.SollicitatiesHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["Nav.ApplicationsTab"]));
        sb.Append("</a><i>›</i><span>");
        sb.Append(WebUtility.HtmlEncode(thread.CompanyName));
        sb.Append("</span><i>›</i><span>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen05.PageTitle"]));
        sb.Append("</span></nav><h1>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen05.Hero.Title"]));
        sb.Append("</h1><p>");
        sb.Append(WebUtility.HtmlEncode(data.HeroLead));
        sb.Append("</p></section>");
    }

    private static void AppendMainStack(
        StringBuilder sb,
        CultureState culture,
        AfterMatchConfirmedSsrData.PageData data,
        string? activePanel)
    {
        var thread = data.Thread;
        sb.Append("<div class=\"stack\"><div class=\"card\"><div class=\"co\"><span class=\"lg\">");
        sb.Append(WebUtility.HtmlEncode(CompanyInitials(thread.CompanyName)));
        sb.Append("</span><div><h3 style=\"font-size:1.2rem\">");
        sb.Append(WebUtility.HtmlEncode(thread.VacancyTitle));
        sb.Append("</h3><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(VacancySubtitle(thread)));
        sb.Append("</p></div></div><div class=\"hr ndm-confirmed-card-hr\"></div>");

        AppendTimeline(sb, culture, data);
        sb.Append("<div class=\"hr ndm-confirmed-card-hr\"></div><div class=\"row wrapflex ndm-confirmed-when\"><span class=\"ico green\">");
        sb.Append(NdMIcons.Check);
        sb.Append("</span><div class=\"sp\"><h3 style=\"font-size:1.2rem\">");
        sb.Append(WebUtility.HtmlEncode(data.ScheduledTitle));
        sb.Append("</h3><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(data.ScheduledSub));
        sb.Append("</p></div></div>");

        sb.Append("<div class=\"hr ndm-confirmed-card-hr ndm-confirmed-calendar-hr\"></div><span class=\"lbl ndm-confirmed-calendar-lbl\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen05.CalendarRow"]));
        sb.Append("</span><div class=\"addc ndm-confirmed-addc\" data-ndm-calendar-row data-ics-href=\"");
        sb.Append(WebUtility.HtmlEncode(data.CalendarIcsHref));
        sb.Append("\"><a class=\"btn line sm\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.CalendarOutlookHref));
        sb.Append("\" target=\"_blank\" rel=\"noopener noreferrer\" data-ndm-cal-outlook>");
        sb.Append(CalendarBtnInner(culture["AfterMatch.Screen05.Outlook"]));
        sb.Append("</a><a class=\"btn line sm\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.CalendarGoogleHref));
        sb.Append("\" target=\"_blank\" rel=\"noopener noreferrer\" data-ndm-cal-google>");
        sb.Append(CalendarBtnInner(culture["AfterMatch.Screen05.GoogleCal"]));
        sb.Append("</a><a class=\"btn line sm\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.CalendarIcsHref));
        sb.Append("\" data-ndm-cal-apple>");
        sb.Append(CalendarBtnInner(culture["AfterMatch.Screen05.AppleCal"]));
        sb.Append("</a><a class=\"btn line sm\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.CalendarIcsHref));
        sb.Append("\" download data-ndm-cal-ics>");
        sb.Append(IcsIcon);
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen05.Ics"]));
        sb.Append("</a></div>");

        sb.Append("<div class=\"hr ndm-confirmed-actions-hr\"></div><div class=\"row wrapflex ndm-confirmed-actions\" style=\"gap:16px\"><a class=\"lk small\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.RescheduleHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Reschedule"]));
        sb.Append("</a><a class=\"lk small\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.CancelHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Cancel"]));
        sb.Append("</a>");
        if (!string.IsNullOrWhiteSpace(data.RouteHref))
        {
            sb.Append("<a class=\"lk small\" href=\"");
            sb.Append(WebUtility.HtmlEncode(data.RouteHref));
            sb.Append("\" target=\"_blank\" rel=\"noopener noreferrer\">");
            sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen05.Route"]));
            sb.Append("</a>");
        }

        sb.Append("</div></div>");
        if (string.Equals(activePanel, "cancel", StringComparison.OrdinalIgnoreCase))
        {
            AppendCancelPanel(sb, culture, data);
        }

        sb.Append("</div>");
    }

    private static void AppendCancelPanel(
        StringBuilder sb,
        CultureState culture,
        AfterMatchConfirmedSsrData.PageData data)
    {
        sb.Append("<div class=\"card ndm-cancel-panel\" id=\"ndm-cancel-panel\" data-ndm-cancel-panel><h3 style=\"font-size:1.15rem\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Hero.Title"]));
        sb.Append("</h3><p class=\"small muted\" style=\"margin-top:6px\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Hero.Lead"]));
        sb.Append("</p><label class=\"lbl\" style=\"margin-top:14px\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Reason"]));
        sb.Append("</label><textarea class=\"inp area ndm-cancel-reason\" style=\"width:100%;min-height:84px;font:inherit;color:var(--text)\" data-ndm-cancel-reason></textarea><div class=\"row wrapflex\" style=\"gap:10px;margin-top:12px\"><a class=\"btn line\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.RescheduleHref));
        sb.Append("\" data-enhance-nav=\"false\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Reschedule"]));
        sb.Append("</a><button type=\"button\" class=\"btn\" data-ndm-cancel-submit>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen08.Cancel"]));
        sb.Append("</button></div></div>");
    }

    private static string CalendarBtnInner(string label) =>
        """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="4" y="5" width="16" height="15" rx="2"/><path d="M4 10h16M9 3v4M15 3v4"/></svg>"""
        + WebUtility.HtmlEncode(label);

    private const string IcsIcon =
        """<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 4v11M7 10l5 5 5-5M5 20h14"/></svg>""";

    private static void AppendTimeline(StringBuilder sb, CultureState culture, AfterMatchConfirmedSsrData.PageData data)
    {
        sb.Append("<nav class=\"tl\" aria-label=\"");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("\"><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Yes"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(data.YesDateLabel));
        sb.Append("</small></div><div class=\"s done\"><i>");
        sb.Append(NdMIcons.Check);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Plan"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(data.PlanDateLabel));
        sb.Append("</small></div><div class=\"s now\"><i>");
        sb.Append(NdMIcons.Handshake);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Interview"]));
        sb.Append("<small>");
        sb.Append(WebUtility.HtmlEncode(data.InterviewDateLabel));
        sb.Append("</small></div><div class=\"s\"><i>");
        sb.Append(NdMIcons.Flag);
        sb.Append("</i>");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Timeline.Outcome"]));
        sb.Append("<small></small></div></nav>");
    }

    private static void AppendRemindersAside(StringBuilder sb, CultureState culture, AfterMatchConfirmedSsrData.PageData data)
    {
        var prefs = data.Thread.ReminderPreference;
        sb.Append("<aside class=\"card\"><h2 style=\"font-size:1.15rem\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen05.RemindersTitle"]));
        sb.Append("</h2><p class=\"small muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen05.RemindersLead"]));
        sb.Append("</p><div class=\"list ndm-confirmed-reminders\" style=\"margin-top:12px\" data-ndm-reminder-list>");

        AppendReminderRow(
            sb,
            "dayBefore18",
            prefs.DayBefore18,
            culture["AfterMatch.Screen05.Reminder.DayBeforeTitle"],
            culture["AfterMatch.Screen05.Reminder.DayBeforeSub"]);
        AppendReminderRow(
            sb,
            "twoHoursBefore",
            prefs.TwoHoursBefore,
            culture["AfterMatch.Screen05.Reminder.TwoHoursTitle"],
            culture["AfterMatch.Screen05.Reminder.TwoHoursSub"]);
        AppendReminderRow(
            sb,
            "weekBefore",
            prefs.WeekBefore,
            culture["AfterMatch.Screen05.Reminder.WeekBeforeTitle"],
            culture["AfterMatch.Screen05.Reminder.WeekBeforeSub"]);

        sb.Append("</div><div class=\"hr ndm-confirmed-tips-hr\"></div><div class=\"row\"><span class=\"ico\">");
        sb.Append("""<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2.5 2.5M15.5 15.5 18 18M6 18l2.5-2.5M15.5 8.5 18 6"/></svg>""");
        sb.Append("</span><p class=\"small ndm-confirmed-tips\"><a class=\"lk\" href=\"");
        sb.Append(WebUtility.HtmlEncode(data.TipsHref));
        sb.Append("\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Pick.GoodToKnow.TipsLink"]));
        sb.Append("</a><br><span class=\"muted\">");
        sb.Append(WebUtility.HtmlEncode(culture["AfterMatch.Screen05.TipsSuffix"]));
        sb.Append("</span></p></div></aside>");
    }

    private static void AppendReminderRow(
        StringBuilder sb,
        string key,
        bool on,
        string title,
        string subtitle)
    {
        sb.Append("<div class=\"ndm-reminder-row\" role=\"button\" tabindex=\"0\" data-ndm-reminder-row=\"");
        sb.Append(WebUtility.HtmlEncode(key));
        sb.Append("\"><span class=\"cbx");
        if (on)
        {
            sb.Append(" on");
        }

        sb.Append("\" data-ndm-reminder=\"");
        sb.Append(WebUtility.HtmlEncode(key));
        sb.Append("\" aria-hidden=\"true\">");
        sb.Append(NdMIcons.Check);
        sb.Append("</span><div class=\"sp\"><b class=\"small\" style=\"color:var(--brand)\">");
        sb.Append(WebUtility.HtmlEncode(title));
        sb.Append("</b><div class=\"xs muted\">");
        sb.Append(WebUtility.HtmlEncode(subtitle));
        sb.Append("</div></div></div>");
    }

    private static void AppendChatDock(StringBuilder sb, CultureState culture, AfterMatchConfirmedSsrData.PageData data)
    {
        sb.Append("<div class=\"chatdock ndm-confirmed-chatdock\"><button type=\"button\" class=\"askl\" data-ndm-ask-lobsy data-assistant-fallback=\"");
        sb.Append(WebUtility.HtmlEncode(data.AssistantHref));
        sb.Append("\"><img src=\"/brand/svg/lobsy-merkteken.svg\" alt=\"\">");
        sb.Append(WebUtility.HtmlEncode(culture["Assistant.SubtitleWarm"]));
        sb.Append("</button></div>");
    }

    private static void AppendMockupFooter(StringBuilder sb, CultureState culture)
    {
        sb.Append("<footer class=\"lf ndm-mockup-footer\"><div class=\"ll\"><a href=\"/privacy\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Privacy"]));
        sb.Append("</a><a href=\"/algemene-voorwaarden\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Terms"]));
        sb.Append("</a><a href=\"/toegankelijkheid\">");
        sb.Append(WebUtility.HtmlEncode(culture["PublicFooter.Accessibility"]));
        sb.Append("</a><a href=\"/bronnen\">");
        sb.Append(WebUtility.HtmlEncode(culture["Footer.Sources"]));
        sb.Append("</a></div><span>© ");
        sb.Append(DateTime.UtcNow.Year);
        sb.Append(" Lobsy</span></footer>");
    }

    private static string VacancySubtitle(AfterMatchThreadDto t) =>
        string.Join(" · ", new[] { t.CompanyName, t.LocationOrLink, t.TravelLabel }.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static string CompanyInitials(string company)
    {
        var parts = company.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
            : company.Length >= 2 ? company[..2].ToUpperInvariant() : company.ToUpperInvariant();
    }
}
